using System.Collections.Generic;

namespace AssetSystem
{
    public class AssetBundleRule
    {
        public string expression;

        public string packName;

        public string[] options;

        public AssetBundleRule parent;

        public List<AssetBundleRule> subRule = new List<AssetBundleRule>();
    }
}