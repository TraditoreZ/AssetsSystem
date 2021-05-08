using System;
[Serializable]
public class ModifyData
{
    [Serializable]
    public class ModifyCell
    {
        public string assetName;
        public string assetHash;
        public long size;
    }

    public ModifyCell[] datas;

}