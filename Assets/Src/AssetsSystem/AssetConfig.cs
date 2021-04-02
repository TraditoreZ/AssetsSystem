using System;
using System.Collections.Generic;
using UnityEngine;

namespace AssetSystem
{
    public class AssetConfig : ISerializationCallbackReceiver
    {

        public Dictionary<string, string> packInfos;

        public Dictionary<string, string> assetMaps;

        [SerializeField]
        List<string> packInfos_keys;
        [SerializeField]
        List<string> packInfos_values;

        [SerializeField]
        List<string> assetMaps_keys;
        [SerializeField]
        List<string> assetMaps_values;


        public AssetConfig()
        {
            packInfos = new Dictionary<string, string>();
            assetMaps = new Dictionary<string, string>();
        }

        public void OnBeforeSerialize()
        {
            if (packInfos != null)
            {
                packInfos_keys = new List<string>(packInfos.Keys);
                packInfos_values = new List<string>(packInfos.Values);
            }
            if (assetMaps != null)
            {
                assetMaps_keys = new List<string>(assetMaps.Keys);
                assetMaps_values = new List<string>(assetMaps.Values);
            }
        }


        public void OnAfterDeserialize()
        {
            var count = Math.Min(packInfos_keys.Count, packInfos_values.Count);
            packInfos = new Dictionary<string, string>(count);
            for (var i = 0; i < count; ++i)
            {
                packInfos.Add(packInfos_keys[i], packInfos_values[i]);
            }
            count = Math.Min(assetMaps_keys.Count, assetMaps_values.Count);
            assetMaps = new Dictionary<string, string>(count);
            for (var i = 0; i < count; ++i)
            {
                assetMaps.Add(assetMaps_keys[i], assetMaps_values[i]);
            }
        }
    }
}