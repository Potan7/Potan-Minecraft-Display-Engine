using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Minecraft
{

    public class MinecraftFileManager
    {
        private static MinecraftFileManager _instance;
        public static MinecraftFileManager Instance
        {
            get
            {
                _instance ??= new MinecraftFileManager();
                return _instance;
            }
        }

        //Dictionary<string, byte[]> textureFiles = new Dictionary<string, byte[]>();
        private readonly ConcurrentDictionary<string, byte[]> _textureFiles = new();
        //HashSet<string> isTextureAnimated = new HashSet<string>();
        private readonly ConcurrentBag<string> _isTextureAnimated = new();

        //public Dictionary<string, string> jsonFiles = new Dictionary<string, string>();
        private readonly ConcurrentDictionary<string, string> _jsonFiles = new();
        
        // PreviewImgGenerator가 모델 목록에 접근할 수 있도록 public 프로퍼티를 추가합니다.
        public IReadOnlyDictionary<string, string> AllJsonFiles => _jsonFiles;

        // readPreReadedFiles�� �ִ� ���ϵ��� �̸� �о��
        private readonly Dictionary<string, MinecraftModelData> _importantModels = new();

        //readonly string[] readFolder = { "models", "textures", "blockstates", "items" }; // ���� ����
        //readonly string[] readTexturesFolders = 
        //    { "block", "item", "entity/bed", "entity/shulker", "entity/chest", "entity/conduit", 
        //    "entity/creeper", "entity/zombie/zombie", "entity/skeleton/", "entity/piglin", "entity/player/wide/steve", "entity/enderdragon/dragon"}; // textures�� ���� ����
        //readonly string[] readPreReadedFiles =
        //    {"block", "cube", "cube_all", "cube_all_inner_faces", "cube_column"};   // �̸� �ε��� ����

        private readonly string[] _hardcodeNames = { "head", "bed", "shulker_box", "chest", "conduit", "shield", "decorated_pot", "banner" };

        // private readonly string _appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        // public static string MinecraftPath = ".minecraft/versions";
        // private const string MinecraftVersion = "1.21.4";

        public static readonly List<string> SurportedVersions = new List<string>
        {
            "1.21.8",
            "1.21.7",
            "1.21.6",
            "1.21.5",
            "1.21.4"
        };

        private static int currentMinecraftVersionIndex = 0;
        public static string MinecraftVersion => SurportedVersions[currentMinecraftVersionIndex];
        public bool IsReadedFiles { get; private set; } = false;

        // 시작하면 마크 파일 읽음 
        public async UniTask<(bool success, string error)> ReadMinecraftFile(string path, string version)
        {
            // filePath = path;
            currentMinecraftVersionIndex = SurportedVersions.IndexOf(version);
            try
            {
                await ReadJarFile(path, "assets/minecraft");
            }
            catch (Exception e)
            {
                CustomLog.UnityLog("Error reading Minecraft file: " + e.Message);
                return (false, "Error reading Minecraft file");
            }

            if (currentMinecraftVersionIndex < 0)
            {
                CustomLog.UnityLog("Unsupported Minecraft version: " + version);
                return (false, "Unsupported Minecraft version");
            }
            IsReadedFiles = true;
            return (true, string.Empty);
        }

        #region Static functions

        public static JObject GetJsonData(string path)
        {
            //Debug.Log(path);
            if (path.Contains("bed") && !path.Contains("items"))
            {
                //CustomLog.Log("Bed: " + path);
                var bed = Resources.Load<TextAsset>("hardcoded/" + path.Replace(".json", ""));
                return JObject.Parse(bed.text);
            }
            //Debug.Log(_instance._jsonFiles.ContainsKey(path));
            #if UNITY_EDITOR
            var data = Instance._jsonFiles.TryGetValue(path, out var file) ? JObject.Parse(file) : null;
            if (data == null)
            {
                CustomLog.LogError("JSON not found: " + path);
            }
            return data;
            #else
            return Instance._jsonFiles.TryGetValue(path, out var file) ? JObject.Parse(file) : null;
            #endif
        }

        /// <summary>
        /// Get model data from the path.
        /// If the model is hardcoded, it will load from the hardcoded folder.
        /// </summary>
        /// <param name="path">dont need .json</param>
        /// <returns></returns>
        public static MinecraftModelData GetModelData(string path)
        {
            //CustomLog.Log("Get Model Data: " + path);

            if (Instance._importantModels.TryGetValue(path, out var data))
            {
                return data;
            }

            foreach (var t in Instance._hardcodeNames)
            {
                if (path.Contains(t))
                {
                    return JsonConvert.DeserializeObject<MinecraftModelData>(Resources.Load<TextAsset>("hardcoded/" + path.Replace(".json", "")).text);
                }
            }

            if (Instance._jsonFiles.TryGetValue(path, out var file))
            {
                return JsonConvert.DeserializeObject<MinecraftModelData>(file);
            }

            CustomLog.LogError("Model not found: " + path);
            return null;
        }

        public static Texture2D GetTextureFile(string path)
        {
            if (Instance._textureFiles.TryGetValue(path, out var file))
            {
                var texture = new Texture2D(2, 2)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };
                //texture.alphaIsTransparency = true;
                texture.Apply();

                texture.LoadImage(file);

                return texture;
            }
            CustomLog.LogError("Texture not found: " + path);
            return null;
        }

        public static bool IsTextureAnimated(string path)
        {
            //CustomLog.Log(path);
            return Instance._isTextureAnimated.Contains(path + ".mcmeta");
        }

        public static string RemoveNamespace(string path) => path.Replace("minecraft:", "");
        #endregion

        #region Read Minecraft JAR file
        private async UniTask ReadJarFile(string path, string targetFolder)
        {
            // 1회용으로 읽을 폴더들
            string[] readTexturesFolders =
            {
                "textures/block", "textures/item", "textures/entity/bed", "textures/entity/shulker",
                "textures/entity/chest", "textures/entity/conduit", "textures/entity/creeper",
                "textures/entity/zombie/zombie", "textures/entity/skeleton/", "textures/entity/piglin",
                "textures/entity/player/wide/steve", "textures/entity/enderdragon/dragon",
                "textures/entity/shield", "textures/entity/conduit/base", "textures/entity/decorated_pot/decorated_pot",
                "textures/entity/banner_base"
            };

            string[] readFolder = { "models", "textures", "blockstates", "items" };

            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Minecraft JAR file not found", path);
            }

            using (var jarArchive = ZipFile.OpenRead(path))
            {
                var tasks = new List<UniTask>(); // Store tasks for async processing

                foreach (var entry in jarArchive.Entries)
                {
                    // assets/minecraft/... ���� ���� targetFolder�� �����ϴ��� Ȯ��
                    if (!entry.FullName.StartsWith(targetFolder) || string.IsNullOrEmpty(entry.Name))
                        continue;

                    // �ֻ��� ���� ����
                    var folderName = GetTopLevelFolder(entry.FullName, targetFolder);

                    var isTextureFolder = false;
                    var isJsonFolder = false;

                    if (folderName == "textures" && IsReadFolder(entry.FullName, readTexturesFolders))
                    {
                        if (entry.FullName.EndsWith(".png") || entry.FullName.EndsWith(".mcmeta"))
                            isTextureFolder = true;
                    }
                    else if (readFolder.Contains(folderName))
                    {
                        if (entry.FullName.EndsWith(".json"))
                            isJsonFolder = true;
                    }

                    if (!isTextureFolder && !isJsonFolder)
                    {
                        continue;
                    }



                    byte[] fileData;
                    using (var stream = entry.Open()) // Read the file data first
                    using (var memoryStream = new MemoryStream())
                    {
                        stream.CopyTo(memoryStream);
                        fileData = memoryStream.ToArray();
                    }

                    // Process each file asynchronously
                    tasks.Add(UniTask.RunOnThreadPool(() =>
                    {
                        if (isTextureFolder)
                        {
                            if (entry.FullName.EndsWith(".png"))
                            {
                                SavePNGData(entry.FullName, fileData);
                            }
                            else if (entry.FullName.EndsWith(".mcmeta"))
                            {
                                _isTextureAnimated.Add(entry.FullName.Replace("assets/minecraft/textures/", ""));
                                //lock (isTextureAnimated)
                                //{
                                //    isTextureAnimated.Add(entry.FullName.Replace("assets/minecraft/", ""));
                                //}
                            }
                        }
                        else if (isJsonFolder)
                        {
                            SaveJsonData(entry.FullName, fileData);
                        }
                    }));
                }

                await UniTask.WhenAll(tasks); // Wait for all async tasks to finish
                
            }

            CachingImportantModels();
        }

        private void CachingImportantModels()
        {
            string[] cachedFiles =
                { "block", "cube", "cube_all", "cube_all_inner_faces", "cube_column" }; 

            foreach (var read in cachedFiles)
            {
                var readPath = $"models/{read}.json";
                if (_jsonFiles.TryGetValue(readPath, out var file))
                {
                    _importantModels.Add(read, GetModelData(file));
                }
            }
        }

        // �ֻ��� ���� �̸� ����
        private string GetTopLevelFolder(string fullPath, string targetFolder)
        {
            var relativePath = fullPath[(targetFolder.Length + 1)..]; // targetFolder ���� ���
            var firstSlashIndex = relativePath.IndexOf('/');
            return firstSlashIndex > -1 ? relativePath[..firstSlashIndex] : relativePath;
        }

        // �־��� ���� ��ΰ� ���� ���ϴ� textures ��� �� �ϳ����� Ȯ��
        private bool IsReadFolder(string fullPath, string[] readTexturesFolders)
        {
            foreach (var texture in readTexturesFolders)
            {
                if (fullPath.Contains(texture))
                {
                    return true;
                }
            }
            return false;
        }

        // JSON ����
        private void SaveJsonData(string path, byte[] fileData)
        {
            path = path.Replace("assets/minecraft/", "");

            using var memoryStream = new MemoryStream(fileData);
            using var reader = new StreamReader(memoryStream);
            var json = reader.ReadToEnd();
            
            _jsonFiles[path] = json;
            //lock (jsonFiles)
            //{
            //    jsonFiles[path] = json;
            //}
        }

        // �ؽ������� ����Ʈ �ڵ�� �����ϱ�
        private void SavePNGData(string path, byte[] fileData)
        {
            path = path.Replace("assets/minecraft/textures/", "");
            _textureFiles[path] = fileData;

            //lock (textureFiles) // Lock because textureFiles is a shared resource
            //{
            //    textureFiles.Add(path, fileData);
            //}

            // CustomLog.Log("PNG: " + path);
        }
        #endregion
    }
}