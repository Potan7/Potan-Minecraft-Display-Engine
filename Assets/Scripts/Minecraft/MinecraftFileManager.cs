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
        // private readonly ConcurrentBag<string> _isTextureAnimated = new();

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

        private readonly string[] _hardcodeNames = {
            "trident", "bed", "shulker_box", "chest", "conduit", "shield", "decorated_pot", "banner",
            "zombie_head", "skeleton_skull", "wither_skeleton_skull", "creeper_head", "piglin_head", "dragon_head", "player_head", "head"
            };

        // private readonly string _appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        // public static string MinecraftPath = ".minecraft/versions";
        // private const string MinecraftVersion = "1.21.4";

        public static readonly List<string> SurportedVersions = new List<string>
        {
            "1.21.10",
            "1.21.9",
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
            // 침대 모델은 하드코딩된 리소스 사용
            if (path.Contains("bed") && !path.Contains("items"))
            {
                var bed = Resources.Load<TextAsset>("hardcoded/" + path.Replace(".json", ""));
                return bed != null ? JObject.Parse(bed.text) : null;
            }

            if (!Instance._jsonFiles.TryGetValue(path, out var file))
            {
#if UNITY_EDITOR
                CustomLog.LogError("JSON not found: " + path);
#endif
                return null;
            }

            return JObject.Parse(file);
        }

        /// <summary>
        /// Get model data from the path.
        /// If the model is hardcoded, it will load from the hardcoded folder.
        /// </summary>
        /// <param name="path">dont need .json</param>
        /// <returns></returns>
        public static MinecraftModelData GetModelData(string path)
        {
            // 1. 중요 모델 캐시 확인 (가장 빠름)
            if (Instance._importantModels.TryGetValue(path, out var cachedData))
            {
                return cachedData;
            }

            // 2. 하드코딩된 모델 확인
            var hardcodeNamesSpan = Instance._hardcodeNames.AsSpan();
            for (int i = 0; i < hardcodeNamesSpan.Length; i++)
            {
                if (path.Contains(hardcodeNamesSpan[i]))
                {
                    // UnityEngine.Debug.Log("Loading hardcoded model: " + path);
                    var hardcodedAsset = Resources.Load<TextAsset>("hardcoded/" + path.Replace(".json", ""));
                    if (hardcodedAsset != null)
                    {
                        return JsonConvert.DeserializeObject<MinecraftModelData>(hardcodedAsset.text);
                    }
                }
            }

            // 3. 일반 JSON 파일 확인
            if (Instance._jsonFiles.TryGetValue(path, out var file))
            {
                return JsonConvert.DeserializeObject<MinecraftModelData>(file);
            }

#if UNITY_EDITOR
            CustomLog.LogError("Model not found: " + path);
#endif
            return null;
        }

        public static Texture2D GetTextureFile(string path)
        {
            if (!Instance._textureFiles.TryGetValue(path, out var fileData))
            {
#if UNITY_EDITOR
                CustomLog.LogError("Texture not found: " + path);
#endif
                return null;
            }

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            if (!texture.LoadImage(fileData))
            {
#if UNITY_EDITOR
                CustomLog.LogError("Failed to load texture: " + path);
#endif
                UnityEngine.Object.Destroy(texture);
                return null;
            }

            texture.Apply(false, true); // makeNoLongerReadable = true로 메모리 최적화

            return texture;
        }

        public static string RemoveNamespace(string path, string namespacePrefix = "minecraft:")
        {
            return path.StartsWith(namespacePrefix) 
                ? path.Substring(namespacePrefix.Length) 
                : path;
        }

        #endregion

        #region Read Minecraft JAR file
        private async UniTask ReadJarFile(string path, string targetFolder)
        {
            // 읽을 텍스처 폴더들
            string[] readTexturesFolders = new[]
            {
                "textures/block", "textures/item", "textures/entity/bed", "textures/entity/shulker",
                "textures/entity/chest", "textures/entity/conduit", "textures/entity/creeper",
                "textures/entity/zombie/zombie", "textures/entity/skeleton/", "textures/entity/piglin",
                "textures/entity/player/wide/steve", "textures/entity/enderdragon/dragon",
                "textures/entity/shield", "textures/entity/conduit/base", 
                "textures/entity/decorated_pot/decorated_pot", "textures/entity/banner_base"
            };

            // 읽을 JSON 폴더들 (더 구체적으로 명시)
            string[] readJsonFolders = new[]
            {
                "models/block", "models/item", "blockstates", "items"
            };

            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Minecraft JAR file not found", path);
            }

            using (var jarArchive = ZipFile.OpenRead(path))
            {
                var tasks = new List<UniTask>();

                foreach (var entry in jarArchive.Entries)
                {
                    // targetFolder로 시작하지 않거나 파일 이름이 없으면 건너뛰기
                    if (!entry.FullName.StartsWith(targetFolder) || string.IsNullOrEmpty(entry.Name))
                        continue;

                    var relativePath = entry.FullName[(targetFolder.Length + 1)..];
                    bool shouldRead = false;
                    bool isTexture = false;

                    // 텍스처 파일 체크
                    if (entry.FullName.EndsWith(".png"))
                    {
                        for (var i = 0; i < readTexturesFolders.Length; i++)
                        {
                            if (relativePath.StartsWith(readTexturesFolders[i]))
                            {
                                shouldRead = true;
                                isTexture = true;
                                break;
                            }
                        }
                    }
                    // JSON 파일 체크
                    else if (entry.FullName.EndsWith(".json"))
                    {
                        for (var i = 0; i < readJsonFolders.Length; i++)
                        {
                            if (relativePath.StartsWith(readJsonFolders[i]))
                            {
                                shouldRead = true;
                                isTexture = false;
                                break;
                            }
                        }
                    }

                    if (!shouldRead)
                        continue;

                    // 파일 데이터 읽기
                    byte[] fileData;
                    using (var stream = entry.Open())
                    using (var memoryStream = new MemoryStream())
                    {
                        stream.CopyTo(memoryStream);
                        fileData = memoryStream.ToArray();
                    }

                    // 비동기 처리
                    var localRelativePath = relativePath;
                    var localIsTexture = isTexture;

                    tasks.Add(UniTask.RunOnThreadPool(() =>
                    {
                        if (localIsTexture)
                        {
                            // 텍스처는 byte[] 그대로 저장
                            if (localRelativePath.StartsWith("textures/"))
                            {
                                localRelativePath = localRelativePath["textures/".Length..];
                            }
                            _textureFiles[localRelativePath] = fileData;
                        }
                        else
                        {
                            // JSON은 바로 string으로 변환하여 저장
                            var json = System.Text.Encoding.UTF8.GetString(fileData);
                            _jsonFiles[localRelativePath] = json;
                        }
                    }));
                }

                await UniTask.WhenAll(tasks);
            }

            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            await UniTask.DelayFrame(1);

            CachingImportantModels();
        }

        private void CachingImportantModels()
        {
            ReadOnlySpan<string> cachedFiles =
                new[] { "block", "cube", "cube_all", "cube_all_inner_faces", "cube_column" }; 

            foreach (var read in cachedFiles)
            {
                var readPath = $"models/{read}.json";
                if (_jsonFiles.TryGetValue(readPath, out var file))
                {
                    _importantModels.Add(read, GetModelData(readPath));
                }
            }
        }
        #endregion
    }
}