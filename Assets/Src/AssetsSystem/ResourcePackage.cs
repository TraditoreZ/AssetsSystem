using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
namespace AssetSystem
{
    public class ResourcePackage : BaseAssetPackage<ResourcePackage>
    {
        // Resource为虚拟包概念
        private HashSet<string> asyncLoading = new HashSet<string>();
        private Dictionary<string, HashSet<Action<Object>>> singleCallBackDic = new Dictionary<string, HashSet<Action<Object>>>();

        public override Object Load(string path)
        {
            Object targer;
            if (!assetMapping.TryGetValue(path, out targer))
            {
                targer = Resources.Load(path);
                assetMapping.Add(path, targer);
            }
            return targer;
        }

        public override Object[] LoadAll()
        {
            Object[] assets = Resources.LoadAll(packagePath);
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
            Object targer;
            if (assetMapping.TryGetValue(path, out targer))
            {
                callback?.Invoke(targer);
            }
            else
            {
                if (asyncLoading.Add(path))
                {
                    if (!singleCallBackDic.ContainsKey(path))
                    {
                        singleCallBackDic.Add(path, new HashSet<Action<Object>>());
                    }
                    singleCallBackDic[path].Add(callback);
                    Resources.LoadAsync(path).completed += OnLoadCompleted;
                }
                else
                {
                    singleCallBackDic[path].Add(callback);
                }
            }
        }

        private void OnLoadCompleted(AsyncOperation obj)
        {
            ResourceRequest requset = obj as ResourceRequest;
            requset.completed -= OnLoadCompleted;
            string path = GetAssetPath(requset.asset.name);
            Debug.Log(path);
            assetMapping.Add(path, requset.asset);
            asyncLoading.Remove(path);
            foreach (Action<Object> call in singleCallBackDic[path])
            {
                call?.Invoke(requset.asset);
            }
            singleCallBackDic[path].Clear();
        }

        public override void LoadAllAsync(Action<Object[]> callback)
        {
            callback?.Invoke(LoadAll());
        }

        public override void LoadPackage(string packagePath, bool async, Action<IAssetPackage> callBack = null)
        {
            this.packagePath = packagePath;
            Debug.Log("[Asset Package] LoadPackage:" + packagePath);
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

        private string GetAssetPath(string assetName)
        {
            return string.Format("{0}/{1}", packagePath, assetName);
        }

        public override bool Exist(string path)
        {
            return Load(path) != null;
        }
        
    }
}