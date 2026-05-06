using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System;
using UnityEngine;
using UnityEngine.Networking;
using FileSystem;
using Minecraft;

namespace BDObjectSystem.Display
{
    public static class PlayerHeadTextureCache
    {
        private static readonly Dictionary<string, Texture2D> _textureCache = new Dictionary<string, Texture2D>();
        // URL별 다운로드 진행 상태를 추적합니다.
        private static readonly Dictionary<string, bool> _downloadingFlags = new Dictionary<string, bool>();
        // URL별 콜백 목록을 저장합니다.
        private static readonly Dictionary<string, List<Action<Texture2D>>> _pendingCallbacks = new Dictionary<string, List<Action<Texture2D>>>();

        private const string DefaultTexturePath = "entity/player/wide/steve.png";

        public static void Dispose()
        {
            _textureCache.Clear();
            _downloadingFlags.Clear();
            _pendingCallbacks.Clear();
        }

        public static void PreloadPlayerHeadTextures(string base64TextureValue = null)
        {
            var (isDefault, downloadUrl) = GetDownloadUrlOrDefault(base64TextureValue);
            if (isDefault)
            {
                // 기본 텍스처를 미리 로드합니다.
                _textureCache[downloadUrl] = MinecraftFileManager.GetTextureFile(downloadUrl);
                return;
            }

            lock (_textureCache)
            {
                if (!_textureCache.ContainsKey(downloadUrl))
                {
                    _textureCache[downloadUrl] = null; // 초기값으로 null 설정
                    DownloadAndCacheTexture(downloadUrl).Forget();
                }
            }
        }

        // 콜백을 사용하는 새로운 메인 메서드
        public static void GetPlayerTexture(string base64TextureValue, Action<Texture2D> callback)
        {
            var (isDefault, downloadUrl) = GetDownloadUrlOrDefault(base64TextureValue);
            if (isDefault)
            {
                callback?.Invoke(MinecraftFileManager.GetTextureFile(downloadUrl));
                return;
            }

            lock (_textureCache)
            {
                if (_textureCache.TryGetValue(downloadUrl, out var cachedTexture) && cachedTexture != null)
                {
                    callback?.Invoke(cachedTexture);
                    return;
                }

                if (_downloadingFlags.TryGetValue(downloadUrl, out bool isDownloading) && isDownloading)
                {
                    if (!_pendingCallbacks.ContainsKey(downloadUrl))
                    {
                        _pendingCallbacks[downloadUrl] = new List<Action<Texture2D>>();
                    }
                    _pendingCallbacks[downloadUrl].Add(callback);
                    return;
                }

                _downloadingFlags[downloadUrl] = true;
                _pendingCallbacks[downloadUrl] = new List<Action<Texture2D>> { callback };

                DownloadAndCacheTexture(downloadUrl).Forget();
            }
        }

        public static void RemoveCallback(string base64TextureValue, Action<Texture2D> callback)
        {
            if (string.IsNullOrEmpty(base64TextureValue) || callback == null) return;

            string downloadUrl = GetUrlFromBase64(base64TextureValue);
            if (string.IsNullOrEmpty(downloadUrl)) return;

            lock (_textureCache) // 기존 lock을 재사용하여 스레드 안전성 보장
            {
                if (_pendingCallbacks.TryGetValue(downloadUrl, out var callbacks))
                {
                    callbacks.Remove(callback);
                    if (callbacks.Count == 0)
                    {
                        _pendingCallbacks.Remove(downloadUrl);
                    }
                }
            }
        }

        private static async UniTaskVoid DownloadAndCacheTexture(string url)
        {
            Texture2D resultTexture = null;
            try
            {
                using var request = UnityWebRequestTexture.GetTexture(url);
                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var downloadedTexture = ((DownloadHandlerTexture)request.downloadHandler).texture;
                    downloadedTexture.filterMode = FilterMode.Point;
                    downloadedTexture.wrapMode = TextureWrapMode.Clamp;
                    downloadedTexture.Apply();
                    
                    lock(_textureCache)
                    {
                        _textureCache[url] = downloadedTexture;
                    }
                    resultTexture = downloadedTexture;
                }
                else
                {
                    CustomLog.LogError($"Texture download failed: {request.error}");
                }
            }
            catch (Exception e)
            {
                CustomLog.LogError($"An error occurred during texture download: {e}");
            }
            finally
            {
                if (resultTexture == null)
                {
                    resultTexture = MinecraftFileManager.GetTextureFile(DefaultTexturePath);
                    lock (_textureCache)
                    {
                        _textureCache[url] = resultTexture;
                    }
                }

                List<Action<Texture2D>> callbacksToInvoke = null;
                lock (_textureCache)
                {
                    if (_pendingCallbacks.TryGetValue(url, out callbacksToInvoke))
                    {
                        _pendingCallbacks.Remove(url);
                    }
                    _downloadingFlags.Remove(url);
                }

                if (callbacksToInvoke != null)
                {
                    foreach (var cb in callbacksToInvoke)
                    {
                        cb?.Invoke(resultTexture);
                    }
                }
            }
        }

        private static (bool isDefault, string url) GetDownloadUrlOrDefault(string base64)
        {
            if (string.IsNullOrEmpty(base64))
            {
                return (true, DefaultTexturePath);
            }

            var url = GetUrlFromBase64(base64);
            if (string.IsNullOrEmpty(url))
            {
                return (true, DefaultTexturePath);
            }

            return (false, url);
        }

        public static string GetUrlFromBase64(string base64)
        {
            try
            {
                var jsonDataBytes = Convert.FromBase64String(base64);
                var jsonString = Encoding.UTF8.GetString(jsonDataBytes);
                var jsonObject = JObject.Parse(jsonString);
                return jsonObject["textures"]?["SKIN"]?["url"]?.ToString().Replace("http", "https");
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}