using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

public class AutomaticAtlas
{
    public static string configPath = "Assets/AutomaticAtlas/Editor/AtlasConfig.asset";
    private static List<string> prefabPathList = new List<string>();
    private static AtlasConfig config;
    private static string atlasName_Comm = "Common";
    private static string atlasName_Comm_big = "Common_big";
    private static Dictionary<string, List<Sprite>> altasDic = new Dictionary<string, List<Sprite>>();
    private static Dictionary<string, TextureResInfo> repeatResDic = new Dictionary<string, TextureResInfo>();
    private static Dictionary<string, HashSet<MergeInfo>> mergeDic = new Dictionary<string, HashSet<MergeInfo>>();

    [MenuItem("Tools/GenerateAtlas")]
    private static void GenerateAtlas()
    {
        if (config == null) config = AssetDatabase.LoadAssetAtPath<AtlasConfig>(configPath);//  Resources.Load<ScriptableObject>("AtlasConfig") as AtlasConfig;
        atlasName_Comm = config.atlasName_Comm;
        atlasName_Comm_big = config.atlasName_Comm_big;

        altasDic.Clear();
        altasDic.Add(atlasName_Comm, new List<Sprite>());
        altasDic.Add(atlasName_Comm_big, new List<Sprite>());

        repeatResDic.Clear();
        mergeDic.Clear();

        CheckRes(config.checkAssetsPath, ".prefab", (_path) => { prefabPathList.Add(_path); });
        SetRepeatResDic();
        MergeTexResForPrefab();
        SetAltasDic();
        OnGenerateAtlas();
    }
    private static void OnGenerateAtlas()
    {
        foreach (KeyValuePair<string, List<Sprite>> keyValuePair in altasDic)
        {
            string atlasName = keyValuePair.Key;
            List<Sprite> sprites = keyValuePair.Value;
            if (sprites.Count == 0) continue;

            string atlasPath = config.outputDirectory + "/" + atlasName + ".spriteatlas";
            SpriteAtlas atlas = new SpriteAtlas(); // 创建新的SpriteAtlas
            atlas.Add(sprites.ToArray()); // 将纹理数组添加到SpriteAtlas


            // 保存SpriteAtlas
            AssetDatabase.CreateAsset(atlas, atlasPath);
            AssetDatabase.SaveAssets();

            Debug.Log("图集生成完成，保存在：" + atlasPath);
        }

        repeatResDic.Clear();
        mergeDic.Clear();
        altasDic.Clear();
        // 刷新资源
        AssetDatabase.Refresh();
    }
    private static void MergeTexResForPrefab()
    {
        foreach (KeyValuePair<string, HashSet<MergeInfo>> keyValuePair in mergeDic)
        {
            string shortPath = keyValuePair.Key;
            string fileStrInfo = File.ReadAllText(shortPath);
            List<MergeInfo> merges = keyValuePair.Value.Distinct().ToList();
            foreach (MergeInfo mergeInfo in merges)
            {
                //开始合并
                string currGuid = mergeInfo.currGuid;
                string targetGuid = mergeInfo.targetGuid;
                fileStrInfo = fileStrInfo.Replace(currGuid, targetGuid);
            }

            //重置预制体依赖
            if (File.Exists(shortPath)) File.Delete(shortPath);
            using (FileStream file = File.Create(shortPath))
            {
                byte[] bytes = Encoding.UTF8.GetBytes(fileStrInfo);
                file.Write(bytes, 0, bytes.Length);
                file.Flush();
                file.Close();
            }
        }
    }

    private static void SetAltasDic()
    {
        List<string> atlasNames = altasDic.Keys.ToList();
        foreach (string atlasName in atlasNames)
        {
            foreach (KeyValuePair<string, TextureResInfo> keyValuePair in repeatResDic)
            {
                TextureResInfo info = keyValuePair.Value;
                if (info.altasName != atlasName) continue;

                Sprite sprite = info.sprite;
                altasDic[atlasName].Add(sprite);
            }
        }
    }
    private static void SetRepeatResDic()
    {
        string dataPath = Application.dataPath.Replace("/", @"\") + @"\";
        for (int i = 0; i < prefabPathList.Count; i++)
        {
            int index = i;
            string _path = prefabPathList[index];
            string shortPath = "Assets" + @"\" + _path.Replace(dataPath, "");
            UnityEngine.Object checkObj = AssetDatabase.LoadAssetAtPath(shortPath, typeof(UnityEngine.Object));

            SetRepeatResDic(checkObj, shortPath);
        }
    }
    private static string GetPrefabFolderName(string prefabPath)
    {
        FileInfo prefabFileInfo = new FileInfo(prefabPath);
        if (prefabFileInfo.Exists)
        {
            DirectoryInfo prefabDirectory = prefabFileInfo.Directory;
            if (prefabDirectory != null)
            {
                return prefabDirectory.Name;
            }
            else
            {
                return "Invalid Directory";
            }
        }
        else
        {
            return "File not found";
        }
    }
    private static void SetRepeatResDic(UnityEngine.Object targetObject, string shortPath)
    {
        string atlasName = GetPrefabFolderName(shortPath);
        string atlasName_big = atlasName + "_big";
        string checkTypeName = typeof(Sprite).FullName;

        if (!altasDic.ContainsKey(atlasName)) altasDic.Add(atlasName, new List<Sprite>());
        if (!altasDic.ContainsKey(atlasName_big)) altasDic.Add(atlasName_big, new List<Sprite>());
        if (targetObject != null)
        {
            UnityEngine.Object[] dependencies = EditorUtility.CollectDependencies(new UnityEngine.Object[] { targetObject });
            foreach (UnityEngine.Object dependency in dependencies)
            {
                if (!dependency.GetType().FullName.Contains(checkTypeName)) continue;
                string filePath = AssetDatabase.GetAssetPath(dependency);
                if (!File.Exists(filePath)) continue;
                string md5 = GetMD5(filePath);
                Sprite sprite = dependency as Sprite;
                bool isExist = repeatResDic.ContainsKey(md5);
                bool isBig = sprite.rect.width >= config.maxSize || sprite.rect.height >= config.maxSize;
                bool isCommon = isExist ? repeatResDic[md5].altasName != (isBig ? atlasName_big : atlasName) : false;
                if (isCommon)
                {
                    repeatResDic[md5].altasName = isBig ? atlasName_Comm_big : atlasName_Comm;

                    //处理合并信息
                    string currGuid = AssetDatabase.AssetPathToGUID(filePath);
                    string targetGuid = AssetDatabase.AssetPathToGUID(repeatResDic[md5].path);
                    CheckMerge(shortPath, currGuid, targetGuid);
                }
                else if (!isExist)
                {
                    repeatResDic.Add(md5, new TextureResInfo() { sprite = sprite, md5 = md5, path = filePath });
                    repeatResDic[md5].altasName = isBig ? atlasName_big : atlasName;
                }
            }
        }
        else
        {
            Debug.LogWarning("No game object selected.");
        }
    }
    private static void CheckMerge(string prefabPath, string currGuid, string targetGuid)
    {
        if (currGuid == targetGuid) return;
        if (!mergeDic.ContainsKey(prefabPath)) mergeDic.Add(prefabPath, new HashSet<MergeInfo>());
        mergeDic[prefabPath].Add(new MergeInfo() { currGuid = currGuid, targetGuid = targetGuid });
    }
    public static string GetMD5(string filePath)
    {
        using (FileStream file = File.OpenRead(filePath))
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] bytes = md5.ComputeHash(file);
            file.Close();
            StringBuilder stringBuilder = new StringBuilder();
            foreach (byte b in bytes) stringBuilder.Append(b.ToString("x2"));

            return stringBuilder.ToString();
        }
    }
    private static void CheckRes(string path, string extension, Action<string> action = null)
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
}
public class TextureResInfo
{
    public string altasName;
    public Sprite sprite;
    public string md5;
    public string path;
}
public class MergeInfo
{
    public string currGuid;
    public string targetGuid;
}
