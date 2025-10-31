using System;
using System.Collections.Generic;
using FileSystem;
using GameSystem;
using Minecraft;
using UnityEngine;

namespace BDObjectSystem.Display
{
    /// <summary>
    /// 머리 블록 생성기
    /// - 모든 머리: BlockModelGenerator 활용 (model.json 파일 기반)
    /// - 커스텀 플레이어 머리: 텍스처만 다운로드하여 적용
    /// </summary>
    public class HeadGenerator : MonoBehaviour
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
        public string downloadUrl;

        private Action<Texture2D> _textureReadyCallback;
        private BlockModelGenerator _blockModelGenerator;

        void Awake()
        {
            _blockModelGenerator = GetComponent<BlockModelGenerator>();
            if (_blockModelGenerator == null)
            {
                CustomLog.LogError("BlockModelGenerator component is missing! Please add it to the prefab.");
            }
        }

        void OnDestroy()
        {
            if (_textureReadyCallback != null)
            {
                var data = transform.parent?.parent?.GetComponent<BdObjectContainer>()?.BdObject;
                if (data != null)
                {
                    string base64Texture = data.GetHeadTexture();
                    PlayerHeadTextureCache.RemoveCallback(base64Texture, _textureReadyCallback);
                }
                _textureReadyCallback = null;
            }
        }

        public void GenerateHead(string name)
        {
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

            // Awake보다 빨리 호출될 수 있으므로 여기서도 컴포넌트 가져오기
            if (_blockModelGenerator == null)
            {
                _blockModelGenerator = GetComponent<BlockModelGenerator>();
            }

            if (headType == HeadType.None)
            {
                CustomLog.LogError("Head Type Error.");
                return;
            }

            if (headType == HeadType.Player)
            {
                GeneratePlayerHead();
            }
            else
            {
                GenerateNormalHead();
            }
        }

        #region Player Head (Custom Skin)
        private void GeneratePlayerHead()
        {
            GameManager.GetManager<FileLoadManager>().WorkingGenerators.Add(this);

            try
            {
                // 메시 자체는 먼저 생성하되 텍스쳐 할당은 나중에 하도록 설정
                _blockModelGenerator.disableTextureCropping = true;
                _blockModelGenerator.centerPivot = true; // 중심 피봇 사용
                _blockModelGenerator.topPivot = true; // Y축은 상단

                string modelPath = "item/player_head";
                var applies = new List<BlockModelGenerator.ApplySpec>
                {
                    new() { Model = modelPath, X = 0, Y = 0, UvLock = false }
                };

                // Debug.Log($"Generating player head mesh using model {modelPath} (no material assignment).");
                // assignMaterials = false -> 메시만 생성하고 텍스처는 적용하지 않음
                _blockModelGenerator.GenerateMeshFromApplies(applies, assignMaterials: false);

                // 스킨 텍스처 다운로드
                var data = transform.parent?.parent?.GetComponent<BdObjectContainer>()?.BdObject;
                if (data == null)
                {
                    CustomLog.LogError("BdObjectContainer not found!");
                    ApplyDefaultPlayerTexture();
                    return;
                }

                string base64Texture = data.GetHeadTexture();
                downloadUrl = PlayerHeadTextureCache.GetUrlFromBase64(base64Texture);

                _textureReadyCallback = OnPlayerHeadTextureReady;
                PlayerHeadTextureCache.GetPlayerTexture(base64Texture, _textureReadyCallback);
            }
            catch (Exception e)
            {
                CustomLog.UnityLog(e);
                GameManager.GetManager<FileLoadManager>().WorkingGenerators.Remove(this);
            }
        }

        private void OnPlayerHeadTextureReady(Texture2D texture)
        {
            if (this == null) return;

            // Debug.Log("Player head texture downloaded: " + downloadUrl);

            _textureReadyCallback = null;

            try
            {
                ApplyPlayerTexture(texture);
            }
            catch (Exception e)
            {
                CustomLog.UnityLog(e);
                ApplyDefaultPlayerTexture();
            }
            finally
            {
                if (this != null && GameManager.Instance != null)
                    GameManager.GetManager<FileLoadManager>().WorkingGenerators.Remove(this);
            }
        }

        private void ApplyPlayerTexture(Texture2D texture)
        {
            if (texture == null)
            {
                ApplyDefaultPlayerTexture();
                return;
            }

            var meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null || meshRenderer.sharedMaterial == null)
            {
                CustomLog.LogError("MeshRenderer or material not found!");
                return;
            }

            var bdManager = GameManager.GetManager<BdObjectManager>();

            // 서브머티리얼 개수에 맞춰 새 재질 생성 후 텍스처 적용
            var existingMats = meshRenderer.sharedMaterials;
            int matCount = existingMats != null && existingMats.Length > 0 ? existingMats.Length : 1;
            var newMats = new Material[matCount];
            for (int i = 0; i < matCount; i++)
            {
                var mat = new Material(bdManager.bdobjBlockMaterial)
                {
                    mainTexture = texture
                };
                newMats[i] = mat;
            }

            meshRenderer.sharedMaterials = newMats;
        }

        private void ApplyDefaultPlayerTexture()
        {
            var defaultTexture = MinecraftFileManager.GetTextureFile(DefaultTexturePath + "player/wide/steve.png");
            ApplyPlayerTexture(defaultTexture);
        }
        #endregion

        #region Normal Head (Model.json based)
        private void GenerateNormalHead()
        {
            GameManager.GetManager<FileLoadManager>().WorkingGenerators.Add(this);

            try
            {
                // 머리 모델은 텍스처 자르기 비활성화 (64x64 텍스처 사용)
                _blockModelGenerator.disableTextureCropping = true;
                _blockModelGenerator.centerPivot = true; // 중심 피봇 사용
                _blockModelGenerator.topPivot = true; // Y축은 상단

                string modelPath = GetModelPath(headType);
                if (string.IsNullOrEmpty(modelPath))
                {
                    CustomLog.LogError($"No model path for {headType}");
                    return;
                }

                var applies = new List<BlockModelGenerator.ApplySpec>
                {
                    new() { Model = modelPath, X = 0, Y = 0, UvLock = false }
                };

                Debug.Log($"Generating head mesh for {headType} using model {modelPath}.");
                _blockModelGenerator.GenerateMeshFromApplies(applies);
            }
            catch (Exception e)
            {
                CustomLog.UnityLog(e);
            }
            finally
            {
                if (this != null && GameManager.Instance != null)
                    GameManager.GetManager<FileLoadManager>().WorkingGenerators.Remove(this);
            }
        }

        private string GetModelPath(HeadType type)
        {
            return type switch
            {
                HeadType.Zombie => "item/zombie_head",
                HeadType.Skull => "item/skeleton_skull",
                HeadType.Witherskull => "item/wither_skeleton_skull",
                HeadType.Creeper => "item/creeper_head",
                HeadType.Piglin => "item/piglin_head",
                HeadType.Dragon => "item/dragon_head",
                _ => ""
            };
        }
        #endregion

        #region Debug
        [ContextMenu("Save Player Texture")]
        public void SavePlayerTexture()
        {
            var meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null || meshRenderer.sharedMaterial == null)
            {
                CustomLog.LogError("No material found.");
                return;
            }

            var texture = meshRenderer.sharedMaterial.mainTexture as Texture2D;
            if (texture == null)
            {
                CustomLog.LogError("Main texture is null.");
                return;
            }

            var path = Application.dataPath + "/../" + "PlayerHeadTexture.png";
            var bytes = texture.EncodeToPNG();
            System.IO.File.WriteAllBytes(path, bytes);
            CustomLog.Log("Player head texture saved to: " + path);
        }
        #endregion
    }
}