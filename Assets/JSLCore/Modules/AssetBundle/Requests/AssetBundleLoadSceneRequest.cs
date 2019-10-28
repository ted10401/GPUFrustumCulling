using UnityEngine;
using UnityEngine.SceneManagement;

namespace JSLCore.AssetBundle
{
    public class AssetBundleLoadSceneRequest : AssetBundleLoadRequest
    {
        private string m_assetBundleName;
        private string m_sceneName;
        private LoadSceneMode m_loadSceneMode;
        private string m_downloadingError;
        protected AsyncOperation m_request;

        public AssetBundleLoadSceneRequest(string assetBundleName, string sceneName, LoadSceneMode loadSceneMode)
        {
            m_assetBundleName = assetBundleName;
            m_sceneName = sceneName;
            m_loadSceneMode = loadSceneMode;
        }

        public override bool Update()
        {
            if (null != m_request)
            {
                return false;
            }

            LoadedAssetBundle loadedAssetBundle = AssetBundleManager.Instance.GetLoadedAssetBundle(m_assetBundleName, out m_downloadingError);
            if (null != loadedAssetBundle)
            {
                m_request = SceneManager.LoadSceneAsync(m_sceneName, m_loadSceneMode);

                return false;
            }

            return true;
        }

        public override bool IsDone()
        {
            if (null == m_request && null != m_downloadingError)
            {
                JSLDebug.LogError(m_downloadingError);
                return true;
            }

            return null != m_request && m_request.isDone;
        }

        public override float GetProgress()
        {
            if (IsDone())
            {
                return 1f;
            }

            if (null == m_request)
            {
                return 0f;
            }

            return m_request.progress;
        }
    }
}