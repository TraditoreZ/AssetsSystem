using System;
using System.Collections;
using System.Collections.Generic;
using AssetSystem;
using UnityEngine;
using Object = UnityEngine.Object;
namespace AssetSystem
{
    public class BundlePackage : BaseAssetPackage<BundlePackage>
    {
        private AssetBundle m_AssetBundle;
        private HashSet<string> _assetRef = new HashSet<string>();
        private HashSet<string> _packageRef = new HashSet<string>();
        private Dictionary<string, Queue<Action<Object>>> asyncCallLists = new Dictionary<string, Queue<Action<Object>>>();
        private Queue<Action<Object[]>> asyncAllCallLists = new Queue<Action<Object[]>>();

        public override void LoadPackage(string packagePath, bool async, Action<IAssetPackage> callBack = null)
        {
            base.LoadPackage(packagePath, async, callBack);
            if (async)
            {
                AssetBundle.LoadFromFileAsync(AssetBundlePathResolver.instance.GetBundleFileRuntime(packagePath, false)).completed += OnAssetBundleLoaded;
                packageLoadedCalls.Enqueue(callBack);
            }
            else
            {
                m_AssetBundle = AssetBundle.LoadFromFile(AssetBundlePathResolver.instance.GetBundleFileRuntime(packagePath, false));
            }
        }

        private void OnAssetBundleLoaded(AsyncOperation obj)
        {
            AssetBundleCreateRequest req = obj as AssetBundleCreateRequest;
            req.completed -= OnAssetBundleLoaded;
            if (m_AssetBundle == null)
            {
                m_AssetBundle = req.assetBundle;
            }
            else
            {
                req.assetBundle.Unload(true);
            }
            while (packageLoadedCalls.Count > 0)
            {
                packageLoadedCalls.Dequeue()?.Invoke(this);
            }
        }

        public override bool PackageLoaded()
        {
            return m_AssetBundle != null;
        }

        public override Object Load(string path)
        {
            string assetName = GetAssetNameByPath(path);
            if (!m_AssetBundle.Contains(assetName))
            {
                Debug.LogError("AssetBunle Load Error: Not find:" + path);
                return null;
            }
            Object asset = m_AssetBundle.LoadAsset(assetName);
            _assetRef.Add(assetName);
            return asset;
        }



        public override Object[] LoadAll()
        {
            Object[] objs = m_AssetBundle.LoadAllAssets();
            foreach (var obj in objs)
            {
                _assetRef.Add(obj.name);
            }
            return m_AssetBundle.LoadAllAssets();
        }

        public override void LoadAsync(string path, Action<Object> callback)
        {
            string assetName = GetAssetNameByPath(path);
            if (!m_AssetBundle.Contains(assetName))
            {
                Debug.LogError("AssetBunle LoadAsync Error: Not find:" + path);
                callback?.Invoke(null);
                return;
            }
            if (!asyncCallLists.ContainsKey(assetName))
            {
                asyncCallLists.Add(assetName, new Queue<Action<Object>>());
            }
            if (asyncCallLists[assetName].Count == 0)
            {
                m_AssetBundle.LoadAssetAsync(assetName).completed += OnAssetLoaded;
            }
            asyncCallLists[assetName].Enqueue(callback);
        }

        private void OnAssetLoaded(AsyncOperation obj)
        {
            AssetBundleRequest req = obj as AssetBundleRequest;
            req.completed -= OnAssetLoaded;
            _assetRef.Add(req.asset.name);
            while (asyncCallLists[req.asset.name].Count > 0)
            {
                asyncCallLists[req.asset.name].Dequeue()?.Invoke(req.asset);
            }
        }

        public override void LoadAllAsync(Action<Object[]> callback)
        {
            if (asyncAllCallLists.Count == 0)
            {
                m_AssetBundle.LoadAllAssetsAsync().completed += OnAllAssetLoaded;
            }
            asyncAllCallLists.Enqueue(callback);
        }

        private void OnAllAssetLoaded(AsyncOperation obj)
        {
            AssetBundleRequest req = obj as AssetBundleRequest;
            req.completed -= OnAllAssetLoaded;
            foreach (var asset in req.allAssets)
            {
                _assetRef.Add(asset.name);
            }
            while (asyncAllCallLists.Count > 0)
            {
                asyncAllCallLists.Dequeue()?.Invoke(req.allAssets);
            }
        }

        public override void Unload(string path)
        {
            string assetName = GetAssetNameByPath(path);
            _assetRef.Remove(assetName);
        }

        public override void Unload(Object obj)
        {
            if (obj != null)
            {
                _assetRef.Remove(obj.name);
            }
        }

        public override void UnloadAll()
        {
            _assetRef.Clear();
        }

        private string GetAssetNameByPath(string path)
        {
            return System.IO.Path.GetFileName(path);
        }

        //自身是否还持有资源引用或被其他包引用
        public bool CheckSelfRef()
        {
            Debug.LogWarning(packagePath + " CheckSelfRef    " + _assetRef.Count + "    " + _packageRef.Count);
            return (_assetRef.Count > 0 || _packageRef.Count > 0);
        }

        public bool AddPackageRef(string package)
        {
            Debug.Log(packagePath + "     AddPackageRef:" + package);
            return _packageRef.Add(package);
        }

        public bool RemovePackageRef(string package)
        {
            Debug.Log(packagePath + "     RemovePackageRef:" + package);
            return _packageRef.Remove(package);
        }


        public override void UnloadPackage()
        {
            base.UnloadPackage();
            Debug.LogWarning("Unload AssetBundle:" + packagePath);
            m_AssetBundle.Unload(true);
            _packageRef.Clear();
            _assetRef.Clear();
            m_AssetBundle = null;
        }
    }
}