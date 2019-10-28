#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JSLCore.AssetBundle
{
    public class AssetBundleLoadSceneRequestSimulate : AssetBundleLoadRequest
    {
        private string m_assetBundleName;
        private string m_sceneName;
        private LoadSceneMode m_loadSceneMode;
        protected AsyncOperation m_request;

        public AssetBundleLoadSceneRequestSimulate(string assetBundleName, string sceneName, LoadSceneMode loadSceneMode)
        {
            m_assetBundleName = assetBundleName;
            m_sceneName = sceneName;
            m_loadSceneMode = loadSceneMode;

            string[] levelPaths = UnityEditor.AssetDatabase.GetAssetPathsFromAssetBundleAndAssetName(m_assetBundleName, m_sceneName);
            if (levelPaths.Length == 0)
            {
                return;
            }

            LoadSceneParameters loadSceneParameters = new LoadSceneParameters();
            loadSceneParameters.loadSceneMode = m_loadSceneMode;
            m_request = UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(levelPaths[0], loadSceneParameters);
        }

        public override bool Update()
        {
            return false;
        }

        public override bool IsDone()
        {
            return null != m_request && m_request.isDone;
        }

        public override float GetProgress()
        {
            if (IsDone())
            {
                return 1f;
            }

            if(m_request == null)
            {
                return 0f;
            }

            return m_request.progress;
        }
    }
}
#endif