using System.IO;
using UnityEngine;

namespace AssetSystem
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
        /// AB 保存的路径的名字
        /// </summary>
        public virtual string BundleSaveDirName { get { return "AssetBundles"; } }
        /// <summary>
        /// 取AB后缀
        /// </summary>
        public virtual string BundleSuffix { get { return ".ab"; } }
        /// <summary>
        /// Bundle增量变化表名称
        /// </summary>
        public virtual string BundleBillName { get { return "bundlebill.cfg"; } }

#if UNITY_EDITOR
        public static string GetBundlePlatformOutput(UnityEditor.BuildTarget buildTarget)
        {
            string target = "Bundle";
            switch (buildTarget)
            {
                case UnityEditor.BuildTarget.StandaloneWindows64:
                    target = "Windows";
                    break;
                case UnityEditor.BuildTarget.Android:
                    target = "Android";
                    break;
                case UnityEditor.BuildTarget.iOS:
                    target = "IOS";
                    break;
            }
            return target;
        }

        public static string BundleOutputPath(UnityEditor.BuildTarget buildTarget)
        {
            return string.Format("{0}/../{1}/{2}", Application.dataPath, AssetBundlePathResolver.instance.BundleSaveDirName, GetBundlePlatformOutput(buildTarget));
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
#if UNITY_EDITOR || UNITY_STANDALONE
            if (forWWW)
                filePath = string.Format("file://{0}/StreamingAssets/{1}/{2}/{3}", Application.dataPath, BundleSaveDirName, GetBundlePlatformRuntime(), path);
            else
                filePath = string.Format("{0}/StreamingAssets/{1}/{2}/{3}", Application.dataPath, BundleSaveDirName, GetBundlePlatformRuntime(), path);
#elif UNITY_ANDROID
            if (forWWW)
                filePath = string.Format("jar:file://{0}!/assets/{1}/{2}/{3}", Application.dataPath, BundleSaveDirName, GetBundlePlatformRuntime(), path);
            else
                filePath = string.Format("{0}!assets/{1}/{2}/{3}", Application.dataPath, BundleSaveDirName, GetBundlePlatformRuntime(), path);
#elif UNITY_IOS
            if (forWWW)
                filePath = string.Format("file://{0}/Raw/{1}/{2}/{3}", Application.dataPath, BundleSaveDirName, GetBundlePlatformRuntime(), path);
            else
                filePath = string.Format("{0}/Raw/{1}/{2}/{3}", Application.dataPath, BundleSaveDirName, GetBundlePlatformRuntime(), path);
#endif
            return filePath;
        }

        public virtual string GetBundlePersistentFile(string path = "", bool forWWW = false)
        {
            if (forWWW)
                return string.Format("file://{0}/{1}/{2}/{3}", Application.persistentDataPath, BundleSaveDirName, GetBundlePlatformRuntime(), path);
            else
                return string.Format("{0}/{1}/{2}/{3}", Application.persistentDataPath, BundleSaveDirName, GetBundlePlatformRuntime(), path);
        }


        /// <summary>
        /// 获取 AB 运行时文件路径  优先寻找Persistent目录下
        /// </summary>
        /// <param name="path"></param>
        /// <param name="forWWW"></param>
        /// <returns></returns>
        public virtual string GetBundleFileRuntime(string path = "", bool forWWW = false)
        {
            return File.Exists(GetBundlePersistentFile(path, false)) ? GetBundlePersistentFile(path, forWWW) : GetBundleSourceFile(path, forWWW);
        }

        public virtual string GetBundlePlatformRuntime()
        {
            string path = "Bundle";
#if UNITY_EDITOR && !UNITY_STANDALONE
            path = GetBundlePlatformOutput(UnityEditor.EditorUserBuildSettings.activeBuildTarget);
#elif UNITY_STANDALONE
            path = "Windows";
#elif UNITY_ANDROID
            path = "Android";
#elif UNITY_IOS
            path = "IOS";
#endif
            return path;
        }

        DirectoryInfo cacheDir;

        /// <summary>
        /// 缓存AB目录，要求可读写
        /// </summary>
        public virtual string BundleCacheDir
        {
            get
            {
                if (cacheDir == null)
                {
#if UNITY_EDITOR
                    string dir = string.Format("{0}/{1}/{2}", Application.persistentDataPath, BundleSaveDirName, GetBundlePlatformRuntime());
#else
                    string dir = string.Format("{0}/{1}/{2}", Application.persistentDataPath, BundleSaveDirName, GetBundlePlatformRuntime());
#endif
                    cacheDir = new DirectoryInfo(dir);
                    if (!cacheDir.Exists)
                        cacheDir.Create();
                }
                return cacheDir.FullName;
            }
        }

        /// <summary>获取bundle 唯一 hash</summary>
        /// <returns>string: 唯一值(64) = 文件名称md5(32) + bundleMD5(32)</returns>
        public static string GetBundleUniqueHashName(string bundle, string assetBundleHash)
        {
            return GetMD5(bundle) + assetBundleHash + Path.GetExtension(bundle);
        }

        /// <summary>获取MD5码</summary>
        /// <returns>string: MD5</returns>
        public static string GetMD5(string sDataIn)
        {
            System.Security.Cryptography.MD5CryptoServiceProvider md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] bytValue, bytHash;
            bytValue = System.Text.Encoding.UTF8.GetBytes(sDataIn);
            bytHash = md5.ComputeHash(bytValue);
            md5.Clear();
            string sTemp = "";
            for (int i = 0; i < bytHash.Length; i++)
            {
                sTemp += bytHash[i].ToString("X").PadLeft(2, '0');
            }
            return sTemp.ToLower();
        }
    }
}