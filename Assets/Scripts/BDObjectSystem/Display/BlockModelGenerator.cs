using System;
using System.Collections.Generic;
using GameSystem;
using Minecraft;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace BDObjectSystem.Display
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class BlockModelGenerator : MonoBehaviour
    {
        public MinecraftModelData ModelData;
        public string modelName;
        public Color color = Color.white;

        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;

        // === 전역 머티리얼 캐시(텍스처 경로 → 공유 머티리얼) ===
        static readonly Dictionary<string, Material> s_matCache = new();

        // 고정 노멀(면 방향 기반)
        static readonly Vector3 N_Down = Vector3.down;
        static readonly Vector3 N_Up = Vector3.up;
        static readonly Vector3 N_North = Vector3.back;
        static readonly Vector3 N_South = Vector3.forward;
        static readonly Vector3 N_West = Vector3.left;
        static readonly Vector3 N_East = Vector3.right;

        private void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
        }

        /// <summary>
        /// 블록스테이트의 variant/array를 받아 모델/회전을 세팅한다.
        /// - 배열일 경우 weight 기반 선택(좌표 시드 등으로 일관성 유지하려면 PickSeed에 좌표를 넣으세요)
        /// - x/y 회전을 모델 회전으로 반영
        /// - uvlock=true면 UV는 모델 회전과 무관(현재 구현은 모델 회전이 UV에 영향 주지 않아 자연스럽게 만족)
        /// </summary>
        public void SetModelByBlockState(JToken modelInfo, Vector3Int? pickSeed = null)
        {
            string modelLocation;
            JObject modelObject;

            if (modelInfo.Type == JTokenType.Array)
            {
                var arr = (JArray)modelInfo;
                int idx = PickIndexByWeights(arr, pickSeed ?? Vector3Int.zero);
                modelObject = (JObject)arr[idx];
                modelLocation = modelObject["model"].ToString();
            }
            else
            {
                modelObject = modelInfo as JObject;
                modelLocation = modelObject["model"].ToString();
            }

            var xRot = modelObject.TryGetValue("x", out var xToken) ? xToken.Value<int>() : 0;
            var yRot = modelObject.TryGetValue("y", out var yToken) ? yToken.Value<int>() : 0;
            bool uvlock = modelObject.TryGetValue("uvlock", out var uvTok) && uvTok.Value<bool>();

            // 모델 전체 회전 (Minecraft 블록스테이트 규칙과 동일한 부호로 적용)
            var modelRotation = Quaternion.Euler(-xRot, -yRot, 0);

            // uvlock=true여도 본 구현은 UV를 자동/정방향 기준으로 계산하므로 추가 조치 불필요
            SetModel(modelLocation, modelRotation);

            // 트랜스폼은 항시 아이덴티티(메시만 회전 반영)
            transform.localRotation = Quaternion.identity;
        }

        public void SetModel(string modelLocation, Quaternion modelRotation)
        {
            modelLocation = MinecraftFileManager.RemoveNamespace(modelLocation);
            ModelData = MinecraftFileManager.GetModelData("models/" + modelLocation + ".json").UnpackParent();

            var bdManager = GameManager.GetManager<BdObjectManager>();

            // 기존 자식 오브젝트들 삭제
            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);

            GenerateCombinedMesh(ModelData, bdManager, modelRotation);
        }

        private void GenerateCombinedMesh(MinecraftModelData modelData, BdObjectManager bdManager, Quaternion modelRotation)
        {
            if (_meshFilter == null) _meshFilter = GetComponent<MeshFilter>();
            if (_meshRenderer == null) _meshRenderer = GetComponent<MeshRenderer>();

            // 투명 블록 머티리얼 선택
            bool isTransparent = false;
            ReadOnlySpan<string> transparent = new[] { "glass", "honey_block", "slime_block" };
            for (int i = 0; i < transparent.Length; i++)
            {
                if (!string.IsNullOrEmpty(modelName) && modelName.Contains(transparent[i]))
                {
                    isTransparent = true;
                    break;
                }
            }
            var baseMaterial = isTransparent ? bdManager.bdObjTransportMaterial : bdManager.bdobjBlockMaterial;

            // 용량 추정(요소 N * (최대 6면) * 4버텍스)
            int elemCount = modelData.Elements?.Count ?? 0;
            int estimatedVerts = Mathf.Max(24 * elemCount, 24);
            int estimatedFaces = Mathf.Max(6 * elemCount, 6);

            var vertices = new List<Vector3>(estimatedVerts);
            var uvs = new List<Vector2>(estimatedVerts);
            var normals = new List<Vector3>(estimatedVerts);

            var submeshTriangles = new List<List<int>>(Mathf.Max(1, estimatedFaces));
            var materials = new List<Material>(Mathf.Max(1, estimatedFaces));
            var materialDict = new Dictionary<string, int>(32);

            // (중요) 부모-조상까지 포함한 실제 반사 여부(우/좌손좌표) 판단
            bool invertWinding = IsMirrored(transform);

            // 요소 → 메시로 베이크
            for (int i = 0; i < elemCount; i++)
            {
                var element = modelData.Elements[i];
                ProcessElementForMesh(element, modelData, vertices, uvs, normals,
                                      submeshTriangles, materials, materialDict, baseMaterial, modelRotation, invertWinding);
            }

            var combinedMesh = new Mesh();
            combinedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // 큰 메시 대응
            combinedMesh.SetVertices(vertices);
            combinedMesh.SetUVs(0, uvs);
            combinedMesh.SetNormals(normals);
            combinedMesh.subMeshCount = submeshTriangles.Count;

            for (int i = 0; i < submeshTriangles.Count; i++)
                combinedMesh.SetTriangles(submeshTriangles[i], i);

            // 노멀은 직접 넣었으니 RecalculateNormals() 불필요
            combinedMesh.RecalculateBounds();

            _meshFilter.sharedMesh = combinedMesh;
            _meshRenderer.sharedMaterials = materials.ToArray();

            HandleSpecialBlockColors(modelData);
        }

        private void ProcessElementForMesh(
            JObject element, MinecraftModelData model,
            List<Vector3> vertices, List<Vector2> uvs, List<Vector3> normals,
            List<List<int>> submeshTriangles, List<Material> materials,
            Dictionary<string, int> materialDict, Material baseMaterial,
            Quaternion modelRotation, bool invertWinding)
        {
            // 1) from/to
            if (!element.TryGetValue("from", out var fromToken) || fromToken is not JArray fromArray) return;
            if (!element.TryGetValue("to", out var toToken) || toToken is not JArray toArray) return;

            var from = new Vector3(fromArray[0].Value<float>(), fromArray[1].Value<float>(), fromArray[2].Value<float>()) / 16.0f - Vector3.one * 0.5f;
            var to   = new Vector3(toArray[0].Value<float>(),   toArray[1].Value<float>(),   toArray[2].Value<float>())   / 16.0f - Vector3.one * 0.5f;

            // 2) 요소 회전 + rescale
            Quaternion elementRotation = Quaternion.identity;
            Vector3 origin = Vector3.zero;
            bool doRescale = false;
            char axisChar = 'y';
            float angleDeg = 0f;

            if (element["rotation"] is JObject rotationData)
            {
                if (rotationData["origin"] is JArray originArray)
                    origin = new Vector3(originArray[0].Value<float>(), originArray[1].Value<float>(), originArray[2].Value<float>()) / 16.0f - Vector3.one * 0.5f;

                string axisStr = rotationData["axis"]?.Value<string>() ?? "y";
                axisChar = axisStr.Length > 0 ? axisStr[0] : 'y';
                angleDeg = rotationData["angle"]?.Value<float>() ?? 0f;

                doRescale = rotationData.TryGetValue("rescale", out var resTok) && resTok.Value<bool>();
                var axis = axisChar == 'x' ? Vector3.right : axisChar == 'y' ? Vector3.up : Vector3.forward;
                elementRotation = Quaternion.AngleAxis(angleDeg, axis);
            }

            if (!element.TryGetValue("faces", out var facesToken) || facesToken is not JObject faces) return;

            // 3) 큐브 8버텍스
            Span<Vector3> cubeVerts = stackalloc Vector3[8];
            cubeVerts[0] = new Vector3(from.x, from.y, from.z);
            cubeVerts[1] = new Vector3(to.x,   from.y, from.z);
            cubeVerts[2] = new Vector3(to.x,   to.y,   from.z);
            cubeVerts[3] = new Vector3(from.x, to.y,   from.z);
            cubeVerts[4] = new Vector3(from.x, from.y, to.z);
            cubeVerts[5] = new Vector3(to.x,   from.y, to.z);
            cubeVerts[6] = new Vector3(to.x,   to.y,   to.z);
            cubeVerts[7] = new Vector3(from.x, to.y,   to.z);

            // rescale 팩터
            float rescaleFactor = 1f;
            if (doRescale && Mathf.Abs(angleDeg) > 0.0001f)
            {
                float cos = Mathf.Abs(Mathf.Cos(angleDeg * Mathf.Deg2Rad));
                if (cos < 1e-4f) cos = 1e-4f;
                rescaleFactor = Mathf.Min(1f / cos, 1.41421356f); // <= √2
            }

            // 월드 변환 행렬 (스케일 포함)
            Matrix4x4 worldMatrix = transform.localToWorldMatrix;
            Matrix4x4 invWorldMatrix = transform.worldToLocalMatrix;

            // 버텍스 변환: (origin 기준) → (rescale) → (요소 회전) → (모델 회전)
            for (int i = 0; i < 8; i++)
            {
                Vector3 rel = cubeVerts[i] - origin;

                if (doRescale && rescaleFactor != 1f)
                {
                    switch (axisChar)
                    {
                        case 'x': rel.y *= rescaleFactor; rel.z *= rescaleFactor; break;
                        case 'y': rel.x *= rescaleFactor; rel.z *= rescaleFactor; break;
                        case 'z': rel.x *= rescaleFactor; rel.y *= rescaleFactor; break;
                    }
                }

                Vector3 v = elementRotation * rel + origin;
                v = modelRotation * v;

                // 스케일 보정을 위해 월드 공간으로 변환 후 다시 로컬 공간으로 변환
                Vector3 worldPos = worldMatrix.MultiplyPoint3x4(v);
                cubeVerts[i] = invWorldMatrix.MultiplyPoint3x4(worldPos);
            }

            // 노멀 회전(라이팅 무시해도 와인딩 일관성 위해 유지)
            Quaternion normalRotation = modelRotation * elementRotation;

            // 4) 각 면
            foreach (var face in faces)
            {
                if (face.Value is not JObject faceData) continue;

                // Debug.Log(model.ToString());
                // Debug.Log(faceData.ToString());
                var textureName = DisplayObject.GetTexturePath(faceData["texture"].ToString(), model.Textures);

                if (!materialDict.TryGetValue(textureName, out int submeshIndex))
                {
                    submeshIndex = materials.Count;
                    materialDict[textureName] = submeshIndex;

                    if (!s_matCache.TryGetValue(textureName, out var sharedMat))
                    {
                        sharedMat = new Material(baseMaterial);
                        sharedMat.mainTexture = CreateTexture(textureName);
                        // ApplyMaterialFixups(sharedMat); // ★ 컬링 강제 Back
                        s_matCache[textureName] = sharedMat;
                    }
                    materials.Add(sharedMat);
                    submeshTriangles.Add(new List<int>(64));
                }

                string faceName = face.Key;

                // 페이스 버텍스 선택 (시계 기준 가정)
                Vector3 v0, v1, v2, v3;
                switch (faceName)
                {
                    case "down":  v0 = cubeVerts[0]; v1 = cubeVerts[1]; v2 = cubeVerts[5]; v3 = cubeVerts[4]; break;
                    case "up":    v0 = cubeVerts[2]; v1 = cubeVerts[3]; v2 = cubeVerts[7]; v3 = cubeVerts[6]; break;
                    case "north": v0 = cubeVerts[3]; v1 = cubeVerts[2]; v2 = cubeVerts[1]; v3 = cubeVerts[0]; break;
                    case "south": v0 = cubeVerts[5]; v1 = cubeVerts[6]; v2 = cubeVerts[7]; v3 = cubeVerts[4]; break;
                    case "west":  v0 = cubeVerts[7]; v1 = cubeVerts[3]; v2 = cubeVerts[0]; v3 = cubeVerts[4]; break;
                    case "east":  v0 = cubeVerts[1]; v1 = cubeVerts[2]; v2 = cubeVerts[6]; v3 = cubeVerts[5]; break;
                    default: continue;
                }

                // 기대 노멀(바깥 방향)
                Vector3 baseN = faceName switch
                {
                    "down"  => Vector3.down,
                    "up"    => Vector3.up,
                    "north" => Vector3.back,
                    "south" => Vector3.forward,
                    "west"  => Vector3.left,
                    "east"  => Vector3.right,
                    _       => Vector3.up
                };
                Vector3 nExpected = normalRotation * baseN;
                if (invertWinding) nExpected = -nExpected; // 부모 반사 시 보정

                // 실제 노멀(현재 정점 순서)
                Vector3 nActual = Vector3.Cross(v1 - v0, v2 - v0);

                // 와인딩 판정 (dot<0 → 뒤집기)
                bool windingOk = Vector3.Dot(nActual, nExpected) >= 0f;

                int vi = vertices.Count;
                vertices.Add(v0); vertices.Add(v1); vertices.Add(v2); vertices.Add(v3);

                var tris = submeshTriangles[submeshIndex];
                if (windingOk)
                {
                    tris.Add(vi); tris.Add(vi + 1); tris.Add(vi + 2);
                    tris.Add(vi); tris.Add(vi + 2); tris.Add(vi + 3);
                }
                else
                {
                    tris.Add(vi); tris.Add(vi + 2); tris.Add(vi + 1);
                    tris.Add(vi); tris.Add(vi + 3); tris.Add(vi + 2);
                    nExpected = -nExpected; // 노멀도 반전
                }

                // UV(자동 계산 + face.rotation)
                AddFaceUVs(faceName, from, to, faceData, uvs);

                // 노멀(반복)
                normals.Add(nExpected); normals.Add(nExpected); normals.Add(nExpected); normals.Add(nExpected);
            }
        }

        // === UV 자동계산 + face.rotation 반영 ===
        static void AddFaceUVs(string faceName, Vector3 from, Vector3 to, JObject faceData, List<Vector2> uvs)
        {
            float x1 = 0, y1 = 0, x2 = 1, y2 = 1;

            if (faceData.TryGetValue("uv", out var uvTok) && uvTok is JArray a && a.Count == 4)
            {
                x1 = a[0].Value<float>() / 16f;
                y1 = 1f - a[1].Value<float>() / 16f;
                x2 = a[2].Value<float>() / 16f;
                y2 = 1f - a[3].Value<float>() / 16f;
            }
            else
            {
                // 스펙 기본: uv 명시 없으면 면 방향 + from/to 기반 자동 UV
                switch (faceName)
                {
                    // 수평면: XZ 매핑
                    case "up":
                        x1 = from.x + 0.5f; x2 = to.x + 0.5f;
                        y1 = 1f - (from.z + 0.5f); y2 = 1f - (to.z + 0.5f);
                        break;
                    case "down":
                        x1 = from.x + 0.5f; x2 = to.x + 0.5f;
                        y1 = 1f - (to.z + 0.5f); y2 = 1f - (from.z + 0.5f);
                        break;

                    // 수직면: XY/ZY 매핑
                    case "north":
                        x1 = 1f - (to.x + 0.5f); x2 = 1f - (from.x + 0.5f);
                        y1 = 1f - (from.y + 0.5f); y2 = 1f - (to.y + 0.5f);
                        break;
                    case "south":
                        x1 = from.x + 0.5f; x2 = to.x + 0.5f;
                        y1 = 1f - (from.y + 0.5f); y2 = 1f - (to.y + 0.5f);
                        break;
                    case "west":
                        x1 = from.z + 0.5f; x2 = to.z + 0.5f;
                        y1 = 1f - (from.y + 0.5f); y2 = 1f - (to.y + 0.5f);
                        break;
                    case "east":
                        x1 = 1f - (to.z + 0.5f); x2 = 1f - (from.z + 0.5f);
                        y1 = 1f - (from.y + 0.5f); y2 = 1f - (to.y + 0.5f);
                        break;
                }

                x1 = Mathf.Clamp01(x1); x2 = Mathf.Clamp01(x2);
                y1 = Mathf.Clamp01(y1); y2 = Mathf.Clamp01(y2);
            }

            // face.rotation (0/90/180/270)
            int rot = (faceData.TryGetValue("rotation", out var rTok)) ? ((rTok.Value<int>() % 360 + 360) % 360) : 0;

            Vector2[] quad = { new(x1, y2), new(x2, y2), new(x2, y1), new(x1, y1) };

            int steps = rot / 90;
            if (steps != 0)
            {
                for (int s = 0; s < steps; s++)
                {
                    var t = quad[0];
                    quad[0] = quad[3];
                    quad[3] = quad[2];
                    quad[2] = quad[1];
                    quad[1] = t;
                }
            }

            uvs.Add(quad[0]); uvs.Add(quad[1]); uvs.Add(quad[2]); uvs.Add(quad[3]);
        }

        // === 면 방향 고정 노멀 추가 (미사용 가능) ===
        static void AddFaceNormals(string faceName, List<Vector3> normals)
        {
            Vector3 n = faceName switch
            {
                "down" => N_Down,
                "up" => N_Up,
                "north" => N_North,
                "south" => N_South,
                "west" => N_West,
                "east" => N_East,
                _ => N_Up
            };
            normals.Add(n); normals.Add(n); normals.Add(n); normals.Add(n);
        }

        private void HandleSpecialBlockColors(MinecraftModelData modelData)
        {
            if (string.IsNullOrEmpty(modelName)) return;

            if (modelName.Contains("redstone_wire"))
            {
                foreach (var mat in _meshRenderer.materials)
                    mat.color = Color.red;
            }
            else if (modelName.Contains("banner"))
            {
                foreach (var mat in _meshRenderer.materials)
                    mat.color = color;
            }
        }

        // HeadGenerator에서 오버라이드 하기 위함
        protected virtual Texture2D CreateTexture(string path)
        {
            return MinecraftFileManager.GetTextureFile(path);
        }

        // === weight 기반 가중치 선택(좌표 시드 넣으면 월드 재로딩 간 일관성 유지) ===
        static int PickIndexByWeights(JArray arr, Vector3Int seed)
        {
            int total = 0;
            var cum = new int[arr.Count];
            for (int i = 0; i < arr.Count; i++)
            {
                var o = (JObject)arr[i];
                int w = o.TryGetValue("weight", out var wTok) ? Mathf.Max(1, wTok.Value<int>()) : 1;
                total += w;
                cum[i] = total;
            }
            if (total <= 0) return 0;

            int r = Mathf.Abs(Hash(seed)) % total;
            for (int i = 0; i < cum.Length; i++)
                if (r < cum[i]) return i;
            return 0;
        }

        static int Hash(Vector3Int v)
        {
            unchecked
            {
                int h = 17;
                h = h * 31 + v.x;
                h = h * 31 + v.y;
                h = h * 31 + v.z;
                return h;
            }
        }

        // === 부모-조상 포함 미러 여부(행렬식 det<0)
        static bool IsMirrored(Transform t)
        {
            var m = t.localToWorldMatrix;
            float det =
                m.m00 * (m.m11 * m.m22 - m.m12 * m.m21) -
                m.m01 * (m.m10 * m.m22 - m.m12 * m.m20) +
                m.m02 * (m.m10 * m.m21 - m.m11 * m.m20);
            return det < 0f;
        }

        // === 머티리얼 컬링 강제(Front 컬링/양면 방지)
        // static void ApplyMaterialFixups(Material m)
        // {
        //     // 표준/URP/HDRP 호환을 위해 존재 여부 체크 후 설정
        //     if (m.HasProperty("_Cull")) m.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Back);
        //     if (m.HasProperty("_CullMode")) m.SetInt("_CullMode", (int)UnityEngine.Rendering.CullMode.Back);
        //     if (m.HasProperty("_DoubleSidedEnable")) m.SetInt("_DoubleSidedEnable", 0);

        //     // 흔한 키워드 방어
        //     m.DisableKeyword("_CULL_FRONT");
        //     m.DisableKeyword("_CULLMODE_FRONT");
        //     m.DisableKeyword("_DOUBLESIDED_ON");
        // }
    }
}
