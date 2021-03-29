using System;
using System.Security.Cryptography;
using System.Text;

public class CoreEncryption
{
    //AES

    //加密
    public static string Encryption(string express, string key)
    {
        if (string.IsNullOrEmpty(express)) return null;
        Byte[] toEncryptArray = Encoding.UTF8.GetBytes(express);

        RijndaelManaged rm = new RijndaelManaged
        {
            Key = Encoding.UTF8.GetBytes(key),
            Mode = CipherMode.ECB,
            Padding = PaddingMode.PKCS7
        };
        ICryptoTransform cTransform = rm.CreateEncryptor();
        Byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
        return Convert.ToBase64String(resultArray, 0, resultArray.Length);
    }

    public static byte[] Encryption(byte[] encryptdata, string key)
    {
        if (encryptdata == null || encryptdata.Length == 0) return null;
        RijndaelManaged rm = new RijndaelManaged
        {
            Key = Encoding.UTF8.GetBytes(key),
            Mode = CipherMode.ECB,
            Padding = PaddingMode.PKCS7
        };
        ICryptoTransform cTransform = rm.CreateEncryptor();
        return cTransform.TransformFinalBlock(encryptdata, 0, encryptdata.Length);
    }

    //解密
    public static string Decrypt(string ciphertext, string key)
    {
        if (string.IsNullOrEmpty(ciphertext)) return null;
        Byte[] toEncryptArray = Convert.FromBase64String(ciphertext);
        RijndaelManaged rm = new RijndaelManaged
        {
            Key = Encoding.UTF8.GetBytes(key),
            Mode = CipherMode.ECB,
            Padding = PaddingMode.PKCS7
        };
        ICryptoTransform cTransform = rm.CreateDecryptor();
        Byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
        return Encoding.UTF8.GetString(resultArray);
    }

    public static byte[] Decrypt(byte[] encryptdata, string key)
    {
        RijndaelManaged rm = new RijndaelManaged
        {
            Key = Encoding.UTF8.GetBytes(key),
            Mode = CipherMode.ECB,
            Padding = PaddingMode.PKCS7
        };
        ICryptoTransform cTransform = rm.CreateDecryptor();
        return cTransform.TransformFinalBlock(encryptdata, 0, encryptdata.Length);
    }
}