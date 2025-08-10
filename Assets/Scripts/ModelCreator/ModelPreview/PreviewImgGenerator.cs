using System;
using System.Collections.Generic;
using System.IO;
using BDObjectSystem;
using BDObjectSystem.Display;
using Cysharp.Threading.Tasks;
using GameSystem; // BdObjectManager를 사용하기 위해 추가
using Minecraft;
using UnityEditor;
using UnityEngine;

namespace ModelCreator.ModelPreview
{
    public class PreviewImgGenerator : MonoBehaviour
    {
        public static PreviewImgGenerator Instance { get; private set; }
        public Camera previewCamera;

        [HideInInspector]
        public string outputFolder = "Preview";
        Dictionary<string, Texture2D> previewTextures = new Dictionary<string, Texture2D>();

        void Awake()
        {
            if (Instance != null)
            {
                Destroy(Instance.gameObject);
                return;
            }
            Instance = this;

            LoadSavedImagesFromStreamingAsset();
            LoadSavedImagesFromDataPath();
        }

        public Texture2D GetModelTexture(BdObject.DisplayType modelType, string modelName)
        {
            string textureName = modelType switch
            {
                BdObject.DisplayType.BlockDisplay => $"{modelName}_block",
                BdObject.DisplayType.ItemDisplay => $"{modelName}_item",
                _ => modelName
            };
            if (previewTextures.TryGetValue(textureName, out var texture))
            {
                return texture;
            }
            return null;
        }

        private void LoadSavedImagesFromStreamingAsset()
        {
            string outputPath = Path.Combine(Application.streamingAssetsPath, outputFolder);
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            var files = Directory.GetFiles(outputPath, "*.png");
            foreach (var file in files)
            {
                var texture = new Texture2D(2, 2);
                byte[] fileData = File.ReadAllBytes(file);
                texture.LoadImage(fileData);
                string fileName = Path.GetFileNameWithoutExtension(file);
                previewTextures[fileName] = texture;
            }
        }

        private void LoadSavedImagesFromDataPath()
        {
            string outputPath = Path.Combine(Application.dataPath, outputFolder);
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            var files = Directory.GetFiles(outputPath, "*.png");
            foreach (var file in files)
            {
                var texture = new Texture2D(2, 2);
                byte[] fileData = File.ReadAllBytes(file);
                texture.LoadImage(fileData);
                string fileName = Path.GetFileNameWithoutExtension(file);
                previewTextures[fileName] = texture;
            }
        }

#if UNITY_EDITOR

        [MenuItem("Tools/Generate All Preview Images")]
        public static void GenerateAllPreviewImages()
        {
            if (Instance == null)
            {
                Debug.LogError("PreviewImgGenerator 인스턴스가 씬에 존재하지 않습니다.");
                return;
            }

            if (!MinecraftFileManager.Instance.IsReadedFiles)
            {
                EditorUtility.DisplayDialog("오류", "마인크래프트 파일이 아직 로드되지 않았습니다. 먼저 파일을 로드해주세요.", "확인");
                return;
            }

            // 비동기 작업을 시작합니다.
            Instance.StartGeneration().Forget();
        }

        private async UniTaskVoid StartGeneration()
        {
            // ! - blockstates/~ 인데
            // ! - blockstates/models/block/~ 로 경로 처리해버림
            
            var modelFiles = MinecraftFileManager.Instance.AllJsonFiles;
            int totalCount = modelFiles.Count;
            int processedCount = 0;
            
            // 생성된 프리뷰 이미지를 담을 임시 오브젝트
            var previewObjectRoot = new GameObject("PreviewObjectRoot");
            previewObjectRoot.transform.position = previewCamera.transform.position + previewCamera.transform.forward * 5f; // 카메라 앞에 배치

            try
            {
                foreach (var modelEntry in modelFiles)
                {
                    processedCount++;
                    string modelPath = modelEntry.Key;
                    
                    // "models/"로 시작하는 json 파일만 처리합니다. (blockstates 등 제외)
                    if (!modelPath.StartsWith("models/")) continue;

                    // 진행 상황 표시
                    EditorUtility.DisplayProgressBar("프리뷰 생성 중", $"{modelPath} ({processedCount}/{totalCount})", (float)processedCount / totalCount);

                    // 기존에 생성된 자식 오브젝트들을 모두 삭제
                    foreach (Transform child in previewObjectRoot.transform)
                    {
                        Destroy(child.gameObject);
                    }

                    // 모델 생성
                    // var modelData = MinecraftFileManager.GetModelData(modelPath.Replace(".json", ""));
                    // if (modelData == null) continue;
                    // var display = new BdObject.Display(BdObject.DisplayType.BlockDisplay, modelData);
                    // var modelObject = display.CreateModel(previewObjectRoot.transform);

                    // --- 수정된 모델 생성 로직 ---
                    var modelObject = CreatePreviewModel(modelPath, previewObjectRoot.transform);
                    if (modelObject == null)
                    {
                        Debug.LogWarning($"모델 생성 실패: {modelPath}");
                        continue;
                    }
                    // --- 로직 수정 끝 ---
                    
                    // 모델의 경계를 계산하고 카메라 프레임에 맞춤
                    Bounds bounds = CalculateBounds(modelObject);
                    FitToBounds(previewCamera, bounds);

                    // 텍스처 생성 및 저장
                    Texture2D previewTexture = CapturePreview(256, 256);
                    SaveTextureToFile(previewTexture, modelPath);
                    
                    // 에디터가 멈추지 않도록 한 프레임 대기
                    await UniTask.Yield();
                }
            }
            finally
            {
                // 작업 완료 후 프로그레스 바를 닫고 임시 오브젝트를 삭제
                EditorUtility.ClearProgressBar();
                Destroy(previewObjectRoot);
                Debug.Log("프리뷰 이미지 생성 완료!");
            }
        }

        /// <summary>
        /// 모델 경로를 기반으로 프리뷰용 게임 오브젝트를 생성합니다.
        /// </summary>
        /// <param name="modelPath">"models/block/stone.json"과 같은 모델 경로</param>
        /// <param name="parent">생성될 오브젝트의 부모 트랜스폼</param>
        /// <returns>생성된 게임 오브젝트. 실패 시 null을 반환합니다.</returns>
        private GameObject CreatePreviewModel(string modelPath, Transform parent)
        {
            var manager = GameManager.GetManager<BdObjectManager>();
            if (manager == null)
            {
                Debug.LogError("BdObjectManager를 찾을 수 없습니다.");
                return null;
            }

            // 모델 경로에서 ".json" 확장자를 제거하여 모델 이름으로 사용합니다.
            string modelName = modelPath.Replace(".json", "");
            ModelDisplayObject displayObj = null;

            // 모델 경로에 따라 블록 또는 아이템 디스플레이를 생성합니다.
            if (modelPath.Contains("/block/"))
            {
                displayObj = Instantiate(manager.blockDisplay, parent);
                displayObj.LoadDisplayModel(modelName, ""); // 프리뷰이므로 상태(state)는 비워둡니다.
            }
            else if (modelPath.Contains("/item/"))
            {
                displayObj = Instantiate(manager.itemDisplay, parent);
                displayObj.LoadDisplayModel(modelName, "");
            }
            else
            {
                // 블록이나 아이템 모델이 아니면 건너뜁니다.
                return null;
            }

            return displayObj.gameObject;
        }

        private Bounds CalculateBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.zero);

            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
            return bounds;
        }

        private void FitToBounds(Camera cam, Bounds bounds)
        {
            float cameraDistance = 2.5f; // 오브젝트와 카메라의 기본 거리
            Vector3 objectSizes = bounds.size;
            float objectSize = Mathf.Max(objectSizes.x, objectSizes.y, objectSizes.z);
            float cameraView = 2.0f * Mathf.Tan(0.5f * Mathf.Deg2Rad * cam.fieldOfView);
            float distance = cameraDistance * objectSize / cameraView;
            distance += 0.5f * objectSize;
            
            cam.transform.position = bounds.center - distance * cam.transform.forward;
        }

        private Texture2D CapturePreview(int width, int height)
        {
            RenderTexture rt = new RenderTexture(width, height, 24);
            previewCamera.targetTexture = rt;
            
            Texture2D screenShot = new Texture2D(width, height, TextureFormat.RGBA32, false);
            previewCamera.Render();
            
            RenderTexture.active = rt;
            screenShot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            screenShot.Apply();
            
            previewCamera.targetTexture = null;
            RenderTexture.active = null;
            Destroy(rt);
            
            return screenShot;
        }

        private void SaveTextureToFile(Texture2D texture, string modelPath)
        {
            byte[] bytes = texture.EncodeToPNG();
            string fileName = Path.GetFileNameWithoutExtension(modelPath) + ".png";
            string outputPath = Path.Combine(Application.streamingAssetsPath, outputFolder, fileName);
            
            File.WriteAllBytes(outputPath, bytes);
        }

#endif
    }
}
