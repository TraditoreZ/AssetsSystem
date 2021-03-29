using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class SystemTools
{
    /// <summary>获取引用类型的内存地址方法</summary>
    /// <returns>string: 内存地址</returns>
    public static string GetMemory(object o)
    {
        GCHandle h = GCHandle.Alloc(o, GCHandleType.WeakTrackResurrection);

        IntPtr addr = GCHandle.ToIntPtr(h);

        return "0x" + addr.ToString("X");
    }


    /// <summary>获取时间戳</summary>
    /// <returns>long: 1970年时间戳</returns>  
    public static long GetTimeStamp()
    {
        TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
        return Convert.ToInt64(ts.TotalMilliseconds);
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
