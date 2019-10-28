using UnityEditor;

namespace JSLCore.AssetBundle
{
    public class AssetBundleBuildInfo
    {
        public string outputPath;
        public BuildTarget buildTarget;
        public bool cleanFolders;
        public bool copyToStreamingAssets;
        public BuildAssetBundleOptions buildAssetBundleOptions;
        public AssetBundleBuild[] specificAssetBundles;

        public AssetBundleBuildInfo(bool cleanFolders, bool copyToStreamingAssets, BuildAssetBundleOptions buildAssetBundleOptions, AssetBundleBuild[] specificAssetBundles = null)
        {
            outputPath = AssetBundleDef.GetDefaultOutputPath();
            buildTarget = EditorUserBuildSettings.activeBuildTarget;
            this.cleanFolders = cleanFolders;
            this.copyToStreamingAssets = copyToStreamingAssets;
            this.buildAssetBundleOptions = buildAssetBundleOptions;
            this.specificAssetBundles = specificAssetBundles;
        }
    }
}
