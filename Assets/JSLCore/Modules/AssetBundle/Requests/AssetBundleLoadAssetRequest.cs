using UnityEngine;

namespace JSLCore.AssetBundle
{
    public abstract class AssetBundleLoadAssetRequest<T> : AssetBundleLoadRequest where T : Object
    {
        public abstract T GetAsset();
    }
}