namespace AssetSystem
{
    public enum EHotDownloadProgress
    {
        None = 0,
        Over = 1,
        CheckAssetVersion = 2,
        DownloadModifyList = 3,
        CullingLocalResource = 4,
        CompareAssetHash = 5,
        DownloadAssets = 6,
        FinishDownload = 7,
        CheckBreakpoint = 8,
        DownloadManifest = 9
    }
}