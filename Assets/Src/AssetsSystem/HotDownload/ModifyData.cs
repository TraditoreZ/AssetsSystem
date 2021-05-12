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
            public string hash;
            public long size;
        }

        public ModifyCell[] datas;

    }
}