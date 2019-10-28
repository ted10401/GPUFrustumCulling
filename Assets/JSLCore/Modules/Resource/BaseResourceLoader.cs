using System;
using System.Collections.Generic;

namespace JSLCore.Resource
{
    public abstract class BaseResourceLoader
    {
        public abstract string assetBundleName { get; }

        public void LoadAsync<T>(string assetName, Action<T> callback) where T : UnityEngine.Object
        {
            ResourceManager.Instance.LoadAsync<T>(assetBundleName, assetName, callback);
        }

        public void LoadAllAsync<T>(string assetName, Action<List<T>> callback) where T : UnityEngine.Object
        {
            ResourceManager.Instance.LoadAllAsync<T>(assetBundleName, assetName, callback);
        }

        public void Unload<T>(string assetName) where T : UnityEngine.Object
        {
            ResourceManager.Instance.Unload<T>(assetBundleName, assetName);
        }

        public void Clear()
        {
            ResourceManager.Instance.Clear();
        }
    }
}
