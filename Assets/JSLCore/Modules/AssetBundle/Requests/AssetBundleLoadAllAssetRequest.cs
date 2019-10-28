using UnityEngine;

namespace JSLCore.AssetBundle
{
    public abstract class AssetBundleLoadAllAssetRequest<T> : AssetBundleLoadRequest where T : Object
    {
        public abstract T[] GetAsset();
    }
}