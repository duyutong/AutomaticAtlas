using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class EditorUtilityExtensions
{
    public static void CheckRes(string path, string extension, Action<string> action = null)
    {
        if (string.IsNullOrEmpty(path)) return;
        if (File.Exists(path))
        {
            FileInfo fileInfo = new FileInfo(path);
            action?.Invoke(fileInfo.FullName);
        }
        else
        {
            string[] vs = Directory.GetDirectories(path);
            foreach (string v in vs) { CheckRes(v, extension, action); }
            DirectoryInfo directory = Directory.CreateDirectory(path);
            FileInfo[] fileInfos = directory.GetFiles();
            foreach (FileInfo info in fileInfos)
            {
                if (string.IsNullOrEmpty(info.FullName)) continue;
                if (info.Extension != extension) continue;
                action?.Invoke(info.FullName);
            }
        }
    }
    public static string ToShortPath(string fullPath)
    {
        string dataPath = Application.dataPath.Replace("/", @"\") + @"\";
        string shortPath = "Assets" + @"\" + fullPath.Replace(dataPath, "");
        return shortPath;
    }
}

