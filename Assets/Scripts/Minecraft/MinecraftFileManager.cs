using System.IO;
using System.IO.Compression;
using UnityEngine;
using System.Threading;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

public class MinecraftFileManager : RootManager
{
    static MinecraftFileManager instance;

    Dictionary<string, byte[]> textureFiles = new Dictionary<string, byte[]>();
    Dictionary<string, string> jsonFiles = new Dictionary<string, string>();

    Dictionary<string, MinecraftModelData> importantModels = new Dictionary<string, MinecraftModelData>();

    string[] readFolder = { "models", "textures", "blockstates", "items" }; // 읽을 폴더
    string[] readTexturesFolders = { "block", "item" }; // textures의 읽을 폴더
    string[] readPreReadedFiles =
        {"block", "cube", "cube_all", "cube_all_inner_faces", "cube_column"};

    readonly string Appdata = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
    readonly string minecraftPath = ".minecraft/versions";
    readonly string minecraftVersion = "1.21.4";

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
        Thread thread = new Thread(() => ReadJarFile(filePath, "assets/minecraft"));
        thread.Start();
    }

    public static JObject GetJSONData(string path)
    {
        if (instance.jsonFiles.ContainsKey(path))
        {
            return JObject.Parse(instance.jsonFiles[path]);
        }
        return null;
    }

    public static MinecraftModelData GetModelData(string path)
    {
        //Debug.Log("Get Model Data: " + path);

        if (instance.importantModels.ContainsKey(path))
        {
            return instance.importantModels[path];
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
            texture.LoadImage(instance.textureFiles[path]);
            return texture;
        }
        return null;
    }

    public static string RemoveNamespace(string path)
    {
        return path.Replace("minecraft:", "");
    }

    void ReadJarFile(string path, string targetFolder)
    {
        Debug.Log($"Reading JAR file: {path}");
        if (!File.Exists(path))
        {
            Debug.LogError("File not found: " + path);
            return;
        }

        using ZipArchive jarArchive = ZipFile.OpenRead(path);
        foreach (var entry in jarArchive.Entries)
        {
            if (!entry.FullName.StartsWith(targetFolder) || string.IsNullOrEmpty(entry.Name))
                continue;

            // 파일 필터링
            string folderName = GetTopLevelFolder(entry.FullName, targetFolder);

            if (folderName == "textures")
            {
                // textures 폴더 처리
                if (!IsReadFolder(entry.FullName)) continue; // 무시할 폴더 확인
                if (entry.FullName.EndsWith(".png"))
                {
                    //Debug.Log($"Found texture file: {entry.FullName}");
                    SavePNGFile(entry, entry.FullName);
                }
            }
            else if (readFolder.Contains(folderName))
            {
                // 다른 폴더 처리
                if (entry.FullName.EndsWith(".json"))
                {
                    SaveJson(entry, entry.FullName);
                    //Debug.Log($"Found JSON file: {entry.FullName}");
                }
            }
        }

        Debug.Log("Finished reading JAR file");
        Debug.Log("Textures: " + textureFiles.Count);
        Debug.Log("JSON: " + jsonFiles.Count);

        // readImportantModels();
        foreach (var read in readPreReadedFiles)
        {
            string readPath = $"models/{read}.json";
            if (instance.jsonFiles.ContainsKey(readPath))
            {
                importantModels.Add(read, GetModelData(jsonFiles[readPath]));
            }
        }
    }

        // 최상위 폴더 이름 추출
    string GetTopLevelFolder(string fullPath, string targetFolder)
    {
        string relativePath = fullPath.Substring(targetFolder.Length + 1); // targetFolder 이후 경로
        int firstSlashIndex = relativePath.IndexOf('/');
        return firstSlashIndex > -1 ? relativePath.Substring(0, firstSlashIndex) : relativePath;
    }

    // 읽어야할 폴더인지 확인
    bool IsReadFolder(string fullPath)
    {
        foreach (string readFolders in readTexturesFolders)
        {
            if (fullPath.Contains($"textures/{readFolders}/"))
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
        //Debug.Log("JSON: " + path);
    }

    void SavePNGFile(ZipArchiveEntry entry, string path)
    {
        using Stream stream = entry.Open();
        using MemoryStream memoryStream = new MemoryStream();

        path = path.Replace("assets/minecraft/", "");

        stream.CopyTo(memoryStream);
        textureFiles.Add(path, memoryStream.ToArray());
        //Debug.Log("PNG: " + path);
    }
}