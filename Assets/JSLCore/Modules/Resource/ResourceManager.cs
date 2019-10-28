using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using JSLCore.AssetBundle;
using UnityEngine.SceneManagement;

namespace JSLCore.Resource
{
    public class ResourceManager : MonoSingleton<ResourceManager>
    {
        private const string KEY_FORMAT_PREFAB = "{0}/{1}.prefab";
        private const string KEY_FORMAT_TYPE = "{0}/{1}.{2}";
        private const string LOG_LOAD_ASSET_FAILED = "[ResourceManager] - Load asset async failed. The asset \"{0}\" with bundle name \"{1}\" didn't exist in the project.";
        private const string LOG_LOAD_ASSET_SUCCEED = "[ResourceManager] - Load asset async succeed. The asset \"{0}\" has loaded from frame {1} to frame {2}.";
        private const string LOG_LOADALL_ASSET_FAILED = "[ResourceManager] - LoadAll assets async failed. There is no asset in \"{0}\" with bundle name \"{1}\" in the project.";
        private const string LOG_LOADALL_ASSET_SUCCEED = "[ResourceManager] - LoadAll assets async succeed. All assets in \"{0}\" has loaded from frame {1} to frame {2}.";
        private const string LOG_UNLOAD_ACTIVE_SCENE = "[ResourceManager] - Unload the active scene is not valid.";
        private const string LOG_NO_SCENE_TO_UNLOAD = "[ResourceManager] - There is no scene that could be unloaded.";
        private const string LOG_UNLOAD_SCENE_SUCCEED = "[ResourceManager] - Unload scene async succeed. The scene \"{0}\" has unloaded from frame {1} to frame {2}.";
        private const string LOG_LOAD_SCENE_SUCCEED = "[ResourceManager] - Load scene async succeed. The scene \"{0}\" has loaded from frame {1} to frame {2}.";

        public class LoadedResource
        {
            public UnityEngine.Object resource;
            public UnityEngine.Object[] resources;
            public int referencedCount;

            public LoadedResource(UnityEngine.Object resource)
            {
                this.resource = resource;
                referencedCount = 0;
            }

            public LoadedResource(UnityEngine.Object[] resources)
            {
                this.resources = resources;
                referencedCount = 0;
            }
        }
        
        private Dictionary<string, LoadedResource> m_loadedResources = new Dictionary<string, LoadedResource>();
        private List<string> m_loadingResourceNames = new List<string>();
        private Dictionary<string, int> m_asyncLoadingReferencedCounts = new Dictionary<string, int>();

        private bool IsPrefab<T>() where T : UnityEngine.Object
        {
            return typeof(T) == typeof(GameObject) || typeof(T).IsSubclassOf(typeof(Component));
        }

        private string GetCacheKey<T>(string assetBundleName, string assetName) where T : UnityEngine.Object
        {
            if (IsPrefab<T>())
            {
                return string.Format(KEY_FORMAT_PREFAB, assetBundleName, assetName);
            }

            return string.Format(KEY_FORMAT_TYPE, assetBundleName, assetName, typeof(T).Name);
        }

        private bool InCache(string cacheKey)
        {
            return m_loadedResources.ContainsKey(cacheKey);
        }

        public void Unload<T>(string assetName, bool forceUnload = false) where T : UnityEngine.Object
        {
            Unload<T>(string.Empty, assetName, forceUnload);
        }
        
        public void Unload<T>(string assetBundleName, string assetName, bool forceUnload = false) where T : UnityEngine.Object
        {
            string cacheKey = GetCacheKey<T>(assetBundleName, assetName);

            if (InCache(cacheKey))
            {
                LoadedResource res = m_loadedResources[cacheKey];
                if(forceUnload)
                {
                    res.referencedCount = 0;
                }
                else
                {
                    res.referencedCount--;
                }

                Release();
            }
        }

        public void Release()
        {
            List<string> removeResources = new List<string>();

            foreach (KeyValuePair<string, LoadedResource> kvp in m_loadedResources)
            {
                if (m_asyncLoadingReferencedCounts.ContainsKey(kvp.Key))
                {
                    continue;
                }

                if (kvp.Value.referencedCount <= 0)
                {
                    removeResources.Add(kvp.Key);
                }
            }

            for (int i = 0; i < removeResources.Count; i++)
            {
                m_loadedResources.Remove(removeResources[i]);
            }
        }

        public void Clear()
        {
            m_loadedResources.Clear();
            Resources.UnloadUnusedAssets();
            GC.Collect();
        }

        #region LoadAsync
        public void LoadAsync<T>(string assetName, Action<T> callback, bool unloadAutomatically = true) where T : UnityEngine.Object
        {
            LoadAsync<T>(string.Empty, assetName, callback, unloadAutomatically);
        }

        public void LoadAsync<T>(string assetBundleName, string assetName, Action<T> callback, bool unloadAutomatically = true) where T : UnityEngine.Object
        {
            StartCoroutine(StartLoadAsync<T>(assetBundleName, assetName, callback, unloadAutomatically));
        }

        public void LoadAsync<T>(string assetName, Action<T, object> callback, object customData, bool unloadAutomatically = true) where T : UnityEngine.Object
        {
            LoadAsync<T>(string.Empty, assetName, callback, customData, unloadAutomatically);
        }

        public void LoadAsync<T>(string assetBundleName, string assetName, Action<T, object> callback, object customData, bool unloadAutomatically = true) where T : UnityEngine.Object
        {
            StartCoroutine(StartLoadAsync<T>(assetBundleName, assetName, callback, customData, unloadAutomatically));
        }

        private IEnumerator StartLoadAsync<T>(string assetBundleName, string assetName, Action<T> callback, bool unloadAutomatically) where T : UnityEngine.Object
        {
            if(string.IsNullOrEmpty(assetName))
            {
                JSLDebug.LogErrorFormat(LOG_LOAD_ASSET_FAILED, assetName, assetBundleName);
                callback?.Invoke(null);
                yield break;
            }

            string cacheKey = GetCacheKey<T>(assetBundleName, assetName);

            if (IsPrefab<T>())
            {
                yield return PreloadAsync<GameObject>(cacheKey, assetBundleName, assetName);
            }
            else
            {
                yield return PreloadAsync<T>(cacheKey, assetBundleName, assetName);
            }

            if (!InCache(cacheKey))
            {
                JSLDebug.LogErrorFormat(LOG_LOAD_ASSET_FAILED, assetName, assetBundleName);

                callback?.Invoke(null);
                RemoveAsyncLoadingReferencedCounts(cacheKey);

                yield break;
            }

            LoadedResource res = m_loadedResources[cacheKey];
            res.referencedCount++;

            T asset = res.resource as T;
            if (asset == null)
            {
                asset = (res.resource as GameObject).GetComponent<T>();
            }

            callback?.Invoke(asset);
            RemoveAsyncLoadingReferencedCounts(cacheKey);

            if (unloadAutomatically)
            {
                Unload<T>(assetBundleName, assetName);
            }
        }

        private IEnumerator StartLoadAsync<T>(string assetBundleName, string assetName, Action<T, object> callback, object customData, bool unloadAutomatically) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(assetName))
            {
                JSLDebug.LogErrorFormat(LOG_LOAD_ASSET_FAILED, assetName, assetBundleName);
                callback?.Invoke(null, customData);
                yield break;
            }

            string cacheKey = GetCacheKey<T>(assetBundleName, assetName);

            if (IsPrefab<T>())
            {
                yield return PreloadAsync<GameObject>(cacheKey, assetBundleName, assetName);
            }
            else
            {
                yield return PreloadAsync<T>(cacheKey, assetBundleName, assetName);
            }

            if (!InCache(cacheKey))
            {
                JSLDebug.LogErrorFormat(LOG_LOAD_ASSET_FAILED, assetName, assetBundleName);

                callback?.Invoke(null, customData);
                RemoveAsyncLoadingReferencedCounts(cacheKey);

                yield break;
            }

            LoadedResource res = m_loadedResources[cacheKey];
            res.referencedCount++;

            T asset = res.resource as T;
            if (asset == null)
            {
                asset = (res.resource as GameObject).GetComponent<T>();
            }

            callback?.Invoke(asset, customData);
            RemoveAsyncLoadingReferencedCounts(cacheKey);

            if (unloadAutomatically)
            {
                Unload<T>(assetBundleName, assetName);
            }
        }

        private IEnumerator PreloadAsync<T>(string cacheKey, string assetBundleName, string assetName) where T : UnityEngine.Object
        {
            AddAsyncLoadingReferencedCounts(cacheKey);

            if (InCache(cacheKey))
            {
                yield break;
            }

            if (m_asyncLoadingReferencedCounts[cacheKey] == 1)
            {
                //Preload the asset from AssetBundle async
                if (!string.IsNullOrEmpty(assetBundleName))
                {
                    yield return PreloadAssetBundleAsync<T>(cacheKey, assetBundleName, assetName);
                }

                //Preload the asset from Resources async
                if (!InCache(cacheKey))
                {
                    yield return PreloadResourceAsync<T>(cacheKey, assetName);
                }
            }
            else
            {
                while (m_loadingResourceNames.Contains(cacheKey))
                {
                    yield return null;
                }
            }
        }

        private void AddAsyncLoadingReferencedCounts(string path)
        {
            if(!m_loadingResourceNames.Contains(path))
            {
                m_loadingResourceNames.Add(path);
            }

            if (m_asyncLoadingReferencedCounts.ContainsKey(path))
            {
                m_asyncLoadingReferencedCounts[path] = m_asyncLoadingReferencedCounts[path] + 1;
            }
            else
            {
                m_asyncLoadingReferencedCounts.Add(path, 1);
            }
        }

        private void RemoveAsyncLoadingReferencedCounts(string path)
        {
            if (m_loadingResourceNames.Contains(path))
            {
                m_loadingResourceNames.Remove(path);
            }

            if (m_asyncLoadingReferencedCounts.ContainsKey(path))
            {
                m_asyncLoadingReferencedCounts[path]--;

                if (m_asyncLoadingReferencedCounts[path] == 0)
                {
                    m_asyncLoadingReferencedCounts.Remove(path);
                }
            }
        }

        private IEnumerator PreloadResourceAsync<T>(string cacheKey, string assetName) where T : UnityEngine.Object
        {
            int startFrameCount = Time.frameCount;

            ResourceRequest resourceRequest = Resources.LoadAsync<T>(assetName);
            while (!resourceRequest.isDone)
            {
                yield return 0;
            }

            LoadedResource res = new LoadedResource(resourceRequest.asset);

            if (null == res.resource)
            {
                yield break;
            }

            m_loadedResources[cacheKey] = res;
            
            JSLDebug.LogFormat(LOG_LOAD_ASSET_SUCCEED, assetName, startFrameCount, Time.frameCount);
        }

        private IEnumerator PreloadAssetBundleAsync<T>(string cacheKey, string assetBundleName, string assetName) where T : UnityEngine.Object
        {
            int startFrameCount = Time.frameCount;

            AssetBundleLoadAssetRequest<T> request = AssetBundleManager.Instance.LoadAssetAsync<T>(assetBundleName, assetName);
            if (null == request)
            {
                yield break;
            }

            yield return request;

            T asset = request.GetAsset();
            if(null == asset)
            {
                yield break;
            }

            LoadedResource res = new LoadedResource(asset);
            m_loadedResources[cacheKey] = res;
            
            JSLDebug.LogFormat(LOG_LOAD_ASSET_SUCCEED, assetName, startFrameCount, Time.frameCount);
        }
        #endregion

        #region LoadAllAsync
        public void LoadAllAsync<T>(string assetName, Action<List<T>> callback, bool unloadAutomatically = true) where T : UnityEngine.Object
        {
            LoadAllAsync<T>(string.Empty, assetName, callback, unloadAutomatically);
        }

        public void LoadAllAsync<T>(string assetBundleName, string assetName, Action<List<T>> callback, bool unloadAutomatically = true) where T : UnityEngine.Object
        {
            StartCoroutine(StartLoadAllAsync<T>(assetBundleName, assetName, callback, unloadAutomatically));
        }

        public void LoadAllAsync<T>(string assetName, Action<List<T>, object> callback, object customData, bool unloadAutomatically = true) where T : UnityEngine.Object
        {
            LoadAllAsync<T>(string.Empty, assetName, callback, customData, unloadAutomatically);
        }

        public void LoadAllAsync<T>(string assetBundleName, string assetName, Action<List<T>, object> callback, object customData, bool unloadAutomatically = true) where T : UnityEngine.Object
        {
            StartCoroutine(StartLoadAllAsync<T>(assetBundleName, assetName, callback, customData, unloadAutomatically));
        }

        private IEnumerator StartLoadAllAsync<T>(string assetBundleName, string assetName, Action<List<T>> callback, bool unloadAutomatically) where T : UnityEngine.Object
        {
            string cacheKey = GetCacheKey<T>(assetBundleName, assetName);
            yield return PreloadAllAsync<T>(cacheKey, assetBundleName, assetName);

            if (!InCache(cacheKey))
            {
                JSLDebug.LogWarningFormat(LOG_LOADALL_ASSET_FAILED, assetName, assetBundleName);

                callback?.Invoke(null);
                RemoveAsyncLoadingReferencedCounts(cacheKey);

                yield break;
            }

            LoadedResource res = m_loadedResources[GetCacheKey<T>(assetBundleName, assetName)];
            res.referencedCount++;

            List<T> assets = new List<T>();
            T cache = null;

            for (int i = 0; i < res.resources.Length; i++)
            {
                cache = (T)res.resources[i];
                if (cache)
                {
                    assets.Add(cache);
                }
            }

            callback?.Invoke(assets);
            RemoveAsyncLoadingReferencedCounts(cacheKey);

            if (unloadAutomatically)
            {
                Unload<T>(assetBundleName, assetName);
            }
        }

        private IEnumerator StartLoadAllAsync<T>(string assetBundleName, string assetName, Action<List<T>, object> callback, object customData, bool unloadAutomatically) where T : UnityEngine.Object
        {
            string cacheKey = GetCacheKey<T>(assetBundleName, assetName);
            yield return PreloadAllAsync<T>(cacheKey, assetBundleName, assetName);

            if (!InCache(cacheKey))
            {
                JSLDebug.LogWarningFormat(LOG_LOADALL_ASSET_FAILED, assetName, assetBundleName);

                callback?.Invoke(null, customData);
                RemoveAsyncLoadingReferencedCounts(cacheKey);

                yield break;
            }

            LoadedResource res = m_loadedResources[cacheKey];
            res.referencedCount++;

            List<T> assets = new List<T>();
            T cache = null;

            for (int i = 0; i < res.resources.Length; i++)
            {
                cache = (T)res.resources[i];
                if (cache)
                {
                    assets.Add(cache);
                }
            }

            callback?.Invoke(assets, customData);
            RemoveAsyncLoadingReferencedCounts(cacheKey);

            if (unloadAutomatically)
            {
                Unload<T>(assetBundleName, assetName);
            }
        }

        private IEnumerator PreloadAllAsync<T>(string cacheKey, string assetBundleName, string assetName) where T : UnityEngine.Object
        {
            AddAsyncLoadingReferencedCounts(cacheKey);

            if (InCache(cacheKey))
            {
                yield break;
            }

            if (m_asyncLoadingReferencedCounts[cacheKey] == 1)
            {
                //Preload the asset from AssetBundle async
                if (!string.IsNullOrEmpty(assetBundleName))
                {
                    yield return PreloadAllAssetBundleAsync<T>(cacheKey, assetBundleName, assetName);
                }

                //Preload the asset from Resources async
                if (!InCache(cacheKey))
                {
                    yield return PreloadAllResourceAsync<T>(cacheKey, assetName);
                }
            }
            else
            {
                while (m_loadingResourceNames.Contains(cacheKey))
                {
                    yield return null;
                }
            }
        }

        private IEnumerator PreloadAllAssetBundleAsync<T>(string cacheKey, string assetBundleName, string assetName) where T : UnityEngine.Object
        {
            int startFrameCount = Time.frameCount;

            AssetBundleLoadAllAssetRequest<T> request = AssetBundleManager.Instance.LoadAllAssetAsync<T>(assetBundleName, assetName);
            if (null == request)
            {
                yield break;
            }

            yield return request;

            T[] assets = request.GetAsset();
            if(null == assets || assets.Length == 0)
            {
                yield break;
            }

            LoadedResource res = new LoadedResource(assets);
            m_loadedResources[cacheKey] = res;

            JSLDebug.LogFormat(LOG_LOADALL_ASSET_SUCCEED, assetName, startFrameCount, Time.frameCount);
        }

        private IEnumerator PreloadAllResourceAsync<T>(string cacheKey, string assetName) where T : UnityEngine.Object
        {
            int startFrameCount = Time.frameCount;

            T[] assets = Resources.LoadAll<T>(assetName);

            LoadedResource res = new LoadedResource(assets);

            if (null == res.resources || res.resources.Length == 0)
            {
                yield break;
            }

            m_loadedResources[cacheKey] = res;

            JSLDebug.LogFormat(LOG_LOADALL_ASSET_SUCCEED, assetName, startFrameCount, Time.frameCount);
        }
        #endregion

        #region LoadSceneAsync
        private bool m_shouldUnload;
        private AsyncOperation m_cacheAsyncOperation;
        private AssetBundleLoadRequest m_cacheAssetBundleLoadRequest;

        public void UnloadSceneAsync(string sceneName, Action callback)
        {
            StartCoroutine(StartUnloadSceneAsync(sceneName, callback));
        }

        public void UnloadSceneAsync(string sceneName, Action<object> callback, object customData)
        {
            StartCoroutine(StartUnloadSceneAsync(sceneName, callback, customData));
        }
        
        private IEnumerator StartUnloadSceneAsync(string sceneName, Action callback)
        {
            int startFrameCount = Time.frameCount;

            if (SceneManager.GetActiveScene().name == sceneName)
            {
                JSLDebug.LogWarning(LOG_UNLOAD_ACTIVE_SCENE);

                callback?.Invoke();

                yield break;
            }

            m_shouldUnload = false;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i).name == sceneName)
                {
                    m_shouldUnload = true;
                }
            }

            if (!m_shouldUnload)
            {
                JSLDebug.LogWarning(LOG_NO_SCENE_TO_UNLOAD);

                callback?.Invoke();

                yield break;
            }

            m_cacheAsyncOperation = SceneManager.UnloadSceneAsync(sceneName);
            if (m_cacheAsyncOperation == null)
            {
                callback?.Invoke();

                yield break;
            }

            while (!m_cacheAsyncOperation.isDone)
            {
                yield return 0;
            }

            JSLDebug.LogFormat(LOG_UNLOAD_SCENE_SUCCEED, sceneName, startFrameCount, Time.frameCount);

            callback?.Invoke();
        }

        private IEnumerator StartUnloadSceneAsync(string sceneName, Action<object> callback, object customData)
        {
            int startFrameCount = Time.frameCount;

            if (SceneManager.GetActiveScene().name == sceneName)
            {
                JSLDebug.LogWarning(LOG_UNLOAD_ACTIVE_SCENE);

                callback?.Invoke(customData);

                yield break;
            }

            m_shouldUnload = false;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i).name == sceneName)
                {
                    m_shouldUnload = true;
                }
            }

            if (!m_shouldUnload)
            {
                JSLDebug.LogWarning(LOG_NO_SCENE_TO_UNLOAD);

                callback?.Invoke(customData);

                yield break;
            }

            m_cacheAsyncOperation = SceneManager.UnloadSceneAsync(sceneName);
            if (m_cacheAsyncOperation == null)
            {
                callback?.Invoke(customData);

                yield break;
            }

            while (!m_cacheAsyncOperation.isDone)
            {
                yield return 0;
            }

            JSLDebug.LogFormat(LOG_UNLOAD_SCENE_SUCCEED, sceneName, startFrameCount, Time.frameCount);

            callback?.Invoke(customData);
        }

        public void LoadSceneAsync(string sceneName, LoadSceneMode loadSceneMode, Action callback, Action<float> progressCallback)
        {
            LoadSceneAsync(string.Empty, sceneName, loadSceneMode, callback, progressCallback);
        }

        public void LoadSceneAsync(string sceneName, LoadSceneMode loadSceneMode, Action<object> callback, object customData, Action<float> progressCallback)
        {
            LoadSceneAsync(string.Empty, sceneName, loadSceneMode, callback, customData, progressCallback);
        }

        public void LoadSceneAsync(string assetBundleName, string sceneName, LoadSceneMode loadSceneMode, Action callback, Action<float> progressCallback)
        {
            StartCoroutine(StartLoadSceneAsync(assetBundleName, sceneName, loadSceneMode, callback, progressCallback));
        }

        public void LoadSceneAsync(string assetBundleName, string sceneName, LoadSceneMode loadSceneMode, Action<object> callback, object customData, Action<float> progressCallback)
        {
            StartCoroutine(StartLoadSceneAsync(assetBundleName, sceneName, loadSceneMode, callback, customData, progressCallback));
        }

        private IEnumerator StartLoadSceneAsync(string assetBundleName, string sceneName, LoadSceneMode loadSceneMode, Action callback, Action<float> progressCallback)
        {
            yield return PreloadSceneAsync(assetBundleName, sceneName, loadSceneMode, progressCallback);
            callback?.Invoke();
        }

        private IEnumerator StartLoadSceneAsync(string assetBundleName, string sceneName, LoadSceneMode loadSceneMode, Action<object> callback, object customData, Action<float> progressCallback)
        {
            yield return PreloadSceneAsync(assetBundleName, sceneName, loadSceneMode, progressCallback);
            callback?.Invoke(customData);
        }

        private IEnumerator PreloadSceneAsync(string assetBundleName, string sceneName, LoadSceneMode loadSceneMode, Action<float> progressCallback)
        {
            if (!string.IsNullOrEmpty(assetBundleName))
            {
                yield return PreloadSceneAssetBundleAsync(assetBundleName, sceneName, loadSceneMode, progressCallback);
            }
            else
            {
                yield return PreloadSceneAsync(sceneName, loadSceneMode, progressCallback);
            }
        }
        
        private IEnumerator PreloadSceneAssetBundleAsync(string assetBundleName, string sceneName, LoadSceneMode loadSceneMode, Action<float> progressCallback)
        {
            int startFrameCount = Time.frameCount;

            m_cacheAssetBundleLoadRequest = AssetBundleManager.Instance.LoadSceneAsync(assetBundleName, sceneName, loadSceneMode);
            if (null == m_cacheAssetBundleLoadRequest)
            {
                yield break;
            }

            while(!m_cacheAssetBundleLoadRequest.IsDone())
            {
                progressCallback?.Invoke(m_cacheAssetBundleLoadRequest.GetProgress());
                yield return null;
            }

            progressCallback?.Invoke(m_cacheAssetBundleLoadRequest.GetProgress());
            JSLDebug.LogFormat(LOG_LOAD_SCENE_SUCCEED, sceneName, startFrameCount, Time.frameCount);
        }

        private IEnumerator PreloadSceneAsync(string sceneName, LoadSceneMode loadSceneMode, Action<float> progressCallback)
        {
            int startFrameCount = Time.frameCount;

            m_cacheAsyncOperation = SceneManager.LoadSceneAsync(sceneName, loadSceneMode);
            while (!m_cacheAsyncOperation.isDone)
            {
                progressCallback?.Invoke(m_cacheAsyncOperation.progress);
                yield return null;
            }

            progressCallback?.Invoke(1f);
            JSLDebug.LogFormat(LOG_LOAD_SCENE_SUCCEED, sceneName, startFrameCount, Time.frameCount);
        }
        #endregion
    }
}

