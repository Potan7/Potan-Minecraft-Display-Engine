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
using Cysharp.Threading.Tasks;
using System.Collections.Generic;

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

        private Action<Texture2D> _textureReadyCallback;

        void OnDestroy()
        {
            if (_textureReadyCallback != null)
            {
                var data = transform.parent.parent.GetComponent<BdObjectContainer>().BdObject;
                string base64Texture = data.GetHeadTexture();
                PlayerHeadTextureCache.RemoveCallback(base64Texture, _textureReadyCallback);
                _textureReadyCallback = null;
            }
        }

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
                CustomLog.LogError("Head Type Error.");
                return;
            }

            GenerateHeadCoroutine();
        }

        private void GenerateHeadCoroutine()
        {
            GameManager.GetManager<FileLoadManager>().WorkingGenerators.Add(this);

            try
            {
                if (headType == HeadType.Player)
                {
                    GetPlayerHeadTexture();
                    // 콜백이 호출될 때까지 기다려야 하므로, 여기서는 바로 반환합니다.
                    // 텍스처가 준비되면 OnPlayerHeadTextureReady에서 나머지 로직이 처리됩니다.
                    return;
                }
                else
                {
                    headTexture = headType switch
                    {
                        HeadType.Piglin => MinecraftFileManager.GetTextureFile(DefaultTexturePath + "piglin/piglin.png"),
                        HeadType.Dragon => MinecraftFileManager.GetTextureFile(DefaultTexturePath + "enderdragon/dragon.png"),
                        HeadType.Zombie => MinecraftFileManager.GetTextureFile(DefaultTexturePath + "zombie/zombie.png"),
                        HeadType.Skull => MinecraftFileManager.GetTextureFile(DefaultTexturePath + "skeleton/skeleton.png"),
                        HeadType.Witherskull => MinecraftFileManager.GetTextureFile(DefaultTexturePath + "skeleton/wither_skeleton.png"),
                        HeadType.Creeper => MinecraftFileManager.GetTextureFile(DefaultTexturePath + "creeper/creeper.png"),
                        _ => MinecraftFileManager.GetTextureFile(DefaultTexturePath + "player/wide/steve.png")
                    };
                }

                FinishModelGeneration();
            }
            catch (Exception e)
            {
                if (this != null && gameObject != null) // 오브젝트가 파괴되지 않았을 때만 로그를 남깁니다.
                {
                    CustomLog.UnityLog(e);
                    headTexture = MinecraftFileManager.GetTextureFile(DefaultTexturePath + "player/wide/steve.png");
                    FinishModelGeneration();
                }
            }
            finally
            {
                if (this != null && GameManager.Instance != null) // 게임이 종료중이 아닐 때
                    GameManager.GetManager<FileLoadManager>().WorkingGenerators.Remove(this);
            }
        }

        private void GetPlayerHeadTexture()
        {
            var data = transform.parent.parent.GetComponent<BdObjectContainer>().BdObject;
            string base64Texture = data.GetHeadTexture();
            this.downloadUrl = PlayerHeadTextureCache.GetUrlFromBase64(base64Texture);

            _textureReadyCallback = OnPlayerHeadTextureReady;
            PlayerHeadTextureCache.GetPlayerTexture(base64Texture, _textureReadyCallback);
        }

        private void OnPlayerHeadTextureReady(Texture2D texture)
        {
            if (this == null)
            {
                return; // 오브젝트가 파괴된 경우 중단
            }

            headTexture = texture;
            _textureReadyCallback = null; // 콜백 사용 완료 후 참조 제거
            FinishModelGeneration();
        }

        private void FinishModelGeneration()
        {
            string modelPath = headType switch
            {
                HeadType.Player => "item/player_head",
                HeadType.Zombie => "item/zombie_head",
                HeadType.Witherskull or HeadType.Skull or HeadType.Creeper => "item/creeper_head",
                HeadType.Piglin => "item/piglin_head",
                HeadType.Dragon => "item/dragon_head",
                HeadType.None => "",
                _ => throw new ArgumentOutOfRangeException()
            };

            if (string.IsNullOrEmpty(modelPath)) return;

            var applies = new List<ApplySpec>
            {
                new() { Model = modelPath, X = 0, Y = 0, UvLock = false }
            };
            GenerateMeshFromApplies(applies);
        }

        protected override Texture2D CreateTexture(string path)
        {
            return headTexture;
        }

        [ContextMenu("Save Texture")]
        public void SaveTexture()
        {
            if (headTexture == null)
            {
                CustomLog.LogError("Head Texture is null.");
                return;
            }

            var path = Application.dataPath + "/../" + "HeadTexture.png";
            var bytes = headTexture.EncodeToPNG();
            System.IO.File.WriteAllBytes(path, bytes);
            CustomLog.Log("Head Texture saved to: " + path);
        }
    }
}