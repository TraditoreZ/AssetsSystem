#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public class AdbLoader : IAssetLoader
{
    // AssetDatabase用于编辑器预览
    private Dictionary<string, IAssetPackage> packMapping;
    private string root;
    public void Initialize(string root)
    {
        this.root = root;
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
        string packagePath = GetPackageName(path);
        return LoadPackage(packagePath, false).LoadAll();
    }


    public void LoadAsync(string path, Action<Object> callback)
    {
        string packagePath = GetPackageName(path);
        LoadPackage(packagePath, true).LoadAsync(path, callback);
    }

    public void LoadAllAsync(string path, Action<Object[]> callback)
    {
        string packagePath = GetPackageName(path);
        LoadPackage(packagePath, true).LoadAllAsync(callback);
    }

    public void Unload(string path)
    {
        string packagePath = GetPackageName(path);
        if (IsPackageCreated(packagePath))
        {
            IAssetPackage package = LoadPackage(packagePath, false);
            package.Unload(root + path);
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
        packagePath = GetPackageName(packagePath);
        if (IsPackageCreated(packagePath))
        {
            LoadPackage(packagePath, false).UnloadAll();
            UnloadPackage(packagePath);
        }
    }

    public bool IsPackageCreated(string path)
    {
        return packMapping.ContainsKey(root + path);
    }

    private IAssetPackage LoadPackage(string packagePath, bool asyncLoaded)
    {
        IAssetPackage package;
        packagePath = GetPackageName(packagePath);
        if (!packMapping.TryGetValue(packagePath, out package))
        {
            package = AdbPackage.CreateObject();
            packMapping.Add(packagePath, package);
            package.LoadPackage(packagePath, asyncLoaded);
        }
        return package;
    }

    private void UnloadPackage(string packagePath)
    {
        IAssetPackage package;
        packagePath = GetPackageName(packagePath);
        if (packMapping.TryGetValue(packagePath, out package))
        {
            packMapping.Remove(packagePath);
            package.UnloadPackage();
            AdbPackage.ReclaimObject(package as AdbPackage);
        }
    }

    public string GetPackageName(string path)
    {
        return path.Substring(0, path.LastIndexOf('/'));
    }

}
#endif