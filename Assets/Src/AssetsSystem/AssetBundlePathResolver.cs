using System.IO;
using UnityEngine;

namespace TF.AssetSystem
{
    /// <summary>
    /// AB 打包及运行时路径解决器
    /// </summary>
    public class AssetBundlePathResolver
    {
        private static AssetBundlePathResolver m_instance;
        public static AssetBundlePathResolver instance
        {
            get
            {
                if (m_instance == null)
                {
                    m_instance = new AssetBundlePathResolver();
                }
                return m_instance;
            }
        }

        public AssetBundlePathResolver()
        {
            m_instance = this;
        }

        /// <summary>
        /// AB 保存的路径相对于 Assets/StreamingAssets 的名字
        /// </summary>
        public virtual string BundleSaveDirName { get { return "AssetBundles"; } }

#if UNITY_EDITOR
        /// <summary>
        /// AB 保存的路径
        /// </summary>
        public string BundleSavePath { get { return "Assets/StreamingAssets/" + BundleSaveDirName; } }
        /// <summary>
        /// AB打包的原文件HashCode要保存到的路径，下次可供增量打包
        /// </summary>
        public virtual string HashCacheSaveFile { get { return "Assets/AssetBundles/cache.txt"; } }
        /// <summary>
        /// 在编辑器模型下将 abName 转为 Assets/... 路径
        /// 这样就可以不用打包直接用了
        /// </summary>
        /// <param name="abName"></param>
        /// <returns></returns>
        public virtual string GetEditorModePath(string abName)
        {
            //将 Assets.AA.BB.prefab 转为 Assets/AA/BB.prefab
            abName = abName.Replace(".", "/");
            int last = abName.LastIndexOf("/");

            if (last == -1)
                return abName;

            string path = string.Format("{0}.{1}", abName.Substring(0, last), abName.Substring(last + 1));
            return path;
        }
#endif

        /// <summary>
        /// 获取 AB 源文件路径（打包进安装包的）
        /// </summary>
        /// <param name="path"></param>
        /// <param name="forWWW"></param>
        /// <returns></returns>
        public virtual string GetBundleSourceFile(string path = "", bool forWWW = false)
        {
            string filePath = null;
#if UNITY_EDITOR
            if (forWWW)
                filePath = string.Format("file://{0}/StreamingAssets/{1}/{2}", Application.dataPath, BundleSaveDirName, path);
            else
                filePath = string.Format("{0}/StreamingAssets/{1}/{2}", Application.dataPath, BundleSaveDirName, path);
#elif UNITY_ANDROID
            if (forWWW)
                filePath = string.Format("jar:file://{0}!/assets/{1}/{2}", Application.dataPath, BundleSaveDirName, path);
            else
                filePath = string.Format("{0}!assets/{1}/{2}", Application.dataPath, BundleSaveDirName, path);
#elif UNITY_IOS
            if (forWWW)
                filePath = string.Format("file://{0}/Raw/{1}/{2}", Application.dataPath, BundleSaveDirName, path);
            else
                filePath = string.Format("{0}/Raw/{1}/{2}", Application.dataPath, BundleSaveDirName, path);
#endif
            return filePath;
        }

        public virtual string GetBundlePersistentFile(string path = "", bool forWWW = false)
        {
            return string.Format("{0}/{1}/{2}", Application.persistentDataPath, BundleSaveDirName, path);
        }


        /// <summary>
        /// 获取 AB 运行时文件路径  优先寻找Persistent目录下
        /// </summary>
        /// <param name="path"></param>
        /// <param name="forWWW"></param>
        /// <returns></returns>
        public virtual string GetBundleFileRuntime(string path = "", bool forWWW = false)
        {
            string PerPath = GetBundlePersistentFile(path, forWWW);
            return File.Exists(PerPath) ? PerPath : GetBundleSourceFile(path, forWWW);
        }

        /// <summary>
        /// AB 依赖信息文件名
        /// </summary>
        public virtual string DependFileName { get { return "dep.all"; } }

        DirectoryInfo cacheDir;

        /// <summary>
        /// 用于缓存AB的目录，要求可读写
        /// </summary>
        public virtual string BundleCacheDir
        {
            get
            {
                if (cacheDir == null)
                {
#if UNITY_EDITOR
                    string dir = string.Format("{0}/{1}", Application.streamingAssetsPath, BundleSaveDirName);
#else
					string dir = string.Format("{0}/{1}", Application.persistentDataPath, BundleSaveDirName);
#endif
                    cacheDir = new DirectoryInfo(dir);
                    if (!cacheDir.Exists)
                        cacheDir.Create();
                }
                return cacheDir.FullName;
            }
        }
    }
}