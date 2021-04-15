#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AssetSystem
{
    public class AdbPackage : BaseAssetPackage<AdbPackage>
    {
        // AssetDataBase为虚拟包概念
        private HashSet<string> asyncLoading = new HashSet<string>();
        private Dictionary<string, HashSet<Action<Object>>> singleCallBackDic = new Dictionary<string, HashSet<Action<Object>>>();
        public override Object Load(string path)
        {
            Object targer;
            if (!assetMapping.TryGetValue(path, out targer))
            {
                Debug.Log(GetAssetSuffix(packagePath, path));
                targer = AssetDatabase.LoadAssetAtPath<Object>(GetAssetSuffix(packagePath, path));
                assetMapping.Add(path, targer);
            }
            return targer;
        }

        public override Object[] LoadAll()
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(packagePath);
            foreach (Object asset in assets)
            {
                string path = packagePath + asset.name;
                if (!assetMapping.ContainsKey(path))
                {
                    assetMapping.Add(path, asset);
                }
            }
            return assets;
        }

        public override void LoadAsync(string path, Action<Object> callback)
        {
            AssetSystemCore.Instance.StartCoroutine(SimulateAsync(path, callback));
        }

        private IEnumerator SimulateAsync(string path, Action<Object> callback)
        {
            // Editor下模拟 加载延迟
            if (assetMapping.ContainsKey(path))
            {
                callback?.Invoke(assetMapping[path]);
                yield break;
            }
            yield return new WaitForSeconds(AssetSystemCore.Instance.SimulateIODelay ? UnityEngine.Random.Range(0, 0.5f) : 0);
            callback?.Invoke(Load(path));
        }

        public override void LoadAllAsync(Action<Object[]> callback)
        {
            callback?.Invoke(LoadAll());
        }


        public override void LoadPackage(string packagePath, bool async, Action<IAssetPackage> callBack = null)
        {
            base.LoadPackage(packagePath, async, callBack);
            callBack?.Invoke(this);
        }

        public override bool PackageLoaded()
        {
            return true;
        }

        public override void Unload(string path)
        {
            Object targer;
            if (assetMapping.TryGetValue(path, out targer))
            {
                assetMapping.Remove(path);
                Unload(targer);
            }
        }

        public override void Unload(Object obj)
        {
            if (obj.GetType() != typeof(GameObject))
            {
                Resources.UnloadAsset(obj);
            }
            else
            {
                Resources.UnloadUnusedAssets();
            }
        }

        public override void UnloadAll()
        {
            assetMapping.Clear();
            Resources.UnloadUnusedAssets();
        }

        private string GetAssetSuffix(string packagePath, string assetPath)
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(Path.GetFileName(assetPath), @".+\..+$"))
            {
                return assetPath;
            }
            var files = Directory.GetFiles(packagePath.Replace("assets", Application.dataPath), Path.GetFileName(assetPath) + ".*");
            if (files != null)
            {
                string suffixPath = assetPath + "." + Path.GetFileName(files.Where(s => !s.EndsWith(".meta")).First()).Split('.')[1];
                return suffixPath;
            }
            else
            {
                return assetPath;
            }
        }

        public override bool Exist(string path)
        {
            return Load(path) != null;
        }

    }
}
#endif