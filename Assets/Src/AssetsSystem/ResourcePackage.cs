using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
public class ResourcePackage : ObjectPool<ResourcePackage>, IAssetPackage
{
    // Resource为虚拟包概念
    private string packagePath;
    private Dictionary<string, Object> assetMapping = new Dictionary<string, Object>();
    private HashSet<string> asyncLoading = new HashSet<string>();
    private Dictionary<string, HashSet<Action<Object>>> singleCallBackDic = new Dictionary<string, HashSet<Action<Object>>>();


    public bool IsLoaded(string path)
    {
        return assetMapping.ContainsKey(path);
    }

    public int LoadCount()
    {
        return assetMapping.Count;
    }

    public Object Load(string path)
    {
        Object targer;
        if (!assetMapping.TryGetValue(path, out targer))
        {
            targer = Resources.Load(path);
            assetMapping.Add(path, targer);
        }
        return targer;
    }

    public Object[] LoadAll()
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

    public void LoadAsync(string path, Action<Object> callback)
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

    public void LoadAllAsync(Action<Object[]> callback)
    {
        callback?.Invoke(LoadAll());
    }

    public void LoadPackage(string packagePath, bool async)
    {
        this.packagePath = packagePath;
        Debug.Log("[Asset Package] LoadPackage:" + packagePath);
    }

    public bool PackageLoaded()
    {
        return true;
    }

    public string PackagePath()
    {
        return packagePath;
    }

    public void Unload(string path)
    {
        Object targer;
        if (assetMapping.TryGetValue(path, out targer))
        {
            assetMapping.Remove(path);
            Unload(targer);
        }
    }

    public void Unload(Object obj)
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

    public void UnloadAll()
    {
        assetMapping.Clear();
        Resources.UnloadUnusedAssets();
    }

    public void UnloadPackage()
    {
        Debug.Log("[Asset Package] UnloadPackage:" + packagePath);
        UnloadAll();
    }

    private string GetAssetPath(string assetName)
    {
        return string.Format("{0}/{1}", packagePath, assetName);
    }
}