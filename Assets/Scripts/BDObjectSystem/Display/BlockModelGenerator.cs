using System;
using System.Collections.Generic;
using System.Linq;
using GameSystem;
using Minecraft;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace BDObjectSystem.Display
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class BlockModelGenerator : MonoBehaviour
    {
        #region Internal Structs
        internal struct ApplySpec
        {
            public string Model;
            public int X, Y;
            public bool UvLock;
        }

        /// <summary>
        /// 메시 생성 과정에서 필요한 데이터들을 그룹화하여 관리합니다.
        /// </summary>
        protected class MeshGenerationData
        {
            public readonly List<Vector3> Vertices = new(1024);
            public readonly List<Vector2> Uvs = new(1024);
            public readonly List<Vector3> Normals = new(1024);
            public readonly List<List<int>> SubmeshTriangles = new(8);
            public readonly List<Material> Materials = new(8);
            public readonly Dictionary<string, int> MaterialDict = new(64);
            public Material BaseMaterial;
            public bool InvertWinding;
            // 새 플래그: 메시 생성 시 실제 텍스처/공유 매터리얼을 적용할지 여부
            public bool AssignMaterials = true;
        }

        /// <summary>
        /// 공통 메시 캐시 키
        /// </summary>
        private struct MeshCacheKey : IEquatable<MeshCacheKey>
        {
            public string ModelPath;
            public int RotationX;
            public int RotationY;
            public bool CenterPivot;
            public bool TopPivot; // 추가

            public bool Equals(MeshCacheKey other) =>
                ModelPath == other.ModelPath &&
                RotationX == other.RotationX &&
                RotationY == other.RotationY &&
                CenterPivot == other.CenterPivot &&
                TopPivot == other.TopPivot;

            public override bool Equals(object obj) => obj is MeshCacheKey other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = 17;
                    hash = hash * 31 + (ModelPath?.GetHashCode() ?? 0);
                    hash = hash * 31 + RotationX;
                    hash = hash * 31 + RotationY;
                    hash = hash * 31 + CenterPivot.GetHashCode();
                    hash = hash * 31 + TopPivot.GetHashCode();
                    return hash;
                }
            }
        }

        /// <summary>
        /// 메시 캐시 정보 (메시 + 사용된 텍스처 경로 목록)
        /// </summary>
        private class CachedMeshData
        {
            public Mesh Mesh;
            public List<string> TexturePaths; // submesh 순서대로 저장된 텍스처 경로
        }
        #endregion

        public MinecraftModelData ModelData { get; private set; }
        public string modelName;
        public Color color = Color.white;

        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;

        // 재질 캐시
        private static readonly Dictionary<string, Material> s_matCache = new();

        // 메시 캐시
        private static readonly Dictionary<MeshCacheKey, CachedMeshData> s_meshCache = new();
        private static readonly Dictionary<string, bool> s_isSimpleCube = new();

        public bool disableTextureCropping = false;
        public bool centerPivot = false; // true면 중심 또는 중심 상단 피봇
        public bool topPivot = false; // centerPivot이 true일 때, Y축을 상단으로 할지 여부

        private void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
        }

        #region Building
        public void BuildFromBlockState(string mName, string state, Vector3Int? pickSeed = null)
        {
            modelName = mName;
            var blockState = MinecraftFileManager.GetJsonData("blockstates/" + mName + ".json");
            var applies = ModelGeneratorHelper.CollectApplies(blockState, state, pickSeed ?? Vector3Int.zero);
            GenerateMeshFromApplies(applies);
        }

        public void BuildDirect(string modelPath)
        {
            modelName = MinecraftFileManager.RemoveNamespace(modelPath);

            // 가상의 ApplySpec 생성 (회전 없음, 기본 설정)
            var apply = new ApplySpec
            {
                Model = modelName,
                X = 0,
                Y = 0,
                UvLock = false
            };

            // 기존 메시 생성 파이프라인 재사용 (캐싱, 재질 할당 등 자동 처리)
            GenerateMeshFromApplies(new List<ApplySpec> { apply });
        }
        #endregion

        #region Mesh Generation
        internal void GenerateMeshFromApplies(List<ApplySpec> applies, bool assignMaterials = true)
        {
            var bdManager = GameManager.GetManager<BdObjectManager>();
            var meshData = new MeshGenerationData
            {
                AssignMaterials = assignMaterials
            };

            bool isTransparent = !string.IsNullOrEmpty(modelName) &&
                                 (modelName.Contains("glass") || modelName.Contains("honey_block") || modelName.Contains("slime_block"));
            meshData.BaseMaterial = isTransparent ? bdManager.bdObjTransportMaterial : bdManager.bdobjBlockMaterial;
            meshData.InvertWinding = ModelGeneratorHelper.IsMirrored(transform);

            if (applies.Count == 0)
            {
                // CustomLog.LogWarning($"No applies found for {modelName}. Creating empty mesh.");
                return;
            }

            // 단일 apply이고 캐시 가능한 경우 메시 재사용 시도
            if (applies.Count == 1 && TryUseCachedMesh(applies[0], meshData, assignMaterials))
            {
                return;
            }

            // 캐시 불가능한 경우 기존 방식으로 생성
            foreach (var apply in applies)
            {
                if (string.IsNullOrEmpty(apply.Model)) continue;
                var loc = MinecraftFileManager.RemoveNamespace(apply.Model);
                var data = MinecraftFileManager.GetModelData("models/" + loc + ".json").UnpackParent();
                var modelRot = Quaternion.Euler(-apply.X, -apply.Y, 0);

                BakeOneModel(data, modelRot, meshData);
                ModelData = data;
            }

            CreateAndAssignMesh(meshData);
        }

        /// <summary>
        /// 캐시된 메시를 사용할 수 있는지 확인하고 사용합니다.
        /// </summary>
        private bool TryUseCachedMesh(ApplySpec apply, MeshGenerationData meshData, bool assignMaterials)
        {
            var loc = MinecraftFileManager.RemoveNamespace(apply.Model);
            var fullPath = "models/" + loc + ".json";

            // 이 모델이 단순 큐브인지 확인 (캐시)
            if (!s_isSimpleCube.TryGetValue(fullPath, out bool isSimple))
            {
                var data = MinecraftFileManager.GetModelData(fullPath);
                if (data == null)
                    return false;

                isSimple = ModelGeneratorHelper.IsSimpleCubeModel(data);
                data = data.UnpackParent();
                s_isSimpleCube[fullPath] = isSimple;
            }

            if (!isSimple)
                return false;

            // 메시 캐시 키 생성 (centerPivot, topPivot 포함)
            var cacheKey = new MeshCacheKey
            {
                ModelPath = fullPath,
                RotationX = apply.X,
                RotationY = apply.Y,
                CenterPivot = centerPivot,
                TopPivot = topPivot
            };

            var modelData = MinecraftFileManager.GetModelData(fullPath);
            if (modelData == null)
                return false;

            modelData = modelData.UnpackParent();
            ModelData = modelData;

            // 컴포넌트 확인
            if (_meshFilter == null) _meshFilter = GetComponent<MeshFilter>();
            if (_meshRenderer == null) _meshRenderer = GetComponent<MeshRenderer>();

            if (_meshFilter == null || _meshRenderer == null)
            {
                CustomLog.LogError("MeshFilter or MeshRenderer is null");
                return false;
            }

            // 캐시된 메시가 있으면 재사용
            if (s_meshCache.TryGetValue(cacheKey, out var cachedData))
            {
                _meshFilter.sharedMesh = cachedData.Mesh;

                if (assignMaterials)
                {
                    if (!AssignMaterialsFromCache(cachedData.TexturePaths, meshData))
                    {
                        return false;
                    }
                }
                else
                {
                    // assignMaterials == false면 플레이스홀더 재질 생성하여 적용
                    var placeholder = new Material[cachedData.TexturePaths.Count];
                    for (int i = 0; i < placeholder.Length; i++)
                    {
                        placeholder[i] = new Material(meshData.BaseMaterial);
                        // mainTexture는 설정하지 않음 (나중에 HeadGenerator에서 적용)
                    }
                    _meshRenderer.sharedMaterials = placeholder;
                }

                HandleSpecialBlockColors();
                return true;
            }

            // 캐시에 없으면 생성하고 캐시에 저장
            var modelRot = Quaternion.Euler(-apply.X, -apply.Y, 0);

            BakeOneModel(modelData, modelRot, meshData);

            if (meshData.Materials.Count == 0)
            {
                CustomLog.LogWarning($"No materials generated for {fullPath}");
                return false;
            }

            var mesh = CreateMesh(meshData);

            // 사용된 텍스처 경로 저장 (submesh 순서대로)
            var texturePaths = new List<string>(meshData.MaterialDict.Count);
            var sortedMaterials = meshData.MaterialDict.OrderBy(kv => kv.Value).ToList();
            foreach (var kv in sortedMaterials)
            {
                texturePaths.Add(kv.Key);
            }

            s_meshCache[cacheKey] = new CachedMeshData
            {
                Mesh = mesh,
                TexturePaths = texturePaths
            };

            _meshFilter.sharedMesh = mesh;
            _meshRenderer.sharedMaterials = meshData.Materials.ToArray();
            HandleSpecialBlockColors();

            return true;
        }

        /// <summary>
        /// 메시 데이터로부터 Mesh 객체를 생성합니다.
        /// </summary>
        private Mesh CreateMesh(MeshGenerationData meshData)
        {
            if (meshData.Vertices.Count > 0)
            {
                if (centerPivot)
                {
                    var min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
                    var max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

                    foreach (var v in meshData.Vertices)
                    {
                        min.x = Mathf.Min(min.x, v.x);
                        min.y = Mathf.Min(min.y, v.y);
                        min.z = Mathf.Min(min.z, v.z);
                        max.x = Mathf.Max(max.x, v.x);
                        max.y = Mathf.Max(max.y, v.y);
                        max.z = Mathf.Max(max.z, v.z);
                    }

                    Vector3 pivot;
                    if (topPivot)
                    {
                        // 머리: X, Z는 중심, Y는 상단
                        pivot = new Vector3((min.x + max.x) * 0.5f, max.y, (min.z + max.z) * 0.5f);
                    }
                    else
                    {
                        // 아이템: 완전한 중심
                        pivot = (min + max) * 0.5f;
                    }

                    for (int i = 0; i < meshData.Vertices.Count; i++)
                    {
                        meshData.Vertices[i] -= pivot;
                    }
                }
                else
                {
                    // 블록: 하단 피봇 (최솟값)
                    var min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
                    foreach (var v in meshData.Vertices)
                    {
                        min.x = Mathf.Min(min.x, v.x);
                        min.y = Mathf.Min(min.y, v.y);
                        min.z = Mathf.Min(min.z, v.z);
                    }
                    for (int i = 0; i < meshData.Vertices.Count; i++)
                    {
                        meshData.Vertices[i] -= min;
                    }
                }
            }

            var combined = new Mesh { name = modelName, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            combined.SetVertices(meshData.Vertices);
            combined.SetUVs(0, meshData.Uvs);
            combined.SetNormals(meshData.Normals);
            combined.subMeshCount = meshData.SubmeshTriangles.Count;

            for (int i = 0; i < meshData.SubmeshTriangles.Count; i++)
            {
                combined.SetTriangles(meshData.SubmeshTriangles[i], i);
            }

            combined.RecalculateBounds();
            return combined;
        }

        /// <summary>
        /// 캐시된 텍스처 경로 목록을 사용하여 재질을 할당합니다.
        /// </summary>
        private bool AssignMaterialsFromCache(List<string> texturePaths, MeshGenerationData meshData)
        {
            if (texturePaths == null || texturePaths.Count == 0)
            {
                CustomLog.LogWarning($"No texture paths in cached data for {modelName}");
                return false;
            }

            var materials = new Material[texturePaths.Count];

            for (int i = 0; i < texturePaths.Count; i++)
            {
                var texturePath = texturePaths[i];

                if (!s_matCache.TryGetValue(texturePath, out var sharedMat))
                {
                    var texture = CreateTexture(texturePath);
                    if (texture == null)
                    {
                        CustomLog.LogWarning($"Failed to create texture: {texturePath}");
                        return false;
                    }

                    texture.filterMode = FilterMode.Point;
                    texture.wrapMode = TextureWrapMode.Clamp;

                    sharedMat = new Material(meshData.BaseMaterial);
                    sharedMat.mainTexture = texture;
                    s_matCache[texturePath] = sharedMat;
                }
                materials[i] = sharedMat;
            }

            _meshRenderer.sharedMaterials = materials;
            return true;
        }

        private void BakeOneModel(MinecraftModelData modelData, Quaternion modelRotation, MeshGenerationData meshData)
        {
            if (modelData.Elements == null) return;
            foreach (var element in modelData.Elements)
            {
                ProcessElementForMesh(element, modelData, modelRotation, meshData);
            }
        }

        private void CreateAndAssignMesh(MeshGenerationData meshData)
        {
            if (_meshFilter == null) _meshFilter = GetComponent<MeshFilter>();
            if (_meshRenderer == null) _meshRenderer = GetComponent<MeshRenderer>();

            var mesh = CreateMesh(meshData);
            _meshFilter.sharedMesh = mesh;
            _meshRenderer.sharedMaterials = meshData.Materials.ToArray();

            HandleSpecialBlockColors();
        }
        #endregion

        #region Element Processing
        private void ProcessElementForMesh(JObject element, MinecraftModelData model, Quaternion modelRotation, MeshGenerationData meshData)
        {
            if (!element.TryGetValue("from", out var fromTok) || fromTok is not JArray fromArr ||
                !element.TryGetValue("to", out var toTok) || toTok is not JArray toArr ||
                !element.TryGetValue("faces", out var facesTok) || facesTok is not JObject faces)
            {
                return;
            }

            var from = new Vector3(fromArr[0].Value<float>(), fromArr[1].Value<float>(), fromArr[2].Value<float>()) / 16f - Vector3.one * 0.5f;
            var to = new Vector3(toArr[0].Value<float>(), toArr[1].Value<float>(), toArr[2].Value<float>()) / 16f - Vector3.one * 0.5f;

            var (elementRotation, origin, rescaleFactor, axisChar) = ModelGeneratorHelper.ParseElementRotation(element);
            var finalRotation = modelRotation * elementRotation;

            Span<Vector3> cubeVerts = stackalloc Vector3[8];
            ModelGeneratorHelper.CalculateTransformedVertices(cubeVerts, from, to, origin, elementRotation, modelRotation, rescaleFactor, axisChar);

            foreach (var face in faces)
            {
                if (face.Value is not JObject faceData) continue;
                ProcessFace(face.Key, faceData, model, finalRotation, cubeVerts, from, to, meshData);
            }
        }

        private void ProcessFace(string faceName, JObject faceData, MinecraftModelData model, Quaternion finalRotation, Span<Vector3> cubeVerts, Vector3 from, Vector3 to, MeshGenerationData meshData)
        {
            var textureName = DisplayObject.GetTexturePath(faceData["texture"].ToString(), model.Textures);
            if (!meshData.MaterialDict.TryGetValue(textureName, out int submeshIndex))
            {
                submeshIndex = meshData.Materials.Count;
                meshData.MaterialDict[textureName] = submeshIndex;
                if (!s_matCache.TryGetValue(textureName, out var sharedMat))
                {
                    if (meshData.AssignMaterials)
                    {
                        var texture = CreateTexture(textureName);
                        if (texture != null)
                        {
                            texture.filterMode = FilterMode.Point;  // 픽셀 아트 스타일 유지
                            texture.wrapMode = TextureWrapMode.Clamp;
                        }
                        sharedMat = new Material(meshData.BaseMaterial);
                        sharedMat.mainTexture = texture;
                        s_matCache[textureName] = sharedMat;
                    }
                    else
                    {
                        // 할당을 하지 않는 경우 플레이스홀더 재질을 사용 (나중에 외부에서 텍스처 적용)
                        sharedMat = new Material(meshData.BaseMaterial);
                    }
                }
                meshData.Materials.Add(sharedMat);
                meshData.SubmeshTriangles.Add(new List<int>(64));
            }

            var (v0, v1, v2, v3) = ModelGeneratorHelper.GetFaceVertices(faceName, cubeVerts);
            var baseNormal = ModelGeneratorHelper.GetFaceBaseNormal(faceName);
            var expectedNormal = finalRotation * baseNormal;
            if (meshData.InvertWinding) expectedNormal = -expectedNormal;

            bool needsFlip = Vector3.Dot(Vector3.Cross(v1 - v0, v2 - v0), expectedNormal) < 0;

            int vi = meshData.Vertices.Count;
            meshData.Vertices.Add(v0); meshData.Vertices.Add(v1); meshData.Vertices.Add(v2); meshData.Vertices.Add(v3);

            var tris = meshData.SubmeshTriangles[submeshIndex];
            if (needsFlip)
            {
                tris.Add(vi); tris.Add(vi + 2); tris.Add(vi + 1);
                tris.Add(vi); tris.Add(vi + 3); tris.Add(vi + 2);
            }
            else
            {
                tris.Add(vi); tris.Add(vi + 1); tris.Add(vi + 2);
                tris.Add(vi); tris.Add(vi + 2); tris.Add(vi + 3);
            }

            ModelGeneratorHelper.AddFaceUVs(faceName, from, to, faceData, meshData.Uvs);
            var finalNormal = needsFlip ? -expectedNormal : expectedNormal;
            meshData.Normals.Add(finalNormal); meshData.Normals.Add(finalNormal); meshData.Normals.Add(finalNormal); meshData.Normals.Add(finalNormal);
        }
        #endregion

        #region Utility
        private void HandleSpecialBlockColors()
        {
            if (string.IsNullOrEmpty(modelName)) return;
            if (modelName.Contains("redstone_wire"))
            {
                foreach (var mat in _meshRenderer.materials) mat.color = Color.red;
            }
            else if (modelName.Contains("banner"))
            {
                foreach (var mat in _meshRenderer.materials) mat.color = color;
            }
            else if (modelName.Contains("grass_block"))
            {
                // 상단 Material만 0x7cbd6b으로 변경
                _meshRenderer.materials[1].color = new Color(124f / 255f, 189f / 255f, 107f / 255f);
            }
            else if (modelName.Contains("grass"))
            {
                foreach (var mat in _meshRenderer.materials) mat.color = new Color(124f / 255f, 189f / 255f, 107f / 255f);
            }
        }

        protected Texture2D CreateTexture(string path)
        {
            var originalTexture = MinecraftFileManager.GetTextureFile(path);
            if (originalTexture == null) return null;

            // 텍스처 자르기가 비활성화되어 있으면 원본을 복사하여 반환
            if (disableTextureCropping)
            {
                // 읽기 가능한 복사본 생성
                var readableTexture = new Texture2D(originalTexture.width, originalTexture.height, originalTexture.format, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };

                // RenderTexture를 통한 복사 (읽기 불가능한 텍스처도 처리 가능)
                RenderTexture tmp = RenderTexture.GetTemporary(
                    originalTexture.width,
                    originalTexture.height,
                    0,
                    RenderTextureFormat.Default,
                    RenderTextureReadWrite.Linear);

                Graphics.Blit(originalTexture, tmp);
                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = tmp;

                readableTexture.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
                readableTexture.Apply();

                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(tmp);

                return readableTexture;
            }

            // 텍스처가 직사각형인 경우 (애니메이션 텍스처 등)
            if (originalTexture.width != originalTexture.height)
            {
                // 너비를 기준으로 정사각형으로 자릅니다.
                int size = originalTexture.width;
                if (originalTexture.height > size)
                {
                    var croppedTexture = new Texture2D(size, size, originalTexture.format, false)
                    {
                        filterMode = FilterMode.Point,
                        wrapMode = TextureWrapMode.Clamp
                    };

                    // RenderTexture를 통한 복사로 변경
                    RenderTexture tmp = RenderTexture.GetTemporary(
                        originalTexture.width,
                        originalTexture.height,
                        0,
                        RenderTextureFormat.Default,
                        RenderTextureReadWrite.Linear);

                    Graphics.Blit(originalTexture, tmp);
                    RenderTexture previous = RenderTexture.active;
                    RenderTexture.active = tmp;

                    // 원본 텍스처의 윗부분(첫 프레임)을 복사합니다.
                    croppedTexture.ReadPixels(new Rect(0, originalTexture.height - size, size, size), 0, 0);
                    croppedTexture.Apply();

                    RenderTexture.active = previous;
                    RenderTexture.ReleaseTemporary(tmp);

                    return croppedTexture;
                }
            }

            // 정사각형 텍스처는 복사본 생성
            var finalTexture = new Texture2D(originalTexture.width, originalTexture.height, originalTexture.format, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            RenderTexture tmpFinal = RenderTexture.GetTemporary(
                originalTexture.width,
                originalTexture.height,
                0,
                RenderTextureFormat.Default,
                RenderTextureReadWrite.Linear);

            Graphics.Blit(originalTexture, tmpFinal);
            RenderTexture prevFinal = RenderTexture.active;
            RenderTexture.active = tmpFinal;

            finalTexture.ReadPixels(new Rect(0, 0, tmpFinal.width, tmpFinal.height), 0, 0);
            finalTexture.Apply();

            RenderTexture.active = prevFinal;
            RenderTexture.ReleaseTemporary(tmpFinal);

            return finalTexture;
        }

        /// <summary>
        /// 메시 캐시를 초기화합니다 (메모리 정리용).
        /// </summary>
        public static void ClearMeshCache()
        {
            foreach (var cachedData in s_meshCache.Values)
            {
                if (cachedData?.Mesh != null) Destroy(cachedData.Mesh);
            }
            s_meshCache.Clear();
            s_isSimpleCube.Clear();
        }
        #endregion
    }
}