namespace AssetSystem
{
    public enum EHotDownloadProgress
    {
        None = 0,
        CheckPersistentResource = 1,
        CheckRemoteVersion = 2,
        DownloadModifyList = 3,
        CullingLocalResource = 4,
        CompareAssetHash = 5,
        DownloadAssets = 6,
        FinishDownload = 7,
        DownloadManifest = 9,
        ClearPersistentResource = 10,
        CheckUnzipBinary = 11,
        UnzipBinary = 12,
        CheckFirstRun = 13,
        FirstRunUnzipBinary = 14,
        Over = 15,
    }
}