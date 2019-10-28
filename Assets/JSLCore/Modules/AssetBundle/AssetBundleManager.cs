using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.Linq;
using JSLCore.Resource;
using UnityEngine.SceneManagement;

namespace JSLCore.AssetBundle
{
	public class AssetBundleManager : MonoSingleton<AssetBundleManager>
	{
		private int m_maxDownloadRequest = 1;
		private AssetBundleLoadType m_assetBundleLoadType;
		private string m_downloadingURL;
		private string m_loadFromFileURL;

		private AssetBundleCatalogs m_assetBundleCatalogs;

		public bool initialized
		{
			get { return m_assetBundleManifest != null; }
		}

		private AssetBundleManifest m_assetBundleManifest;
		private Action<bool> m_onInitializeFinish;

		private Action<AssetBundleDownloadProgress> m_onAssetBundleDownloadProgressChanged;
		private AssetBundleDownloadProgress m_assetBundleDownloadProgress;

		private Dictionary<string, LoadedAssetBundle> m_loadedAssetBundles = new Dictionary<string, LoadedAssetBundle>();
		private Dictionary<string, UnityWebRequest> m_downloadingRequests = new Dictionary<string, UnityWebRequest>();
		private Dictionary<string, string> m_downloadingErrors = new Dictionary<string, string>();
		private List<AssetBundleLoadRequest> m_inProgressRequests = new List<AssetBundleLoadRequest>();
		private Dictionary<string, string[]> m_dependencies = new Dictionary<string, string[]>();
		private List<string> m_completeDownloadAssetBundles = new List<string>();
		private UnityWebRequest m_cacheRequest;

		private List<string> m_waitingDownloadAssetBundleNames = new List<string>();

		private class WaitingDownloadRequest
		{
			public string AssetBundleName;
			public bool IsManifest;
		}
		private Queue<WaitingDownloadRequest> m_waitingDownloadRequests = new Queue<WaitingDownloadRequest>();

		protected override void OnDestroy()
		{
			base.OnDestroy();

			foreach (KeyValuePair<string, UnityWebRequest> downloadingRequest in m_downloadingRequests)
			{
				downloadingRequest.Value.Dispose();
			}
		}

		private void Update()
		{
			UpdateWaitingDownloadRequests();
			UpdateDownloadingRequests();
			UpdateCompleteDownloadRequests();
		}

		private void UpdateWaitingDownloadRequests()
		{
			if (m_waitingDownloadRequests.Count == 0 ||
				m_downloadingRequests.Count() >= m_maxDownloadRequest)
			{
				return;
			}

			for (int i = m_downloadingRequests.Count(); i < m_maxDownloadRequest; i++)
			{
				if (m_waitingDownloadRequests.Count == 0)
				{
					break;
				}

				WaitingDownloadRequest waitingDownloadRequest = m_waitingDownloadRequests.Dequeue();

				UnityWebRequest request = null;
				string url = m_downloadingURL + waitingDownloadRequest.AssetBundleName;

				if (waitingDownloadRequest.IsManifest)
				{
					request = UnityWebRequestAssetBundle.GetAssetBundle(url);
				}
				else
				{
					JSLDebug.LogFormat("[AssetBundleManager] - Start downloading asset bundle '{0}' at frame {1}", waitingDownloadRequest.AssetBundleName, Time.frameCount);
					request = UnityWebRequestAssetBundle.GetAssetBundle(url, m_assetBundleManifest.GetAssetBundleHash(waitingDownloadRequest.AssetBundleName), 0);
				}

				request.SendWebRequest();

				m_downloadingRequests.Add(waitingDownloadRequest.AssetBundleName, request);
				m_waitingDownloadAssetBundleNames.Remove(waitingDownloadRequest.AssetBundleName);
			}
		}

		private void UpdateDownloadingRequests()
		{
			m_completeDownloadAssetBundles.Clear();

			foreach (KeyValuePair<string, UnityWebRequest> keyValue in m_downloadingRequests)
			{
				CachedAssetBundle(keyValue.Key, keyValue.Value);
			}
		}

		private void CachedAssetBundle(string assetBundleName, UnityWebRequest unityWebRequest)
		{
			if (unityWebRequest.isDone)
			{
				if (HasRequestError(assetBundleName, unityWebRequest))
				{
					return;
				}

				UnityEngine.AssetBundle assetBundle = DownloadHandlerAssetBundle.GetContent(unityWebRequest);
				if (assetBundle != null)
				{
					m_loadedAssetBundles.Add(assetBundleName, new LoadedAssetBundle(assetBundle));
				}
				else
				{
					AddDownloadError(assetBundleName, unityWebRequest.error);
				}

				m_completeDownloadAssetBundles.Add(assetBundleName);
			}
		}

		private bool HasRequestError(string assetBundleName, UnityWebRequest unityWebRequest)
		{
			if (unityWebRequest.isHttpError || unityWebRequest.isNetworkError || !string.IsNullOrEmpty(unityWebRequest.error))
			{
				AddDownloadError(assetBundleName, unityWebRequest.error);
				m_completeDownloadAssetBundles.Add(assetBundleName);

				return true;
			}

			return false;
		}

		private void AddDownloadError(string assetBundleName, string error)
		{
			if (m_downloadingErrors.ContainsKey(assetBundleName))
			{
				m_downloadingErrors[assetBundleName] = error;
			}
			else
			{
				m_downloadingErrors.Add(assetBundleName, error);
			}
		}

		private void UpdateCompleteDownloadRequests()
		{
			foreach (string key in m_completeDownloadAssetBundles)
			{
				JSLDebug.LogFormat("[AssetBundleManager] - Download asset bundle '{0}' successfully at frame {1}", key, Time.frameCount);

				m_cacheRequest = m_downloadingRequests[key];
				m_cacheRequest.Dispose();
				m_downloadingRequests.Remove(key);
			}

			if (m_assetBundleDownloadProgress != null)
			{
				m_assetBundleDownloadProgress.SetDownloadCount(m_downloadingRequests.Count + m_waitingDownloadRequests.Count);

				if (m_assetBundleDownloadProgress.progress == 1)
				{
					JSLDebug.LogFormat("[AssetBundleManager] - Download AssetBundle complete at frame {0}", Time.frameCount);
					ResourceManager.Instance.Clear();
				}

				if (m_onAssetBundleDownloadProgressChanged != null)
				{
					m_onAssetBundleDownloadProgressChanged(m_assetBundleDownloadProgress);
				}
			}

			if (m_downloadingRequests.Count == 0 && m_waitingDownloadRequests.Count == 0)
			{
				m_onAssetBundleDownloadProgressChanged = null;
				m_assetBundleDownloadProgress = null;
			}

			m_inProgressRequests.RemoveAll(request => !request.Update());
		}

		private void ReassignShader(UnityEngine.AssetBundle assetBundle)
		{
			if (assetBundle.isStreamedSceneAssetBundle)
			{
				return;
			}

			Material[] materials = assetBundle.LoadAllAssets<Material>();
			Shader cacheShader = null;
			for (int i = 0; i < materials.Length; i++)
			{
				cacheShader = materials[i].shader;
				if (cacheShader == null)
				{
					continue;
				}

				materials[i].shader = Shader.Find(cacheShader.name);
			}
		}

		public void Initialize(AssetBundleInitializeData initializeData)
		{
			m_maxDownloadRequest = initializeData.maxDownloadRequestAmount;
			m_onInitializeFinish = initializeData.onInitializeFinish;
			m_assetBundleLoadType = initializeData.assetBundleLoadType;

			JSLDebug.LogFormat("[AssetBundleManager] - Initialize with load type '{0}'", m_assetBundleLoadType);

			if (m_assetBundleLoadType == AssetBundleLoadType.Streaming)
			{
				initializeData.downloadURL = AssetBundleDef.GetDownloadStreamingAssetsPath();
			}

			if (m_assetBundleLoadType != AssetBundleLoadType.Simulate)
			{
				StartCoroutine(PreInitialize(initializeData.downloadURL));
			}
			else
			{
				if (m_onInitializeFinish != null)
				{
					m_onInitializeFinish(true);
					m_onInitializeFinish = null;
				}
			}
		}

		private IEnumerator PreInitialize(string relativePath)
		{
			string platformName = AssetBundleDef.GetPlatformName();

			m_downloadingURL = string.Format("{0}/{1}/", relativePath, platformName);
			JSLDebug.LogFormat("[AssetBundleManager] - The AssetBundle Download URL is {0}", m_downloadingURL);

			m_loadFromFileURL = string.Format("{0}/{1}/", AssetBundleDef.GetStreamingAssetsPath(), platformName);
            JSLDebug.LogFormat("[AssetBundleManager] - The AssetBundle LoadFromFile URL is {0}", m_loadFromFileURL);

			yield return StartCoroutine(LoadCatalogFromNetwork());

			if (m_assetBundleCatalogs == null)
			{
				if (m_onInitializeFinish != null)
				{
					m_onInitializeFinish(false);
					m_onInitializeFinish = null;
					yield break;
				}
			}

			AssetBundleLoadManifestRequest assetBundleLoadManifestRequest = InitializeManifest(platformName);
			if (assetBundleLoadManifestRequest != null)
			{
				yield return assetBundleLoadManifestRequest;
			}
		}

		private IEnumerator LoadCatalogFromNetwork()
		{
			JSLDebug.LogFormat("[AssetBundleManager] - Start download AssetBundleCatalog at frame {0}", Time.frameCount);

			UnityWebRequest request = UnityWebRequest.Get(m_downloadingURL + AssetBundleDef.CATALOG_FILE_NAME);
			request.SendWebRequest();

			while (!request.isDone)
			{
				yield return null;
			}

			if (!string.IsNullOrEmpty(request.error))
			{
				m_assetBundleCatalogs = null;
				JSLDebug.LogErrorFormat("[AssetBundleManager] - Download AssetBundleCatalog failed. Error log \"{0}\"", request.error);
			}
			else
			{
				m_assetBundleCatalogs = new AssetBundleCatalogs(request.downloadHandler.text);
				JSLDebug.LogFormat("[AssetBundleManager] - Download AssetBundleCatalog complete at frame {0}", Time.frameCount);
			}

			request.Dispose();
		}

		private AssetBundleLoadManifestRequest InitializeManifest(string path)
		{
			UnloadAssetBundles(new List<string> { AssetBundleDef.GetPlatformName() });

			JSLDebug.LogFormat("[AssetBundleManager] - Start download AssetBundleManifest at frame {0}", Time.frameCount);

			DownloadAssetBundle(path, true);

			AssetBundleLoadManifestRequest assetBundleLoadManifestRequest = new AssetBundleLoadManifestRequest(path, "AssetBundleManifest");
			m_inProgressRequests.Add(assetBundleLoadManifestRequest);

			return assetBundleLoadManifestRequest;
		}

		public void SetupManifest(AssetBundleManifest manifest)
		{
			m_assetBundleManifest = manifest;
			if (m_onInitializeFinish != null)
			{
				m_onInitializeFinish(m_assetBundleManifest != null);
				m_onInitializeFinish = null;
			}
		}

		public void Download(Action<AssetBundleDownloadProgress> onAssetBundleDownloadProgressChanged)
		{
			m_onAssetBundleDownloadProgressChanged = onAssetBundleDownloadProgressChanged;

			if (m_assetBundleLoadType == AssetBundleLoadType.Simulate)
			{
				JSLDebug.LogFormat("[AssetBundleManager] - The AssetBundle load type is on Simulate, don't need to download.");
				return;
			}

			if (null == m_assetBundleManifest)
			{
				JSLDebug.LogError("[AssetBundleManager] - Please download AssetBundleManifest by calling ResourceSystem.InstancenitAssetBundle() first");
				return;
			}

			JSLDebug.LogFormat("[AssetBundleManager] - Start download AssetBundle at frame {0}", Time.frameCount);

			string[] allAssetBundles = m_assetBundleManifest.GetAllAssetBundles();
			List<string> downloadAssetBundleNames = new List<string>();

			for (int i = 0; i < allAssetBundles.Length; i++)
			{
				if (Caching.IsVersionCached(m_downloadingURL + allAssetBundles[i], m_assetBundleManifest.GetAssetBundleHash(allAssetBundles[i])))
				{
					continue;
				}

				downloadAssetBundleNames.Add(allAssetBundles[i]);
			}

			UnloadAssetBundles(allAssetBundles.ToList());

			for (int i = 0; i < allAssetBundles.Length; i++)
			{
				DownloadAssetBundle(allAssetBundles[i], false);
			}

			m_assetBundleDownloadProgress = new AssetBundleDownloadProgress(allAssetBundles.Length, m_assetBundleCatalogs.GetAllFileSize(downloadAssetBundleNames));
		}

		public AssetBundleLoadAssetRequest<T> LoadAssetAsync<T>(string assetBundleName, string assetName) where T : UnityEngine.Object
		{
			AssetBundleLoadAssetRequest<T> request = null;

#if UNITY_EDITOR
			if (m_assetBundleLoadType == AssetBundleLoadType.Simulate)
			{
				request = new AssetBundleLoadAssetRequestSimulate<T>(assetBundleName, assetName);
			}
			else
#endif
			{
				LoadAssetBundle(assetBundleName);
				request = new AssetBundleLoadAssetRequestFull<T>(assetBundleName, assetName);
			}

			m_inProgressRequests.Add(request);

			return request;
		}

		public AssetBundleLoadAllAssetRequest<T> LoadAllAssetAsync<T>(string assetBundleName, string assetName) where T : UnityEngine.Object
		{
			AssetBundleLoadAllAssetRequest<T> request = null;

#if UNITY_EDITOR
			if (m_assetBundleLoadType == AssetBundleLoadType.Simulate)
			{
				request = new AssetBundleLoadAllAssetRequestSimulate<T>(assetBundleName, assetName);
			}
			else
#endif
			{
				LoadAssetBundle(assetBundleName);
				request = new AssetBundleLoadAllAssetRequestFull<T>(assetBundleName);
			}

			m_inProgressRequests.Add(request);

			return request;
		}

		public AssetBundleLoadRequest LoadSceneAsync(string assetBundleName, string sceneName, LoadSceneMode loadSceneMode)
		{
			AssetBundleLoadRequest request = null;

#if UNITY_EDITOR
			if (m_assetBundleLoadType == AssetBundleLoadType.Simulate)
			{
				request = new AssetBundleLoadSceneRequestSimulate(assetBundleName, sceneName, loadSceneMode);
			}
			else
#endif
			{
				LoadAssetBundle(assetBundleName);
				request = new AssetBundleLoadSceneRequest(assetBundleName, sceneName, loadSceneMode);
			}

			m_inProgressRequests.Add(request);

			return request;
		}

		private void LoadAssetBundle(string assetBundleName)
		{
            if(m_assetBundleLoadType == AssetBundleLoadType.Streaming)
            {
                LoadFromFile(assetBundleName);
            }
            else if(m_assetBundleLoadType == AssetBundleLoadType.Network)
            {
                LoadFromNetwork(assetBundleName);
            }
        }

        private void LoadFromFile(string assetBundleName)
		{
			LoadDependenciesFromFile(assetBundleName);
			StartCoroutine(LoadAssetBundleFromFile(assetBundleName));
		}

		private void LoadDependenciesFromFile(string assetBundleName)
		{
			if (m_dependencies.ContainsKey(assetBundleName))
			{
				return;
			}

			string[] dependencies = m_assetBundleManifest.GetAllDependencies(assetBundleName);
			if (dependencies.Length == 0)
			{
				return;
			}

			m_dependencies.Add(assetBundleName, dependencies);
			for (int i = 0; i < dependencies.Length; i++)
			{
				LoadFromFile(dependencies[i]);
			}
		}

		private IEnumerator LoadAssetBundleFromFile(string assetBundleName)
		{
			if (m_loadedAssetBundles.ContainsKey(assetBundleName))
			{
				yield break;
			}

			string url = m_loadFromFileURL + assetBundleName;

			AssetBundleCreateRequest assetBundleCreateRequest = UnityEngine.AssetBundle.LoadFromFileAsync(url);
			yield return assetBundleCreateRequest;

			if (assetBundleCreateRequest.assetBundle == null)
			{
				JSLDebug.LogErrorFormat("[AssetBundleManager] - Failed to load AssetBundle from {0}", url);
				yield break;
			}

#if UNITY_EDITOR
			ReassignShader(assetBundleCreateRequest.assetBundle);
#endif

			var loadedAssetBundle = new LoadedAssetBundle(assetBundleCreateRequest.assetBundle);
			m_loadedAssetBundles[assetBundleName] = loadedAssetBundle;
		}

		private void LoadFromNetwork(string assetBundleName)
		{
			LoadDependenciesFromNetwork(assetBundleName);
			LoadAssetBundleFromNetwork(assetBundleName);
		}

		private bool LoadAssetBundleFromNetwork(string assetBundleName)
		{
			LoadedAssetBundle bundle = null;
			m_loadedAssetBundles.TryGetValue(assetBundleName, out bundle);
			if (bundle != null)
			{
				return true;
			}

			return DownloadAssetBundle(assetBundleName, false);
		}

		private bool DownloadAssetBundle(string assetBundleName, bool isLoadingAssetBundleManifest)
		{
			if (m_loadedAssetBundles.ContainsKey(assetBundleName) && m_loadedAssetBundles[assetBundleName].assetBundle != null)
			{
				return true;
			}

			if (m_waitingDownloadAssetBundleNames.Contains(assetBundleName))
			{
				return true;
			}

			// @TODO: Do we need to consider the referenced count of WWWs?
			// In the demo, we never have duplicate WWWs as we wait LoadAssetAsync()/LoadLevelAsync() to be finished before calling another LoadAssetAsync()/LoadLevelAsync().
			// But in the real case, users can call LoadAssetAsync()/LoadLevelAsync() several times then wait them to be finished which might have duplicate WWWs.
			if (m_downloadingRequests.ContainsKey(assetBundleName))
			{
				return true;
			}

			m_waitingDownloadAssetBundleNames.Add(assetBundleName);
			m_waitingDownloadRequests.Enqueue(new WaitingDownloadRequest()
			{
				AssetBundleName = assetBundleName,
				IsManifest = isLoadingAssetBundleManifest
			});

			return false;
		}

		private void LoadDependenciesFromNetwork(string assetBundleName)
		{
			if (m_dependencies.ContainsKey(assetBundleName))
			{
				return;
			}

			string[] dependencies = m_assetBundleManifest.GetAllDependencies(assetBundleName);
			if (dependencies.Length == 0)
			{
				return;
			}

			m_dependencies.Add(assetBundleName, dependencies);
			for (int i = 0; i < dependencies.Length; i++)
			{
				LoadAssetBundleFromNetwork(dependencies[i]);
			}
		}

		private void UnloadAssetBundles(List<string> bundleNames)
		{
			foreach (KeyValuePair<string, UnityWebRequest> downloadingRequest in m_downloadingRequests)
			{
				downloadingRequest.Value.Dispose();
			}
			m_downloadingRequests.Clear();

			foreach (string bundleName in bundleNames)
			{
				UnloadAssetBundle(bundleName);
			}

			m_inProgressRequests.Clear();
			m_waitingDownloadRequests.Clear();
			m_waitingDownloadAssetBundleNames.Clear();

			foreach (UnityEngine.AssetBundle bundle in Resources.FindObjectsOfTypeAll<UnityEngine.AssetBundle>())
			{
				if (bundleNames.Contains(bundle.name))
				{
					bundle.Unload(false);
				}
			}

			Resources.UnloadUnusedAssets();
		}

		private void UnloadAssetBundle(string bundleName)
		{
			UnloadAssetBundleInternal(bundleName);
			UnloadDependencies(bundleName);
		}

		private void UnloadAssetBundleInternal(string bundleName)
		{
			string error = string.Empty;
			LoadedAssetBundle bundle = GetLoadedAssetBundle(bundleName, out error);

			if (bundle != null && bundle.assetBundle != null)
			{
				bundle.assetBundle.Unload(false);
				bundle.assetBundle = null;
			}

			m_loadedAssetBundles.Remove(bundleName);
			m_downloadingErrors.Remove(bundleName);
			m_completeDownloadAssetBundles.Remove(bundleName);
		}

		private void UnloadDependencies(string bundleName)
		{
			string[] dependencies = null;
			if (!m_dependencies.TryGetValue(bundleName, out dependencies))
			{
				return;
			}

			foreach (string dependency in dependencies)
			{
				UnloadAssetBundleInternal(dependency);
			}

			m_dependencies.Remove(bundleName);
		}

		public LoadedAssetBundle GetLoadedAssetBundle(string assetBundleName, out string error)
		{
			if (m_downloadingErrors.TryGetValue(assetBundleName, out error))
			{
				return null;
			}

			LoadedAssetBundle bundle = null;
			m_loadedAssetBundles.TryGetValue(assetBundleName, out bundle);
			if (null == bundle)
			{
				return null;
			}

			string[] dependencies = null;
			if (!m_dependencies.TryGetValue(assetBundleName, out dependencies))
			{
				return bundle;
			}

			foreach (string dependency in dependencies)
			{
				if (m_downloadingErrors.TryGetValue(assetBundleName, out error))
				{
					return bundle;
				}

				LoadedAssetBundle dependentBundle;
				m_loadedAssetBundles.TryGetValue(dependency, out dependentBundle);
				if (null == dependentBundle)
				{
					return null;
				}
			}

			return bundle;
		}
	}
}