
namespace JSLCore.AssetBundle
{
    public class SingleAssetBundleDownloadInfo
    {
        public float progress;
        public float downloadSize;

        public SingleAssetBundleDownloadInfo(float progress, float downloadSize)
        {
            this.progress = progress;
            this.downloadSize = downloadSize;
        }
    }
}
