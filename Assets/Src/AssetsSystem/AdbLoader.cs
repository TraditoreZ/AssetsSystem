#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
using System.IO;

public class AdbLoader : BaseAssetLoader
{
    // AssetDatabase用于编辑器预览
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
        path = CombinePath(root, path);
        string packagePath = GetPackageName(path);
        return LoadPackage(packagePath).Load(path);
    }

    public override Object[] LoadAll(string path)
    {
        path = CombinePath(root, path);
        string packagePath = GetPackageName(path);
        return LoadPackage(packagePath).LoadAll();
    }


    public override void LoadAsync(string path, Action<Object> callback)
    {
        path = CombinePath(root, path);
        string packagePath = GetPackageName(path);
        LoadPackage(packagePath).LoadAsync(path, callback);
    }

    public override void LoadAllAsync(string path, Action<Object[]> callback)
    {
        path = CombinePath(root, path);
        string packagePath = GetPackageName(path);
        LoadPackage(packagePath).LoadAllAsync(callback);
    }

    public override void Unload(string path)
    {
        path = CombinePath(root, path);
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
        packagePath = CombinePath(root, packagePath);
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

    protected override IAssetPackage CreatePackage() { return AdbPackage.CreateObject(); }

    protected override void DestoryPackage(IAssetPackage package) { AdbPackage.ReclaimObject(package as AdbPackage); }

    private string CombinePath(string p1, string p2)
    {
        return System.IO.Path.Combine(p1, p2).Replace('\\', '/');
    }

}
#endif