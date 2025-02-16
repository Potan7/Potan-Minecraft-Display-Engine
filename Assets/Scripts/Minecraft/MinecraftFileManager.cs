using System.IO;
using System.IO.Compression;
using UnityEngine;
using System.Threading;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Diagnostics;

public class MinecraftFileManager : RootManager
{
    static MinecraftFileManager instance;

    Dictionary<string, byte[]> textureFiles = new Dictionary<string, byte[]>();
    HashSet<string> isTextureAnimated = new HashSet<string>();
    public Dictionary<string, string> jsonFiles = new Dictionary<string, string>();

    // readPreReadedFiles에 있는 파일들은 미리 읽어둠
    Dictionary<string, MinecraftModelData> importantModels = new Dictionary<string, MinecraftModelData>();

    //readonly string[] readFolder = { "models", "textures", "blockstates", "items" }; // 읽을 폴더
    //readonly string[] readTexturesFolders = 
    //    { "block", "item", "entity/bed", "entity/shulker", "entity/chest", "entity/conduit", 
    //    "entity/creeper", "entity/zombie/zombie", "entity/skeleton/", "entity/piglin", "entity/player/wide/steve", "entity/enderdragon/dragon"}; // textures의 읽을 내용
    //readonly string[] readPreReadedFiles =
    //    {"block", "cube", "cube_all", "cube_all_inner_faces", "cube_column"};   // 미리 로드할 파일

    readonly string[] hardcodeNames = { "bed", "shulker_box", "chest", "conduit", "head" };

    readonly string Appdata = Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);

    const string minecraftPath = ".minecraft/versions";
    const string minecraftVersion = "1.21.4";

    [SerializeField]
    string filePath;

    protected override void Awake()
    {
        base.Awake();
        instance = this;
    }

    private void Start()
    {
        filePath = $"{Appdata}/{minecraftPath}/{minecraftVersion}/{minecraftVersion}.jar";
        new Thread(() => ReadJarFile(filePath, "assets/minecraft")).Start();
        //Thread thread = new Thread(() => ReadJarFile(filePath, "assets/minecraft"));
        //thread.Start();
    }

    #region Static 함수들

    public static JObject GetJSONData(string path)
    {
        if (path.Contains("bed") && !path.Contains("items"))
        {
            //CustomLog.Log("Bed: " + path);
            var bed = Resources.Load<TextAsset>("hardcoded/" + path.Replace(".json", ""));
            return JObject.Parse(bed.text);
        }

        if (instance.jsonFiles.ContainsKey(path))
        {
            return JObject.Parse(instance.jsonFiles[path]);
        }
        return null;
    }

    public static MinecraftModelData GetModelData(string path)
    {
        //CustomLog.Log("Get Model Data: " + path);

        if (instance.importantModels.ContainsKey(path))
        {
            return instance.importantModels[path];
        }

        for (int i = 0; i < instance.hardcodeNames.Length; i++)
        {
            if (path.Contains(instance.hardcodeNames[i]))
            {
                return JsonConvert.DeserializeObject<MinecraftModelData>(Resources.Load<TextAsset>("hardcoded/" + path.Replace(".json", "")).text);
            }
        }

        if (instance.jsonFiles.ContainsKey(path))
        {
            return  JsonConvert.DeserializeObject<MinecraftModelData>(instance.jsonFiles[path]);
        }
        return null;
    }

    public static Texture2D GetTextureFile(string path)
    {
        if (instance.textureFiles.ContainsKey(path))
        {
            Texture2D texture = new Texture2D(2, 2);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.alphaIsTransparency = true;
            texture.Apply();

            texture.LoadImage(instance.textureFiles[path]);
            
            return texture;
        }
        CustomLog.LogError("Texture not found: " + path);
        return null;
    }

    public static bool IsTextureAnimated(string path)
    {
        return instance.isTextureAnimated.Contains(path + ".mcmeta");
    }

    public static string RemoveNamespace(string path) => path.Replace("minecraft:", "");
    #endregion

    #region 파일 로드
    void ReadJarFile(string path, string targetFolder)
    {
        Stopwatch sw = new Stopwatch();
        sw.Start();

        ReadOnlySpan<string> readTexturesFolders = new[]
        {
        "textures/block", "textures/item", "textures/entity/bed", "textures/entity/shulker",
        "textures/entity/chest", "textures/entity/conduit", "textures/entity/creeper",
        "textures/entity/zombie/zombie", "textures/entity/skeleton/", "textures/entity/piglin",
        "textures/entity/player/wide/steve", "textures/entity/enderdragon/dragon"
        };

        ReadOnlySpan<string> readFolder = new[] { "models", "textures", "blockstates", "items" };

        ReadOnlySpan<string> readPreReadedFiles = new[]
        {"block", "cube", "cube_all", "cube_all_inner_faces", "cube_column"};   // 미리 로드할 파일

        CustomLog.Log($"Reading JAR file: {path}");
        if (!File.Exists(path))
        {
            CustomLog.LogError("File not found: " + path);
            return;
        }

        using (ZipArchive jarArchive = ZipFile.OpenRead(path))
            foreach (var entry in jarArchive.Entries)
            {
                if (!entry.FullName.StartsWith(targetFolder) || string.IsNullOrEmpty(entry.Name))
                    continue;

                // 파일 필터링
                string folderName = GetTopLevelFolder(entry.FullName, targetFolder);

                if (folderName == "textures")
                {
                    // textures 폴더 처리
                    if (!IsReadFolder(entry.FullName, readTexturesFolders)) continue; // 무시할 폴더 확인
                    if (entry.FullName.EndsWith(".png"))
                    {
                        //CustomLog.Log($"Found texture file: {entry.FullName}");
                        SavePNGFile(entry, entry.FullName);
                    }
                    else if (entry.FullName.EndsWith(".mcmeta"))
                    {
                        isTextureAnimated.Add(entry.FullName.Replace("assets/minecraft/", ""));
                        //CustomLog.Log("Animated texture: " + entry.FullName.Replace("assets/minecraft/", ""));
                    }
                    continue;
                }

                if (readFolder.IndexOf(folderName) > -1)
                {
                    // 다른 폴더 처리
                    if (entry.FullName.EndsWith(".json"))
                    {
                        SaveJson(entry, entry.FullName);
                        //CustomLog.Log($"Found JSON file: {entry.FullName}");
                    }
                }
            }

        CustomLog.Log("Finished reading JAR file");
        CustomLog.Log("Textures: " + textureFiles.Count);
        CustomLog.Log("JSON: " + jsonFiles.Count);

        // readImportantModels();
        foreach (var read in readPreReadedFiles)
        {
            string readPath = $"models/{read}.json";
            if (instance.jsonFiles.ContainsKey(readPath))
            {
                importantModels.Add(read, GetModelData(jsonFiles[readPath]));
            }
        }

        sw.Stop();
        CustomLog.Log($"Reading JAR file took {sw.ElapsedMilliseconds}ms");
    }

    // 최상위 폴더 이름 추출
    string GetTopLevelFolder(string fullPath, string targetFolder)
    {
        string relativePath = fullPath.Substring(targetFolder.Length + 1); // targetFolder 이후 경로
        int firstSlashIndex = relativePath.IndexOf('/');
        return firstSlashIndex > -1 ? relativePath.Substring(0, firstSlashIndex) : relativePath;
    }

    // 읽어야할 폴더인지 확인
    bool IsReadFolder(string fullPath, ReadOnlySpan<string> readTexturesFolders)
    {
        foreach (string readFolders in readTexturesFolders)
        {
            if (fullPath.Contains(readFolders))
            {
                return true;
            }
        }
        return false;
    }

    void SaveJson(ZipArchiveEntry entry, string path)
    {
        using Stream stream = entry.Open();
        using StreamReader reader = new StreamReader(stream);

        path = path.Replace("assets/minecraft/", "");

        string json = reader.ReadToEnd();
        jsonFiles.Add(path, json);
        //CustomLog.Log("JSON: " + path);
    }

    void SavePNGFile(ZipArchiveEntry entry, string path)
    {
        using Stream stream = entry.Open();
        using MemoryStream memoryStream = new MemoryStream();

        path = path.Replace("assets/minecraft/", "");

        stream.CopyTo(memoryStream);
        textureFiles.Add(path, memoryStream.ToArray());
        //CustomLog.Log("PNG: " + path);
    }
    #endregion
}