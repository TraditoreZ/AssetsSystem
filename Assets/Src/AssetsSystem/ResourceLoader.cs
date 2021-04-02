using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
public class ResourceLoader : BaseAssetLoader
{
    public override void Initialize(string root)
    {
        base.Initialize(root);
    }

    public override void Destory()
    {

        base.Destory();
    }

    public override Object Load(string path)
    {
        string packagePath = GetPackageName(path);
        return LoadPackage(packagePath).Load(path);
    }

    public override Object[] LoadAll(string path)
    {
        return LoadPackage(path).LoadAll();
    }


    public override void LoadAsync(string path, Action<Object> callback)
    {
        string packagePath = GetPackageName(path);
        LoadPackage(packagePath).LoadAsync(path, callback);
    }

    public override void LoadAllAsync(string path, Action<Object[]> callback)
    {
        LoadPackage(path).LoadAllAsync(callback);
    }

    public override void Unload(string path)
    {
        string packagePath = GetPackageName(path);
        if (packMapping.ContainsKey(packagePath))
        {
            IAssetPackage package = LoadPackage(packagePath);
            package.Unload(path);
            if (package.LoadCount() == 0)
            {
                UnloadPackage(packagePath);
            }
        }
    }

    public override void Unload(Object obj)
    {
        IAssetPackage package = null;
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

    public override void UnloadAll(string packagePath)
    {
        if (packMapping.ContainsKey(packagePath))
        {
            LoadPackage(packagePath).UnloadAll();
            UnloadPackage(packagePath);
        }
    }

    protected override string GetPackageName(string path)
    {
        return path.Substring(0, path.LastIndexOf('/'));
    }

    protected override IAssetPackage CreatePackage() { return ResourcePackage.CreateObject(); }

    protected override void DestoryPackage(IAssetPackage package) { ResourcePackage.ReclaimObject(package as ResourcePackage); }

}
