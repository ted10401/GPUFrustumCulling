
namespace JSLCore.AssetBundle
{
    public class AssetBundleDownloadProgress
    {
        public float progress;
        public int downloadedAssetAmount;
        public int totalDownloadAssetAmount;
        public float totalDownloadSize;

        public AssetBundleDownloadProgress(int totalDownloadAssetAmount, float totalDownloadSize)
        {
            progress = 0;
            downloadedAssetAmount = 0;
            this.totalDownloadAssetAmount = totalDownloadAssetAmount;
            this.totalDownloadSize = totalDownloadSize;
        }

        public void SetDownloadCount(int downloadCount)
        {
            downloadedAssetAmount = totalDownloadAssetAmount - downloadCount;

            if (totalDownloadAssetAmount == 0)
            {
                progress = 1;
            }
            else
            {
                progress = (float)downloadedAssetAmount / totalDownloadAssetAmount;
            }
        }
    }
}
