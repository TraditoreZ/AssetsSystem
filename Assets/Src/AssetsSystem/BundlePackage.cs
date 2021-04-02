using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
public class BundlePackage : BaseAssetPackage<BundlePackage>
{
    public override Object Load(string path)
    {
        throw new NotImplementedException();
    }

    public override Object[] LoadAll()
    {
        throw new NotImplementedException();
    }

    public override void LoadAllAsync(Action<Object[]> callback)
    {
        throw new NotImplementedException();
    }

    public override void LoadAsync(string path, Action<Object> callback)
    {
        throw new NotImplementedException();
    }

    public override bool PackageLoaded()
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

    public override void UnloadAll()
    {
        throw new NotImplementedException();
    }
}
