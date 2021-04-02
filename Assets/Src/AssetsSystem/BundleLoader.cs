using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Object = UnityEngine.Object;
using TF.AssetSystem;
internal class BundleLoader : BaseAssetLoader
{
    public static AssetBundleManifest allManifest;

    public override void Initialize(string root)
    {
        base.Initialize(root);
        AssetBundle assetBundle = AssetBundle.LoadFromFile(AssetBundlePathResolver.instance.GetBundleFileRuntime(AssetBundlePathResolver.instance.GetBundlePlatformRuntime()));
        allManifest = assetBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
        foreach (var item in allManifest.GetAllAssetBundles())
        {
            Debug.Log("init bundle:" + item);
        }
    }



    public override Object Load(string path)
    {
        throw new NotImplementedException();
    }

    public override Object[] LoadAll(string packagePath)
    {
        throw new NotImplementedException();
    }

    public override void LoadAllAsync(string path, Action<Object[]> callback)
    {
        throw new NotImplementedException();
    }

    public override void LoadAsync(string path, Action<Object> callback)
    {
        throw new NotImplementedException();
    }

    public override void Unload(string path)
    {
        throw new NotImplementedException();
    }

    public override void Unload(Object obj)
    {
        throw new NotImplementedException();
    }

    public override void UnloadAll(string packagePath)
    {
        throw new NotImplementedException();
    }

    public override void Destory()
    {
        base.Destory();
    }

    protected override string GetPackageName(string path)
    {
        throw new NotImplementedException();
    }

    protected override IAssetPackage CreatePackage() { return BundlePackage.CreateObject(); }

    protected override void DestoryPackage(IAssetPackage package) { BundlePackage.ReclaimObject(package as BundlePackage); }

}
