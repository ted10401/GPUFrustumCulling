using System;

namespace JSLCore.AssetBundle
{
    public class AssetBundleInitializeData
    {
        public int maxDownloadRequestAmount;
        public string downloadURL;
        public AssetBundleLoadType assetBundleLoadType;
        public Action<bool> onInitializeFinish;

        public AssetBundleInitializeData(int maxDownloadRequestAmount,
                                         string downloadURL,
                                         AssetBundleLoadType assetBundleLoadType,
                                         Action<bool> onInitializeFinish)
        {
            this.maxDownloadRequestAmount = maxDownloadRequestAmount;
            this.downloadURL = downloadURL;
            this.assetBundleLoadType = assetBundleLoadType;
            this.onInitializeFinish = onInitializeFinish;
        }
    }
}