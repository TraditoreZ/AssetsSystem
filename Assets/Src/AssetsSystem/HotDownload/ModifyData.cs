using System;
namespace AssetSystem
{
    [Serializable]
    public class ModifyData
    {
        [Serializable]
        public class ModifyCell
        {
            public string name;
            public string bundleHash;
            public string fileHash;
            public long size;
            public string writeTime;
        }

        public ModifyCell[] datas;

    }
}