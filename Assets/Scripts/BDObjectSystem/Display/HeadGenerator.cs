using System;
using System.Collections;
using System.Text;
using GameSystem;
using Minecraft;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;
using BDObjectSystem;
using FileSystem;

namespace BDObjectSystem.Display
{
    public class HeadGenerator : BlockModelGenerator
    {
        public enum HeadType
        {
            Player,
            Piglin,
            Dragon,
            Zombie,
            Skull,
            Witherskull,
            Creeper,
            None
        }

        private const string DefaultTexturePath = "entity/";

        public HeadType headType;
        public Texture2D headTexture;
        public string downloadUrl;

        public void GenerateHead(string name)
        {
            modelName = "head";

            headType = name switch
            {
                "player" => HeadType.Player,
                "piglin" => HeadType.Piglin,
                "dragon" => HeadType.Dragon,
                "zombie" => HeadType.Zombie,
                "skeleton" => HeadType.Skull,
                "wither_skeleton" => HeadType.Witherskull,
                "creeper" => HeadType.Creeper,
                _ => HeadType.None
            };

            if (headType == HeadType.None)
            {
                CustomLog.LogError("��� Ÿ���� �߸��Ǿ����ϴ�.");
                return;
            }

            StartCoroutine(GenerateHeadCoroutine());
        }

        // ReSharper disable Unity.PerformanceAnalysis
        private IEnumerator GenerateHeadCoroutine()
        {
            headTexture = headType switch
            {
                HeadType.Player => SetPlayerTexture(),
                HeadType.Piglin => MinecraftFileManager.GetTextureFile(DefaultTexturePath + "piglin/piglin.png"),
                HeadType.Dragon => MinecraftFileManager.GetTextureFile(DefaultTexturePath + "enderdragon/dragon.png"),
                HeadType.Zombie => MinecraftFileManager.GetTextureFile(DefaultTexturePath + "zombie/zombie.png"),
                HeadType.Skull => MinecraftFileManager.GetTextureFile(DefaultTexturePath + "skeleton/skeleton.png"),
                HeadType.Witherskull => MinecraftFileManager.GetTextureFile(DefaultTexturePath + "skeleton/wither_skeleton.png"),
                HeadType.Creeper => MinecraftFileManager.GetTextureFile(DefaultTexturePath + "creeper/creeper.png"),
                _ => MinecraftFileManager.GetTextureFile(DefaultTexturePath + "player/wide/steve.png")
            };

            GameManager.GetManager<FileLoadManager>().WorkingGenerators.Add(this);

            WaitForSeconds wait = new WaitForSeconds(0.1f);
            try
            {
                int timeout = 0;
                while (headTexture == null)
                {
                    if (timeout > 10000)
                    {
                        CustomLog.LogError("Timeout");
                        break;
                    }

                    yield return wait;
                    timeout++;
                }
            }
            finally
            {
                GameManager.GetManager<FileLoadManager>().WorkingGenerators.Remove(this);
            }


            switch (headType)
            {
                case HeadType.Player:
                    SetModel("item/player_head");
                    break;
                case HeadType.Zombie:
                    SetModel("item/zombie_head");
                    break;
                case HeadType.Witherskull:
                case HeadType.Skull:
                case HeadType.Creeper:
                    SetModel("item/creeper_head");
                    break;
                case HeadType.Piglin:
                    SetModel("item/piglin_head");
                    break;
                case HeadType.Dragon:
                    SetModel("item/dragon_head");
                    break;
                case HeadType.None:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        //static void SetPlayerSkin(Texture2D edit)
        //{
        //    int interval = edit.width/2;

        //    edit.filterMode = FilterMode.Point;
        //    edit.wrapMode = TextureWrapMode.Clamp;

        //    for (int i = 0; i < interval; i++)
        //    {
        //        for (int j = 0; j < interval; j++)
        //        {
        //            Color color = edit.GetPixel(i + interval, j+interval);

        //            if (color.a > 0)
        //            {
        //                //CustomLog.Log(color);
        //                edit.SetPixel(i, j + interval, color);
        //            }
        //        }
        //    }

        //    edit.Apply();
        //}



        protected override Texture2D CreateTexture(string path)
        {
            return headTexture;
        }

        //protected override bool CheckForTransparency(Texture2D texture)
        //{
        //    return false;
        //}

        //protected override void SetFaces(MinecraftModelData model, JObject element, MeshRenderer cubeObject)
        //{
        //    base.SetFaces(model, element, cubeObject);

        //    if (headType == HeadType.PLAYER)
        //    {
        //        int cnt = cubeObject.materials.Length;
        //        for (int i = 0; i < cnt; i++)
        //        {
        //            cubeObject.materials[i].EnableKeyword("_ALPHATEST_ON");
        //            cubeObject.materials[i].SetFloat("_AlphaClip", 1.0f);
        //        }
        //    }
        //}

        private Texture2D SetPlayerTexture()
        {
            // Get Playter Texture
            var data = transform.parent.parent.GetComponent<BdObjectContainer>().BdObject;
            
            if (!data.ExtraData.TryGetValue("defaultTextureValue", out var value))
                return MinecraftFileManager.GetTextureFile(DefaultTexturePath + "player/wide/steve.png");
            
            try
            {
                var jsonDataBytes = Convert.FromBase64String(value.ToString());
                var jsonString = Encoding.UTF8.GetString(jsonDataBytes);

                var jsonObject = JObject.Parse(jsonString);

                var url = jsonObject["textures"]?["SKIN"]?["url"]?.ToString();
                StartCoroutine(DownloadTexture(url));
                return null;
            }
            catch (Exception e)
            {
                CustomLog.LogError("Can't Get Player Texture" + e.Message);
            }
            return MinecraftFileManager.GetTextureFile(DefaultTexturePath + "player/wide/steve.png");
        }

        private IEnumerator DownloadTexture(string url)
        {
            url = url.Replace("http://", "https://");
            downloadUrl = url;
            using var request = UnityWebRequestTexture.GetTexture(url);
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
#if UNITY_EDITOR
                CustomLog.LogError("Error: " + request.error);
#else
            CustomLog.LogError("Download Fail! Try Again");
#endif
                StartCoroutine(DownloadTexture(url));
            }
            else
            {
                var downloadedTexture = ((DownloadHandlerTexture)request.downloadHandler).texture;

                downloadedTexture.filterMode = FilterMode.Point;
                downloadedTexture.wrapMode = TextureWrapMode.Clamp;
                downloadedTexture.Apply();

                //SetPlayerSkin(downloadedTexture);
                //downloadedTexture.Apply();

                headTexture = downloadedTexture;

            }
        }
    }
}
