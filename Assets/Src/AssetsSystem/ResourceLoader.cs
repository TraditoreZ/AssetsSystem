using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
public class ResourceLoader : IAssetLoader
{
    private Dictionary<string, IAssetPackage> packMapping;
    public void Initialize(string root)
    {
        packMapping = new Dictionary<string, IAssetPackage>();
    }


    public void Destory()
    {

    }


    public Object Load(string path)
    {
        string packagePath = GetPackageName(path);
        return LoadPackage(packagePath, false).Load(path);
    }

    public Object[] LoadAll(string path)
    {
        return LoadPackage(path, false).LoadAll();
    }


    public void LoadAsync(string path, Action<Object> callback)
    {
        string packagePath = GetPackageName(path);
        LoadPackage(packagePath, true).LoadAsync(path, callback);
    }

    public void LoadAllAsync(string path, Action<Object[]> callback)
    {
        LoadPackage(path, true).LoadAllAsync(callback);
    }

    public void Unload(string path)
    {
        string packagePath = GetPackageName(path);
        if (packMapping.ContainsKey(packagePath))
        {
            IAssetPackage package = LoadPackage(packagePath, false);
            package.Unload(path);
            if (package.LoadCount() == 0)
            {
                UnloadPackage(packagePath);
            }
        }
    }

    public void Unload(Object obj)
    {
        IAssetPackage package = null;
        // TODO  如性能开销过高再cash优化
        foreach (var tempPack in packMapping.Values)
        {
            if (tempPack.IsLoaded(obj.name))
            {
                package = tempPack;
            }
        }
        if (package != null)
        {
            package.Unload(obj);
            if (package.LoadCount() == 0)
            {
                UnloadPackage(package.PackagePath());
            }
        }
    }

    public void UnloadAll(string packagePath)
    {
        if (packMapping.ContainsKey(packagePath))
        {
            LoadPackage(packagePath, false).UnloadAll();
            UnloadPackage(packagePath);
        }
    }

    private IAssetPackage LoadPackage(string packagePath, bool asyncLoaded)
    {
        IAssetPackage package;
        if (!packMapping.TryGetValue(packagePath, out package))
        {
            package = ResourcePackage.CreateObject();
            packMapping.Add(packagePath, package);
            package.LoadPackage(packagePath, asyncLoaded);
        }
        return package;
    }

    private void UnloadPackage(string packagePath)
    {
        IAssetPackage package;
        if (packMapping.TryGetValue(packagePath, out package))
        {
            packMapping.Remove(packagePath);
            package.UnloadPackage();
            ResourcePackage.ReclaimObject(package as ResourcePackage);
        }
    }

    private string GetPackageName(string path)
    {
        return path.Substring(0, path.LastIndexOf('/'));
    }

}
