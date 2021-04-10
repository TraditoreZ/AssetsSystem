using System;
using System.Collections;
using System.Collections.Generic;
using AssetSystem;
using UnityEngine;
using System.Linq;
using Object = UnityEngine.Object;
namespace AssetSystem
{
    public class BundlePackage : BaseAssetPackage<BundlePackage>
    {
        private AssetBundle m_AssetBundle;
        private Dictionary<string, Object> _assetRef = new Dictionary<string, Object>();
        private HashSet<string> _packageRef = new HashSet<string>();
        private Dictionary<string, Queue<Action<Object>>> asyncCallLists = new Dictionary<string, Queue<Action<Object>>>();
        private Queue<Action<Object[]>> asyncAllCallLists = new Queue<Action<Object[]>>();
        private bool isStreamedSceneAssetBundle;
        private bool NULLPACKAGE = false;
        public override void LoadPackage(string packagePath, bool async, Action<IAssetPackage> callBack = null)
        {
            base.LoadPackage(packagePath, async, callBack);
            NULLPACKAGE = !System.IO.File.Exists(AssetBundlePathResolver.instance.GetBundleFileRuntime(packagePath, false));
            if (NULLPACKAGE)
            {
                callBack?.Invoke(this);
                return;
            }
            if (async)
            {
                AssetBundle.LoadFromFileAsync(AssetBundlePathResolver.instance.GetBundleFileRuntime(packagePath, false)).completed += OnAssetBundleLoaded;
                packageLoadedCalls.Enqueue(callBack);
            }
            else
            {
                m_AssetBundle = AssetBundle.LoadFromFile(AssetBundlePathResolver.instance.GetBundleFileRuntime(packagePath, false));
                isStreamedSceneAssetBundle = m_AssetBundle.isStreamedSceneAssetBundle;
            }
        }

        private void OnAssetBundleLoaded(AsyncOperation obj)
        {
            AssetBundleCreateRequest req = obj as AssetBundleCreateRequest;
            req.completed -= OnAssetBundleLoaded;
            if (m_AssetBundle == null)
            {
                m_AssetBundle = req.assetBundle;
                isStreamedSceneAssetBundle = m_AssetBundle.isStreamedSceneAssetBundle;
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
            return NULLPACKAGE == false && m_AssetBundle != null;
        }

        public override Object Load(string path)
        {
            if (NULLPACKAGE)
            {
                Debug.LogError("AssetBunle Error Null package:" + packagePath);
                return null;
            }
            if (isStreamedSceneAssetBundle)
            {
                Debug.LogError("AssetBunle isStreamedSceneAssetBundle:" + path);
                return null;
            }
            string assetName = GetAssetNameByPath(path);
            if (!m_AssetBundle.Contains(assetName))
            {
                Debug.LogError("AssetBunle Load Error: Not find:" + path);
                return null;
            }
            Object asset = null;
            if (!_assetRef.TryGetValue(assetName, out asset))
            {
                asset = m_AssetBundle.LoadAsset(assetName);
                _assetRef.Add(assetName, asset);
            }
            // _assetRef.Add(assetName);
            return asset;
        }



        public override Object[] LoadAll()
        {
            if (NULLPACKAGE)
            {
                Debug.LogError("AssetBunle Error Null package:" + packagePath);
                return null;
            }
            if (isStreamedSceneAssetBundle)
            {
                Debug.LogError("AssetBunle isStreamedSceneAssetBundle:" + packagePath);
                return null;
            }
            string[] names = m_AssetBundle.GetAllAssetNames();
            Object[] objs = new Object[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                string assetName = names[i];
                Object asset = null;
                if (!_assetRef.TryGetValue(assetName, out asset))
                {
                    asset = m_AssetBundle.LoadAsset(assetName);
                    _assetRef.Add(assetName, asset);
                }
                objs[i] = asset;
            }
            return objs;
        }

        public override void LoadAsync(string path, Action<Object> callback)
        {
            if (NULLPACKAGE)
            {
                Debug.LogError("AssetBunle Error Null package:" + packagePath);
                callback?.Invoke(null);
                return;
            }
            if (isStreamedSceneAssetBundle)
            {
                Debug.LogError("AssetBunle isStreamedSceneAssetBundle:" + packagePath);
                callback?.Invoke(null);
                return;
            }
            string assetName = GetAssetNameByPath(path);
            if (!m_AssetBundle.Contains(assetName))
            {
                Debug.LogError("AssetBunle LoadAsync Error: Not find:" + path);
                callback?.Invoke(null);
                return;
            }
            Object asset = null;
            if (_assetRef.TryGetValue(assetName, out asset))
            {
                callback?.Invoke(asset);
            }
            else
            {
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
        }

        private void OnAssetLoaded(AsyncOperation obj)
        {
            AssetBundleRequest req = obj as AssetBundleRequest;
            req.completed -= OnAssetLoaded;
            string assetName = req.asset.name.ToLower();
            _assetRef.Add(assetName, req.asset);
            while (asyncCallLists[assetName].Count > 0)
            {
                asyncCallLists[assetName].Dequeue()?.Invoke(req.asset);
            }
        }

        public override void LoadAllAsync(Action<Object[]> callback)
        {
            if (NULLPACKAGE)
            {
                Debug.LogError("AssetBunle Error Null package:" + packagePath);
                callback?.Invoke(null);
                return;
            }
            if (isStreamedSceneAssetBundle)
            {
                Debug.LogError("AssetBunle isStreamedSceneAssetBundle:" + packagePath);
                callback?.Invoke(null);
                return;
            }
            string[] names = m_AssetBundle.GetAllAssetNames();
            if (names.Length == _assetRef.Count)
            {
                callback?.Invoke(_assetRef.Values.ToArray());
            }
            else
            {
                if (asyncAllCallLists.Count == 0)
                {
                    m_AssetBundle.LoadAllAssetsAsync().completed += OnAllAssetLoaded;
                }
                asyncAllCallLists.Enqueue(callback);
            }
        }

        private void OnAllAssetLoaded(AsyncOperation obj)
        {
            AssetBundleRequest req = obj as AssetBundleRequest;
            req.completed -= OnAllAssetLoaded;
            foreach (var asset in req.allAssets)
            {
                string assetName = asset.name.ToLower();
                if (!_assetRef.ContainsKey(assetName))
                {
                    _assetRef.Add(assetName, asset);
                }
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
            // Debug.Log(packagePath + " BundleRef:     asset:" + _assetRef.Count + "     package:" + _packageRef.Count);
            return (_assetRef.Count > 0 || _packageRef.Count > 0);
        }

        public bool AddPackageRef(string package)
        {
            return _packageRef.Add(package);
        }

        public bool RemovePackageRef(string package)
        {
            return _packageRef.Remove(package);
        }


        public override void UnloadPackage()
        {
            base.UnloadPackage();
            m_AssetBundle.Unload(true);
            _packageRef.Clear();
            _assetRef.Clear();
            m_AssetBundle = null;
        }
    }
}