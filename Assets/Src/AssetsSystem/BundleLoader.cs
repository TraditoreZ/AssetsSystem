using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Object = UnityEngine.Object;
using TF.AssetSystem;
internal class BundleLoader : IAssetLoader
{
    private string root;
    public static AssetBundleManifest allManifest;
    public void Initialize(string root)
    {
        this.root = root;
        AssetBundle assetBundle = AssetBundle.LoadFromFile(AssetBundlePathResolver.instance.GetBundleFileRuntime(AssetBundlePathResolver.instance.BundleSaveDirName));
        allManifest = assetBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
        foreach (var item in allManifest.GetAllAssetBundles())
        {
            Debug.Log("init bundle:" + item);
        }
    }

    private bool IsPackageCreated(string path)
    {
        throw new NotImplementedException();
    }

    public Object Load(string path)
    {
        throw new NotImplementedException();
    }

    public Object[] LoadAll(string packagePath)
    {
        throw new NotImplementedException();
    }

    public void LoadAllAsync(string path, Action<Object[]> callback)
    {
        throw new NotImplementedException();
    }

    public void LoadAsync(string path, Action<Object> callback)
    {
        throw new NotImplementedException();
    }

    public void Unload(string path)
    {
        throw new NotImplementedException();
    }

    public void Unload(Object obj)
    {
        throw new NotImplementedException();
    }

    public void UnloadAll(string packagePath)
    {
        throw new NotImplementedException();
    }

    public void Destory()
    {
        throw new NotImplementedException();
    }

    private string GetPackageName(string path)
    {
        throw new NotImplementedException();
    }

}
