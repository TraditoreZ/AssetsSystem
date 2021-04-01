#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class AdbPackage : ObjectPool<AdbPackage>, IAssetPackage
{
    // AssetDataBase为虚拟包概念
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
            GetAssetSuffix(packagePath, path);
            targer = AssetDatabase.LoadAssetAtPath<Object>(GetAssetSuffix(packagePath, path));
            assetMapping.Add(path, targer);
        }
        return targer;
    }

    public Object[] LoadAll()
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(packagePath);
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
        AssetSystemCore.Instance.StartCoroutine(IELoadAsync(path, callback));
    }


    private IEnumerator IELoadAsync(string path, Action<Object> callback)
    {
        // 模拟延迟
        yield return new WaitForSeconds(AssetSystemCore.Instance.SimulateIODelay ? UnityEngine.Random.Range(0, 1f) : 0);
        callback?.Invoke(Load(path));
    }


    public void LoadAllAsync(Action<Object[]> callback)
    {
        AssetSystemCore.Instance.StartCoroutine(IELoadAllAsync(callback));
    }

    private IEnumerator IELoadAllAsync(Action<Object[]> callback)
    {
        // 模拟延迟
        yield return new WaitForSeconds(AssetSystemCore.Instance.SimulateIODelay ? UnityEngine.Random.Range(0, 1f) : 0);
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

    private string GetAssetSuffix(string packagePath, string assetPath)
    {
        var files = Directory.GetFiles(packagePath.Replace("Assets", Application.dataPath), Path.GetFileName(assetPath) + ".*");
        if (files != null)
        {
            string suffixPath = assetPath + "." + Path.GetFileName(files.Where(s => !s.EndsWith(".meta")).First()).Split('.')[1];
            return suffixPath;
        }
        else
        {
            return assetPath;
        }

    }

}
#endif