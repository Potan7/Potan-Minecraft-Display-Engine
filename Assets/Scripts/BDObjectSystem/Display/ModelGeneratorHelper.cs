using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace BDObjectSystem.Display
{
    /// <summary>
    /// BlockModelGenerator의 유틸리티 함수들을 모아놓은 헬퍼 클래스
    /// </summary>
    public static class ModelGeneratorHelper
    {
        #region BlockState Parsing
        
        /// <summary>
        /// BlockState JSON에서 적용할 모델 스펙들을 수집합니다.
        /// </summary>
        internal static List<BlockModelGenerator.ApplySpec> CollectApplies(JObject blockState, string state, Vector3Int seed)
        {
            var list = new List<BlockModelGenerator.ApplySpec>(8);
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

        private static void NormalizeApply(JToken tok, Vector3Int seed, List<BlockModelGenerator.ApplySpec> dst)
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

        private static BlockModelGenerator.ApplySpec ToApplySpec(JObject obj)
        {
            return new BlockModelGenerator.ApplySpec
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

        #endregion

        #region Element Processing

        /// <summary>
        /// Element의 회전 정보를 파싱합니다.
        /// </summary>
        public static (Quaternion rot, Vector3 origin, float rescale, char axis) ParseElementRotation(JObject element)
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

        /// <summary>
        /// 큐브의 정점들을 회전 및 스케일 변환합니다.
        /// </summary>
        public static void CalculateTransformedVertices(
            Span<Vector3> cubeVerts, 
            Vector3 from, 
            Vector3 to, 
            Vector3 origin, 
            Quaternion elementRotation, 
            Quaternion modelRotation, 
            float rescaleFactor, 
            char axisChar)
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

        /// <summary>
        /// 면 이름에 따라 큐브의 정점을 반환합니다.
        /// </summary>
        public static (Vector3, Vector3, Vector3, Vector3) GetFaceVertices(string faceName, Span<Vector3> verts) => faceName switch
        {
            // 모든 면을 반시계 방향으로 통일 (normal 방향에서 봤을 때)
            "down" => (verts[0], verts[1], verts[5], verts[4]),  // -Y 면
            "up" => (verts[3], verts[2], verts[6], verts[7]),    // +Y 면
            "north" => (verts[0], verts[1], verts[2], verts[3]), // -Z 면
            "south" => (verts[5], verts[4], verts[7], verts[6]), // +Z 면
            "west" => (verts[0], verts[4], verts[7], verts[3]),  // -X 면
            "east" => (verts[1], verts[2], verts[6], verts[5]),  // +X 면
            _ => (Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero)
        };

        /// <summary>
        /// 면 이름에 따라 기본 노말 벡터를 반환합니다.
        /// </summary>
        public static Vector3 GetFaceBaseNormal(string faceName) => faceName switch
        {
            "down" => Vector3.down,
            "up" => Vector3.up,
            "north" => Vector3.back,
            "south" => Vector3.forward,
            "west" => Vector3.left,
            "east" => Vector3.right,
            _ => Vector3.zero
        };

        #endregion

        #region UV Processing

        /// <summary>
        /// 면의 UV 좌표를 추가합니다.
        /// </summary>
        public static void AddFaceUVs(string faceName, Vector3 from, Vector3 to, JObject faceData, List<Vector2> uvs)
        {
            float u1, v1, u2, v2;
            if (faceData.TryGetValue("uv", out var uvTok) && uvTok is JArray a && a.Count == 4)
            {
                // UV가 명시되어 있으면 사용 (픽셀 단위 -> 정규화)
                var (divisorU, divisorV) = DetermineTextureDivisor(a);

                u1 = a[0].Value<float>() / divisorU; 
                v1 = 1f - a[3].Value<float>() / divisorV;  // V축 반전 (bottom)
                u2 = a[2].Value<float>() / divisorU; 
                v2 = 1f - a[1].Value<float>() / divisorV;  // V축 반전 (top)
            }
            else
            {
                // UV가 없으면 자동 계산 (블록 기준)
                (u1, v1, u2, v2) = CalculateAutoUV(faceName, from, to);
            }

            int rot = faceData.TryGetValue("rotation", out var rTok) ? rTok.Value<int>() : 0;

            // UV 좌표를 반시계 방향으로 배치 (좌하, 우하, 우상, 좌상)
            var uv0 = new Vector2(u1, v1); // 좌하
            var uv1 = new Vector2(u2, v1); // 우하
            var uv2 = new Vector2(u2, v2); // 우상
            var uv3 = new Vector2(u1, v2); // 좌상

            ReadOnlySpan<Vector2> quad = stackalloc Vector2[] { uv0, uv1, uv2, uv3 };
            
            // 회전은 시계 방향으로 적용 (마인크래프트 기준)
            int steps = (rot / 90) % 4;

            // north 면만 UV를 반시계 방향으로 90도 회전 (시계 방향 270도와 동일)
            if (faceName == "east")
            {
                steps = (steps + 3) % 4;  // 270도 추가 회전 = 반시계 90도
            }

            // GetFaceVertices의 정점 순서에 맞게 UV를 추가
            uvs.Add(quad[(4 - steps) % 4]); 
            uvs.Add(quad[(5 - steps) % 4]);
            uvs.Add(quad[(6 - steps) % 4]); 
            uvs.Add(quad[(7 - steps) % 4]);
        }

        /// <summary>
        /// UV 값의 크기에 따라 텍스처 크기를 판단합니다.
        /// 반환값: (U축 divisor, V축 divisor)
        /// </summary>
        private static (float divisorU, float divisorV) DetermineTextureDivisor(JArray uvArray)
        {
            float maxU = Mathf.Max(uvArray[0].Value<float>(), uvArray[2].Value<float>());
            float maxV = Mathf.Max(uvArray[1].Value<float>(), uvArray[3].Value<float>());

            // U축: 16을 넘으면 64, 아니면 16
            float divisorU = maxU > 16f ? 64f : 16f;
            
            // V축: 16을 넘으면 U축과 동일하게 처리 (64x64, 64x32 모두 64 사용)
            float divisorV = maxV > 16f ? 64f : 16f;

            return (divisorU, divisorV);
        }

        /// <summary>
        /// UV가 명시되지 않은 경우 자동으로 계산합니다.
        /// </summary>
        private static (float u1, float v1, float u2, float v2) CalculateAutoUV(string faceName, Vector3 from, Vector3 to)
        {
            // from, to는 이미 -0.5~0.5 범위로 정규화된 값
            // 0~1 범위로 변환: +0.5
            return faceName switch
            {
                // Y축 면들 (up, down) - Z축 기준
                "up"    => (from.x + 0.5f, from.z + 0.5f, to.x + 0.5f, to.z + 0.5f),
                "down"  => (from.x + 0.5f, from.z + 0.5f, to.x + 0.5f, to.z + 0.5f),
                
                // Z축 면들 (north, south) - X, Y 기준
                "north" => (from.x + 0.5f, from.y + 0.5f, to.x + 0.5f, to.y + 0.5f),
                "south" => (from.x + 0.5f, from.y + 0.5f, to.x + 0.5f, to.y + 0.5f),
                
                // X축 면들 (west, east) - Z, Y 기준
                "west"  => (from.z + 0.5f, from.y + 0.5f, to.z + 0.5f, to.y + 0.5f),
                "east"  => (from.z + 0.5f, from.y + 0.5f, to.z + 0.5f, to.y + 0.5f),
                
                _ => (0, 0, 1, 1)
            };
        }

        #endregion

        #region Model Analysis

        /// <summary>
        /// 모델이 단순 큐브(cube_all 기반)인지 확인합니다.
        /// </summary>
        public static bool IsSimpleCubeModel(Minecraft.MinecraftModelData data)
        {
            if (data == null)
                return false;

            // parent가 cube, cube_all 등인 경우
            if (!string.IsNullOrEmpty(data.Parent))
            {
                var parentName = Minecraft.MinecraftFileManager.RemoveNamespace(data.Parent);
                if (parentName is "block/cube" or "block/cube_all" or "block/cube_column")
                    return true;
            }

            // elements가 정확히 1개이고 16x16x16 큐브인 경우
            if (data.Elements == null || data.Elements.Count != 1)
                return false;

            var element = data.Elements[0];
            if (!element.TryGetValue("from", out var fromTok) || fromTok is not JArray fromArr ||
                !element.TryGetValue("to", out var toTok) || toTok is not JArray toArr)
                return false;

            var from = new Vector3(fromArr[0].Value<float>(), fromArr[1].Value<float>(), fromArr[2].Value<float>());
            var to = new Vector3(toArr[0].Value<float>(), toArr[1].Value<float>(), toArr[2].Value<float>());

            // 16x16x16 큐브인지 확인
            return from == Vector3.zero && to == new Vector3(16, 16, 16);
        }

        #endregion

        #region Utility

        private static int Hash(Vector3Int v)
        {
            unchecked { return (17 * 31 + v.x) * 31 + v.y * 31 + v.z; }
        }

        public static bool IsMirrored(Transform t) => t.localToWorldMatrix.determinant < 0f;

        #endregion
    }
}