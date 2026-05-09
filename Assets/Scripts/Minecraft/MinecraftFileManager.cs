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
        private readonly ConcurrentDictionary<string, JObject> _jsonCache = new();
        private readonly ConcurrentDictionary<string, Texture2D> _textureCache = new();
        private readonly ConcurrentDictionary<string, MinecraftModelData> _modelCache = new();


        // PreviewImgGenerator가 모델 목록에 접근할 수 있도록 public 프로퍼티를 추가합니다.
        public IReadOnlyDictionary<string, string> AllJsonFiles => _jsonFiles;

        // readPreReadedFiles ִ ϵ ̸ о
        private readonly Dictionary<string, MinecraftModelData> _importantModels = new();

        //readonly string[] readFolder = { "models", "textures", "blockstates", "items" }; //  
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

        public bool IsReadedFiles { get; private set; } = false;

        public SurportedVersionsSO surportedVersions;
        public Version CurrentMinecraftVersion;
        public int CurrentMinecraftVersionNumber;
        public int VersionToNumber(int major, int minor, int build)
        {
            return major * 10000 + minor * 100 + build;
        }

        public MinecraftFileManager()
        {
            surportedVersions = Resources.Load<SurportedVersionsSO>("SurportedVersions");
            if (surportedVersions == null)
            {
                CustomLog.LogError("Failed to load SurportedVersionsSO from Resources.");
                return;
            }

            surportedVersions.StartVersion = Version.Parse(surportedVersions.surportedVersionStart);
            surportedVersions.EndVersion = Version.Parse(surportedVersions.surportedVersionEnd);
        }

        public bool IsVersionSupported(ReadOnlySpan<char> version)
        {
            if (Version.TryParse(version, out var parsedVersion))
            {
                // StartVersion보다 작으면 지원하지 않음
                int result = parsedVersion.CompareTo(surportedVersions.StartVersion);
                if (result < 0) return false; // parsedVersion < StartVersion

                // EndVersion보다 크면 지원하지 않음
                result = parsedVersion.CompareTo(surportedVersions.EndVersion);
                if (result > 0) return false; // parsedVersion > EndVersion

                return true;
            }
            else
            {
                // 버전 문자열이 유효하지 않으면 지원하지 않음
                CustomLog.LogError("Invalid version format: " + version.ToString());
                return false;
            }
        }


        // 시작하면 마크 파일 읽음 
        public async UniTask<(bool success, string error)> ReadMinecraftFile(string path, string version)
        {
            // filePath = path;
            // currentMinecraftVersionIndex = SurportedVersions.IndexOf(version);

            if (!IsVersionSupported(version))
            {
                CustomLog.UnityLog("Unsupported Minecraft version: " + version);
                return (false, "Unsupported Minecraft version");
            }

            try
            {
                await ReadJarFile(path, "assets/minecraft");
            }
            catch (Exception e)
            {
                CustomLog.UnityLog("Error reading Minecraft file: " + e.Message);
                return (false, "Error reading Minecraft file");
            }

            CurrentMinecraftVersion = Version.Parse(version);
            CurrentMinecraftVersionNumber = VersionToNumber(CurrentMinecraftVersion.Major, CurrentMinecraftVersion.Minor, CurrentMinecraftVersion.Build);
            IsReadedFiles = true;

            return (true, string.Empty);
        }

        #region Static functions

        public static JObject GetJsonData(string path)
        {
            if (Instance._jsonCache.TryGetValue(path, out var cachedJson))
            {
                return cachedJson;
            }

            // 침대 모델은 하드코딩된 리소스 사용
            if (path.Contains("bed") && !path.Contains("items"))
            {
                var bed = Resources.Load<TextAsset>("hardcoded/" + path.Replace(".json", ""));
                if (bed == null) return null;

                var parsedBed = JObject.Parse(bed.text);
                Instance._jsonCache[path] = parsedBed;
                return parsedBed;
            }

            if (!Instance._jsonFiles.TryGetValue(path, out var file))
            {
#if UNITY_EDITOR
                CustomLog.LogError("JSON not found: " + path);
#endif
                return null;
            }

            var parsedJson = JObject.Parse(file);
            Instance._jsonCache[path] = parsedJson;
            return parsedJson;
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
            if (Instance._modelCache.TryGetValue(path, out var cachedData))
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
                        var modelData = JsonConvert.DeserializeObject<MinecraftModelData>(hardcodedAsset.text);
                        Instance._modelCache[path] = modelData;
                        return modelData;
                    }
                }
            }

            // 3. 일반 JSON 파일 확인
            if (Instance._jsonFiles.TryGetValue(path, out var file))
            {
                var modelData = JsonConvert.DeserializeObject<MinecraftModelData>(file);
                Instance._modelCache[path] = modelData;
                return modelData;
            }

#if UNITY_EDITOR
            CustomLog.LogError("Model not found: " + path);
#endif
            return null;
        }

        public static Texture2D GetTextureFile(string path, bool makeReadable = false)
        {
            // 아이템 텍스처는 동적 메시 생성을 위해 기본적으로 Readable 상태가 되어야 함
            if (path.StartsWith("item/"))
            {
                makeReadable = true;
            }

            if (Instance._textureCache.TryGetValue(path, out var cachedTexture))
            {
                // 캐시된 텍스처가 요청된 Readable 상태와 다른 경우, 재생성해야 할 수 있으나
                // 우선은 캐시된 것을 그대로 반환. 아이템 텍스처는 항상 Readable로 캐시될 것임.
                return cachedTexture;
            }

            if (!Instance._textureFiles.TryGetValue(path, out var fileData))
            {
#if UNITY_EDITOR
                CustomLog.LogError("Texture not found: " + path);
#endif
                return null;
            }

            // 생성자의 마지막 파라미터를 true로 설정하여 Linear 색 공간임을 명시합니다.
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true)
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

            // makeReadable이 true이면, CPU 메모리 복사본을 유지 (makeNoLongerReadable = false)
            texture.Apply(false, !makeReadable);
            Instance._textureCache[path] = texture;

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
                "textures/block", "textures/item", "textures/entity"
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
                    var modelData = GetModelData(readPath);
                    _modelCache.TryAdd(read, modelData);
                }
            }
        }
        #endregion
    }
}