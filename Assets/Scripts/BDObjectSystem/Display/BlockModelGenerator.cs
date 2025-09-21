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
        }
        #endregion

        public MinecraftModelData ModelData { get; private set; }
        public string modelName;
        public Color color = Color.white;

        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;

        private static readonly Dictionary<string, Material> s_matCache = new();

        private void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
        }

        #region Building from BlockState
        public void BuildFromBlockState(string mName, string state, Vector3Int? pickSeed = null)
        {
            modelName = mName;
            var blockState = MinecraftFileManager.GetJsonData("blockstates/" + mName + ".json");
            var applies = CollectApplies(blockState, state, pickSeed ?? Vector3Int.zero);
            GenerateMeshFromApplies(applies);
        }
        #endregion
        

        #region BlockState Parsing
        private List<ApplySpec> CollectApplies(JObject blockState, string state, Vector3Int seed)
        {
            var list = new List<ApplySpec>(8);
            if (blockState == null) return list;

            if (blockState.TryGetValue("variants", out var variantTok) && variantTok is JObject variants)
            {
                if (variants.TryGetValue(state, out var sel))
                {
                    NormalizeApply(sel, seed, list);
                }
            }
            else if (blockState.TryGetValue("multipart", out var multiTok) && multiTok is JArray multipart)
            {
                foreach (var item in multipart)
                {
                    if (item is not JObject part) continue;
                    bool ok = !part.TryGetValue("when", out var whenTok) ||
                              (whenTok is JObject whenObj && CheckState(whenObj, state));

                    if (ok && part.TryGetValue("apply", out var applyTok))
                    {
                        NormalizeApply(applyTok, seed, list);
                    }
                }
            }
            return list;
        }

        private void NormalizeApply(JToken tok, Vector3Int seed, List<ApplySpec> dst)
        {
            if (tok is JObject single)
            {
                dst.Add(ToApplySpec(single));
            }
            else if (tok is JArray arr && arr.Count > 0)
            {
                int idx = PickIndexByWeights(arr, seed);
                dst.Add(ToApplySpec((JObject)arr[idx]));
            }
        }

        private ApplySpec ToApplySpec(JObject obj)
        {
            return new ApplySpec
            {
                Model = obj["model"]?.ToString() ?? "",
                X = obj.TryGetValue("x", out var xTok) ? xTok.Value<int>() : 0,
                Y = obj.TryGetValue("y", out var yTok) ? yTok.Value<int>() : 0,
                UvLock = obj.TryGetValue("uvlock", out var uTok) && uTok.Value<bool>()
            };
        }

        private static bool CheckState(JObject when, string state)
        {
            if (when.TryGetValue("OR", out var orTok) && orTok is JArray orArr)
            {
                foreach (var item in orArr)
                    if (item is JObject jObj && CheckStateName(jObj, state)) return true;
                return false;
            }
            if (when.TryGetValue("AND", out var andTok) && andTok is JArray andArr)
            {
                foreach (var item in andArr)
                    if (item is not JObject jObj || !CheckStateName(jObj, state)) return false;
                return true;
            }
            return CheckStateName(when, state);
        }

        private static bool CheckStateName(JObject checks, string state)
        {
            if (string.IsNullOrEmpty(state)) return checks.Count == 0;

            var pairs = state.Split(',');
            foreach (var kv in checks)
            {
                bool matched = false;
                foreach (var pair in pairs)
                {
                    var sp = pair.Split('=');
                    if (sp.Length != 2 || sp[0] != kv.Key) continue;

                    var values = kv.Value.ToString().Split('|');
                    foreach (var value in values)
                    {
                        if (value == sp[1])
                        {
                            matched = true;
                            break;
                        }
                    }
                    break;
                }
                if (!matched) return false;
            }
            return true;
        }
        #endregion

        #region Mesh Generation
        internal void GenerateMeshFromApplies(List<ApplySpec> applies)
        {
            var bdManager = GameManager.GetManager<BdObjectManager>();
            var meshData = new MeshGenerationData();

            bool isTransparent = !string.IsNullOrEmpty(modelName) &&
                                 (modelName.Contains("glass") || modelName.Contains("honey_block") || modelName.Contains("slime_block"));
            meshData.BaseMaterial = isTransparent ? bdManager.bdObjTransportMaterial : bdManager.bdobjBlockMaterial;
            meshData.InvertWinding = IsMirrored(transform);

            if (applies.Count == 0)
            {
                Debug.LogWarning($"No applies found for {modelName}. Creating empty mesh.");
            }

            foreach (var apply in applies)
            {
                if (string.IsNullOrEmpty(apply.Model)) continue;
                var loc = MinecraftFileManager.RemoveNamespace(apply.Model);
                var data = MinecraftFileManager.GetModelData("models/" + loc + ".json").UnpackParent();
                var modelRot = Quaternion.Euler(-apply.X, -apply.Y, 0);

                BakeOneModel(data, modelRot, meshData);
                ModelData = data; // Store the last model data as representative
            }

            CreateAndAssignMesh(meshData);
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

            // Pivot to bottom-left-front corner
            if (meshData.Vertices.Count > 0)
            {
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
            _meshFilter.sharedMesh = combined;
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

            var (elementRotation, origin, rescaleFactor, axisChar) = ParseElementRotation(element);
            var finalRotation = modelRotation * elementRotation;

            Span<Vector3> cubeVerts = stackalloc Vector3[8];
            CalculateTransformedVertices(cubeVerts, from, to, origin, elementRotation, modelRotation, rescaleFactor, axisChar);

            foreach (var face in faces)
            {
                if (face.Value is not JObject faceData) continue;
                ProcessFace(face.Key, faceData, model, finalRotation, cubeVerts, from, to, meshData);
            }
        }

        private (Quaternion rot, Vector3 origin, float rescale, char axis) ParseElementRotation(JObject element)
        {
            if (element["rotation"] is not JObject rotData)
                return (Quaternion.identity, Vector3.zero, 1f, 'y');

            var origin = Vector3.zero;
            if (rotData["origin"] is JArray originArr)
                origin = new Vector3(originArr[0].Value<float>(), originArr[1].Value<float>(), originArr[2].Value<float>()) / 16f - Vector3.one * 0.5f;

            var axisStr = rotData["axis"]?.Value<string>() ?? "y";
            var axisChar = axisStr.Length > 0 ? axisStr[0] : 'y';
            var angle = rotData["angle"]?.Value<float>() ?? 0f;
            var doRescale = rotData.TryGetValue("rescale", out var resTok) && resTok.Value<bool>();

            var axisVec = axisChar == 'x' ? Vector3.right : axisChar == 'y' ? Vector3.up : Vector3.forward;
            var rot = Quaternion.AngleAxis(angle, axisVec);

            float rescale = 1f;
            if (doRescale && Mathf.Abs(angle) > 1e-4f)
            {
                float cos = Mathf.Abs(Mathf.Cos(angle * Mathf.Deg2Rad));
                rescale = 1f / Mathf.Max(cos, 1e-4f);
            }
            return (rot, origin, rescale, axisChar);
        }

        private void CalculateTransformedVertices(Span<Vector3> cubeVerts, Vector3 from, Vector3 to, Vector3 origin, Quaternion elementRotation, Quaternion modelRotation, float rescaleFactor, char axisChar)
        {
            cubeVerts[0] = new Vector3(from.x, from.y, from.z);
            cubeVerts[1] = new Vector3(to.x, from.y, from.z);
            cubeVerts[2] = new Vector3(to.x, to.y, from.z);
            cubeVerts[3] = new Vector3(from.x, to.y, from.z);
            cubeVerts[4] = new Vector3(from.x, from.y, to.z);
            cubeVerts[5] = new Vector3(to.x, from.y, to.z);
            cubeVerts[6] = new Vector3(to.x, to.y, to.z);
            cubeVerts[7] = new Vector3(from.x, to.y, to.z);

            for (int i = 0; i < 8; i++)
            {
                Vector3 rel = cubeVerts[i] - origin;
                if (rescaleFactor != 1f)
                {
                    switch (axisChar)
                    {
                        case 'x': rel.y *= rescaleFactor; rel.z *= rescaleFactor; break;
                        case 'y': rel.x *= rescaleFactor; rel.z *= rescaleFactor; break;
                        case 'z': rel.x *= rescaleFactor; rel.y *= rescaleFactor; break;
                    }
                }
                cubeVerts[i] = modelRotation * (elementRotation * rel + origin);
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
                    sharedMat = new Material(meshData.BaseMaterial);
                    sharedMat.mainTexture = CreateTexture(textureName);
                    s_matCache[textureName] = sharedMat;
                }
                meshData.Materials.Add(sharedMat);
                meshData.SubmeshTriangles.Add(new List<int>(64));
            }

            var (v0, v1, v2, v3) = GetFaceVertices(faceName, cubeVerts);
            var baseNormal = GetFaceBaseNormal(faceName);
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

            AddFaceUVs(faceName, from, to, faceData, meshData.Uvs);
            var finalNormal = needsFlip ? -expectedNormal : expectedNormal;
            meshData.Normals.Add(finalNormal); meshData.Normals.Add(finalNormal); meshData.Normals.Add(finalNormal); meshData.Normals.Add(finalNormal);
        }

        private static (Vector3, Vector3, Vector3, Vector3) GetFaceVertices(string faceName, Span<Vector3> verts) => faceName switch
        {
            "down" => (verts[0], verts[1], verts[5], verts[4]),
            "up" => (verts[2], verts[3], verts[7], verts[6]),
            "north" => (verts[3], verts[2], verts[1], verts[0]),
            "south" => (verts[5], verts[6], verts[7], verts[4]),
            "west" => (verts[7], verts[3], verts[0], verts[4]),
            "east" => (verts[1], verts[2], verts[6], verts[5]),
            _ => (Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero)
        };

        private static Vector3 GetFaceBaseNormal(string faceName) => faceName switch
        {
            "down" => Vector3.down, "up" => Vector3.up, "north" => Vector3.back,
            "south" => Vector3.forward, "west" => Vector3.left, "east" => Vector3.right,
            _ => Vector3.zero
        };

        private static void AddFaceUVs(string faceName, Vector3 from, Vector3 to, JObject faceData, List<Vector2> uvs)
        {
            float u1, v1, u2, v2;
            if (faceData.TryGetValue("uv", out var uvTok) && uvTok is JArray a && a.Count == 4)
            {
                u1 = a[0].Value<float>() / 16f; v1 = 1f - a[1].Value<float>() / 16f;
                u2 = a[2].Value<float>() / 16f; v2 = 1f - a[3].Value<float>() / 16f;
            }
            else
            {
                (u1, v1, u2, v2) = faceName switch
                {
                    "up" => (from.x + 0.5f, 1f - (to.z + 0.5f), to.x + 0.5f, 1f - (from.z + 0.5f)),
                    "down" => (from.x + 0.5f, 1f - (from.z + 0.5f), to.x + 0.5f, 1f - (to.z + 0.5f)),
                    "north" => (1f - (to.x + 0.5f), 1f - (to.y + 0.5f), 1f - (from.x + 0.5f), 1f - (from.y + 0.5f)),
                    "south" => (from.x + 0.5f, 1f - (to.y + 0.5f), to.x + 0.5f, 1f - (from.y + 0.5f)),
                    "west" => (from.z + 0.5f, 1f - (to.y + 0.5f), to.z + 0.5f, 1f - (from.y + 0.5f)),
                    "east" => (1f - (to.z + 0.5f), 1f - (to.y + 0.5f), 1f - (from.z + 0.5f), 1f - (from.y + 0.5f)),
                    _ => (0, 0, 1, 1)
                };
            }

            int rot = faceData.TryGetValue("rotation", out var rTok) ? rTok.Value<int>() : 0;
            Span<Vector2> quad = stackalloc Vector2[] { new(u1, v2), new(u2, v2), new(u2, v1), new(u1, v1) };
            int steps = (rot / 90 % 4 + 4) % 4;
            uvs.Add(quad[(0 + steps) % 4]); uvs.Add(quad[(1 + steps) % 4]);
            uvs.Add(quad[(2 + steps) % 4]); uvs.Add(quad[(3 + steps) % 4]);
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
        }

        // HeadGenerator에서 오버라이드해서 사용
        protected virtual Texture2D CreateTexture(string path) => MinecraftFileManager.GetTextureFile(path);

        private static int PickIndexByWeights(JArray arr, Vector3Int seed)
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

        private static int Hash(Vector3Int v)
        {
            unchecked { return (17 * 31 + v.x) * 31 + v.y * 31 + v.z; }
        }

        private static bool IsMirrored(Transform t) => t.localToWorldMatrix.determinant < 0f;
        #endregion
    }
}
