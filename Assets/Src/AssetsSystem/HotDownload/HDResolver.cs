using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AssetSystem
{
    public class HDResolver
    {
        public static bool CheckModify(string compareManifestPath, string beCompareManifestPath, out Dictionary<string, string> modifyList)
        {
            modifyList = null;
            AssetBundle beCompareManifestBundle = AssetBundle.LoadFromFile(beCompareManifestPath);
            if (beCompareManifestBundle == null)
                return false;
            AssetBundleManifest beCompareManifest = beCompareManifestBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
            if (beCompareManifest == null)
                return false;
            beCompareManifestBundle.Unload(false);
            AssetBundle compareManifestBundle = AssetBundle.LoadFromFile(compareManifestPath);
            if (compareManifestBundle == null)
                return false;
            AssetBundleManifest compareManifest = compareManifestBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
            if (compareManifest == null)
                return false;
            compareManifestBundle.Unload(false);
            modifyList = new Dictionary<string, string>();
            string[] compareBundles = compareManifest.GetAllAssetBundles();
            foreach (var compareBundle in compareBundles)
            {
                if (!beCompareManifest.GetAssetBundleHash(compareBundle).Equals(compareManifest.GetAssetBundleHash(compareBundle)))
                {
                    string bundleHash = compareManifest.GetAssetBundleHash(compareBundle).ToString();
                    if (modifyList.ContainsKey(compareBundle))
                    {
                        modifyList[compareBundle] = bundleHash;
                    }
                    else
                    {
                        modifyList.Add(compareBundle, bundleHash);
                    }
                }
            }
            return true;
        }

        public static string GetManifestName(string version)
        {
            return "ver_" + version + "_manifest";
        }

        public static string GetModifyListName(string version)
        {
            return "ver_" + version + "_modify.json";
        }


    }
}