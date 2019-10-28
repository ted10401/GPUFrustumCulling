
namespace JSLCore.AssetBundle
{
    public class LoadedAssetBundle
    {
        public UnityEngine.AssetBundle assetBundle;

        public LoadedAssetBundle(UnityEngine.AssetBundle assetBundle)
        {
            this.assetBundle = assetBundle;
        }

#if UNITY_EDITOR
        public UnityEngine.Object simulateObject;
        public LoadedAssetBundle(UnityEngine.Object asset)
        {
            simulateObject = asset;
        }

        public UnityEngine.Object[] simulateObjects;
        public LoadedAssetBundle(UnityEngine.Object[] assets)
        {
            simulateObjects = assets;
        }
#endif
    }
}