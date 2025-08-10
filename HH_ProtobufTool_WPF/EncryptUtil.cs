using System.Collections;
using System;
using System.IO;
using System.Management;
using System.Collections.Generic;

/// <summary>
/// 加密
/// </summary>
public sealed class EncryptUtil
{
    public static string Md5(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }
        System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
        byte[] bytResult = md5.ComputeHash(System.Text.Encoding.Default.GetBytes(value));
        string strResult = BitConverter.ToString(bytResult);
        strResult = strResult.Replace("-", "");
        return strResult;
    }

    /// <summary>
    /// 获取文件的MD5
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    public static string GetFileMD5(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            return null;
        }
        try
        {
            FileStream file = new FileStream(filePath, FileMode.Open);
            System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] bytResult = md5.ComputeHash(file);
            string strResult = BitConverter.ToString(bytResult);
            strResult = strResult.Replace("-", "");
            return strResult;
        }
        catch
        {
            return null;
        }
    }



    public static string R0ZlIjHRTRUEIYxaY()
    {
        ManagementClass mc = new ManagementClass("Win32_Processor");
        ManagementObjectCollection moc = mc.GetInstances();
        string strID1 = null;
        foreach (ManagementObject mo in moc)
        {
            strID1 = mo.Properties["ProcessorId"].Value.ToString().Trim();
            break;
        }

        mc = new ManagementClass("Win32_BaseBoard");
        moc = mc.GetInstances();
        string strID2 = null;
        foreach (ManagementObject mo in moc)
        {
            strID2 = mo.Properties["SerialNumber"].Value.ToString().Trim();
            break;
        }
        return strID1 + "PT" + strID2;
    }

    public static string R0ZlIjHRTRUEIYxaYMM(string WERWER, string EIYx, string DFS)
    {
        List<char> lst = new List<char>();
        char[] arr1 = WERWER.Trim().ToCharArray();
        char[] arr2 = EIYx.Trim().ToCharArray();
        char[] arr3 = DFS.Trim().ToCharArray();
        int index = 0;
        foreach (char v in arr2)
        {
            index++;
            if (index % 2 == 0)
            {
                lst.Add(v);
            }
        }
        foreach (char v in arr1)
        {
            index++;
            if (index % 2 == 0)
            {
                lst.Add(v);
            }
        }
        foreach (char v in arr3)
        {
            index++;
            if (index % 2 == 0)
            {
                lst.Add(v);
            }
        }
        lst.Sort();

        string md5 = "";
        foreach (char v in lst)
        {
            md5 += Md5(v.ToString());
        }

        return md5;
    }
}