using UnityEngine;

namespace JSLCore.AssetBundle
{
    public class AssetBundleLoadAssetRequestFull<T> : AssetBundleLoadAssetRequest<T> where T : Object
    {
        private string m_assetBundleName;
        private string m_assetName;
        private string m_downloadingError;
        protected AssetBundleRequest m_request = null;

        public AssetBundleLoadAssetRequestFull(string assetBundleName, string assetName)
        {
            m_assetBundleName = assetBundleName;
            m_assetName = assetName;
        }

        public override T GetAsset()
        {
            if (null != m_request && m_request.isDone)
            {
                return m_request.asset as T;
            }

            return null;
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
                m_request = loadedAssetBundle.assetBundle.LoadAssetAsync<T>(m_assetName);
                return false;
            }

            return true;
        }

        public override bool IsDone()
        {
            if (null == m_request && null != m_downloadingError)
            {
                JSLDebug.LogErrorFormat("[AssetBundleLoadAssetRequestFull] - Load AssetBundle '{0}' failed with reason {1}", m_assetBundleName, m_downloadingError);
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
