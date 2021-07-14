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
            public long size;
        }

        public ModifyCell[] datas;

    }
}