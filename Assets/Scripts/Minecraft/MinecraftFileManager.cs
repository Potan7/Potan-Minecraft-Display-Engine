using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Manager;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Minecraft
{

    public class MinecraftFileManager : BaseManager
    {
        private static MinecraftFileManager _instance;

        //Dictionary<string, byte[]> textureFiles = new Dictionary<string, byte[]>();
        private readonly ConcurrentDictionary<string, byte[]> _textureFiles = new();
        //HashSet<string> isTextureAnimated = new HashSet<string>();
        private readonly ConcurrentBag<string> _isTextureAnimated = new();

        //public Dictionary<string, string> jsonFiles = new Dictionary<string, string>();
        private readonly ConcurrentDictionary<string, string> _jsonFiles = new();

        // readPreReadedFiles�� �ִ� ���ϵ��� �̸� �о��
        private readonly Dictionary<string, MinecraftModelData> _importantModels = new();

        //readonly string[] readFolder = { "models", "textures", "blockstates", "items" }; // ���� ����
        //readonly string[] readTexturesFolders = 
        //    { "block", "item", "entity/bed", "entity/shulker", "entity/chest", "entity/conduit", 
        //    "entity/creeper", "entity/zombie/zombie", "entity/skeleton/", "entity/piglin", "entity/player/wide/steve", "entity/enderdragon/dragon"}; // textures�� ���� ����
        //readonly string[] readPreReadedFiles =
        //    {"block", "cube", "cube_all", "cube_all_inner_faces", "cube_column"};   // �̸� �ε��� ����

        private readonly string[] _hardcodeNames = { "head", "bed", "shulker_box", "chest", "conduit", "shield", "decorated_pot", "banner" };

        private readonly string _appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        private const string MinecraftPath = ".minecraft/versions";
        private const string MinecraftVersion = "1.21.4";

        [SerializeField] private string filePath;

        protected override void Awake()
        {
            base.Awake();
            _instance = this;
        }

        // 시작하면 마크 파일 읽음 
        private async void Start()
        {
            filePath = $"{_appdata}/{MinecraftPath}/{MinecraftVersion}/{MinecraftVersion}.jar";

            CustomLog.Log($"Reading minecraft file: {MinecraftVersion}");
            var sw = new Stopwatch();
            sw.Start();

            await ReadJarFile(filePath, "assets/minecraft");

            CustomLog.Log("Finished reading JAR file");
            //CustomLog.Log("Textures: " + textureFiles.Count + ", JSON: " + jsonFiles.Count);

            sw.Stop();
            CustomLog.Log($"Reading JAR file took {sw.ElapsedMilliseconds}ms");

        }

        #region Static �Լ���

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
            
            return _instance._jsonFiles.TryGetValue(path, out var file) ? JObject.Parse(file) : null;
        }

        /// <summary>
        /// �� �����͸� �����ɴϴ�.
        /// ���� hardcodeNames�� �ִ� �̸��� Ȯ���ϰ�, �� ���� jsonFiles�� �ִ��� Ȯ���մϴ�.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static MinecraftModelData GetModelData(string path)
        {
            //CustomLog.Log("Get Model Data: " + path);

            if (_instance._importantModels.TryGetValue(path, out var data))
            {
                return data;
            }

            foreach (var t in _instance._hardcodeNames)
            {
                if (path.Contains(t))
                {
                    return JsonConvert.DeserializeObject<MinecraftModelData>(Resources.Load<TextAsset>("hardcoded/" + path.Replace(".json", "")).text);
                }
            }

            if (_instance._jsonFiles.TryGetValue(path, out var file))
            {
                return JsonConvert.DeserializeObject<MinecraftModelData>(file);
            }

            CustomLog.LogError("Model not found: " + path);
            return null;
        }

        // ReSharper disable Unity.PerformanceAnalysis
        public static Texture2D GetTextureFile(string path)
        {
            if (_instance._textureFiles.TryGetValue(path, out var file))
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
            return _instance._isTextureAnimated.Contains(path + ".mcmeta");
        }

        public static string RemoveNamespace(string path) => path.Replace("minecraft:", "");
        #endregion

        #region ���� �ε�

                private async Task ReadJarFile(string path, string targetFolder)
        {
            // ó���� �ε��ϰ� �����Ƿ� ���⼭ ���� �� �޸𸮿� �ø��� �ʰ� ó��
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
                CustomLog.LogError("File not found: " + path);
                return;
            }

            using (var jarArchive = ZipFile.OpenRead(path))
            {
                var tasks = new List<Task>(); // Store tasks for async processing

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
                    tasks.Add(Task.Run(() =>
                    {
                        if (isTextureFolder)
                        {
                            if (entry.FullName.EndsWith(".png"))
                            {
                                SavePNGData(entry.FullName, fileData);
                            }
                            else if (entry.FullName.EndsWith(".mcmeta"))
                            {
                                _isTextureAnimated.Add(entry.FullName.Replace("assets/minecraft/", ""));
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

                await Task.WhenAll(tasks); // Wait for all async tasks to finish
                
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
        private static string GetTopLevelFolder(string fullPath, string targetFolder)
        {
            var relativePath = fullPath[(targetFolder.Length + 1)..]; // targetFolder ���� ���
            var firstSlashIndex = relativePath.IndexOf('/');
            return firstSlashIndex > -1 ? relativePath[..firstSlashIndex] : relativePath;
        }

        // �־��� ���� ��ΰ� ���� ���ϴ� textures ��� �� �ϳ����� Ȯ��
        private static bool IsReadFolder(string fullPath, string[] readTexturesFolders)
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

        public List<string> fileNames = new List<string>();
        // �ؽ������� ����Ʈ �ڵ�� �����ϱ�
        private void SavePNGData(string path, byte[] fileData)
        {
            path = path.Replace("assets/minecraft/textures/", "");
            _textureFiles[path] = fileData;
            
            fileNames.Add(path);

            //lock (textureFiles) // Lock because textureFiles is a shared resource
            //{
            //    textureFiles.Add(path, fileData);
            //}

            // CustomLog.Log("PNG: " + path);
        }
        #endregion
    }
}