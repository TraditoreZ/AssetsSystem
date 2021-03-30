using System;
using Object = UnityEngine.Object;
public interface IAssetPackage
{
    string PackagePath();
    bool PackageLoaded();
    void LoadPackage(string packagePath, bool async);
    void UnloadPackage();
    Object Load(string path);
    Object[] LoadAll();
    void LoadAsync(string path, Action<Object> callback);
    void LoadAllAsync(Action<Object[]> callback);
    void Unload(string path);
    void Unload(Object obj);
    void UnloadAll();
    bool IsLoaded(string path);
    int LoadCount();
}