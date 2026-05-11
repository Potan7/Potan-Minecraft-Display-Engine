using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace BDObjectSystem.Display
{
    public class ItemModelGenerator : MonoBehaviour
    {
        public MeshRenderer meshRenderer;
        public MeshFilter meshFilter;
        private Mesh _mesh;

        public Texture2D layer0Textures;
        public Texture2D layer1Textures;

        // 미리 합성된 최종 픽셀
        private Color32[] _pixels;
        private int _width;
        private int _height;

        #region Voxel Data
        private static readonly Vector3[] verticePositions =
        {
            new(-0.5f,  0.5f, -0.5f), new(-0.5f,  0.5f,  0.5f), // 0, 1
            new( 0.5f,  0.5f,  0.5f), new( 0.5f,  0.5f, -0.5f), // 2, 3
            new(-0.5f, -0.5f, -0.5f), new(-0.5f, -0.5f,  0.5f), // 4, 5
            new( 0.5f, -0.5f,  0.5f), new( 0.5f, -0.5f, -0.5f)  // 6, 7
        };

        private static readonly int4[] faceVertices =
        {
            new(1, 2, 3, 0),   // up
            new(6, 5, 4, 7),   // down
            new(2, 1, 5, 6),   // front
            new(0, 3, 7, 4),   // back
            new(3, 2, 6, 7),   // right
            new(1, 0, 4, 5)    // left
        };

        private static readonly int[] triangleVertices = { 0, 1, 2, 0, 2, 3 };

        private static readonly Vector2Int[] dUV =
        {
            new(0, 1),
            new(1, 1),
            new(1, 0),
            new(0, 0)
        };
        #endregion

        private readonly List<Vector3> _vertices = new();
        private readonly List<int> _triangles = new();
        private readonly List<Vector2> _uvs = new();
        private readonly List<Color32> _colors = new();

        private static Dictionary<string, Mesh> _meshCache = new();
        private string meshName;

        public void Init(string name, Texture2D layer0, Texture2D layer1 = null)
        {
            layer0Textures = layer0;
            layer1Textures = layer1;

            meshName = name;

            if (_meshCache.TryGetValue(meshName, out var cachedMesh))
            {
                // 캐시된 메쉬가 있으면 재사용
                meshFilter.sharedMesh = cachedMesh;
                return;
            }

            // 메쉬는 한 번만 생성
            if (_mesh == null)
            {
                _mesh = new Mesh
                {
                    name = "ItemModelMesh"
                };
                meshFilter.sharedMesh = _mesh;
            }

            // 텍스처 정보 캐싱
            _width = layer0Textures.width;
            _height = layer0Textures.height;

            // 두 레이어를 미리 합성해서 하나의 픽셀 배열로 만든다.
            var basePixels = layer0Textures.GetPixels32();
            Color32[] layer1Pixels = null;
            var hasLayer1 = layer1Textures != null;

            if (hasLayer1)
                layer1Pixels = layer1Textures.GetPixels32();

            _pixels = new Color32[basePixels.Length];

            for (var i = 0; i < basePixels.Length; i++)
            {
                if (hasLayer1)
                {
                    var c1 = layer1Pixels[i];
                    _pixels[i] = c1.a != 0 ? c1 : basePixels[i];
                }
                else
                {
                    _pixels[i] = basePixels[i];
                }
            }

            Generate();
        }

        /// <summary>
        /// 캐시된 _pixels에서 색상을 가져온다.
        /// index 계산만 하기 때문에 GetPixel보다 훨씬 빠름.
        /// </summary>
        private Color32 GetPixelFast(int x, int y)
        {
            return _pixels[y * _width + x];
        }

        private void Generate()
        {
            _vertices.Clear();
            _triangles.Clear();
            _uvs.Clear();
            _colors.Clear();

            // 대략적인 용량 미리 예약 (GC 줄이기)
            var estimatedFacesPerPixel = 4; // 앞/뒤 + 일부 옆면
            var estimatedVertices = _width * _height * estimatedFacesPerPixel * 4;
            var estimatedTriangles = _width * _height * estimatedFacesPerPixel * 6;

            if (_vertices.Capacity < estimatedVertices) _vertices.Capacity = estimatedVertices;
            if (_triangles.Capacity < estimatedTriangles) _triangles.Capacity = estimatedTriangles;
            if (_uvs.Capacity < estimatedVertices) _uvs.Capacity = estimatedVertices;
            if (_colors.Capacity < estimatedVertices) _colors.Capacity = estimatedVertices;

            // 텍스처를 스캔하면서 면 생성
            for (var y = 0; y < _height; y++)
            {
                var rowOffset = y * _width;
                for (var x = 0; x < _width; x++)
                {
                    var idx = rowOffset + x;
                    var pixelColor = _pixels[idx];
                    if (pixelColor.a == 0) continue;

                    var p = new Vector3(x, y, 0);

                    // 앞/뒤 면
                    AddFace(p, 2, pixelColor); // front
                    AddFace(p, 3, pixelColor); // back

                    // 좌
                    if (x == 0 || _pixels[idx - 1].a == 0)
                        AddFace(p, 5, pixelColor);

                    // 우
                    if (x == _width - 1 || _pixels[idx + 1].a == 0)
                        AddFace(p, 4, pixelColor);

                    // 아래
                    if (y == 0 || _pixels[idx - _width].a == 0)
                        AddFace(p, 1, pixelColor);

                    // 위
                    if (y == _height - 1 || _pixels[idx + _width].a == 0)
                        AddFace(p, 0, pixelColor);
                }
            }

            // 메쉬를 중앙 정렬
            if (_vertices.Count > 0)
            {
                var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

                foreach (var v in _vertices)
                {
                    min = Vector3.Min(min, v);
                    max = Vector3.Max(max, v);
                }

                var center = (min + max) / 2f;
                for (var i = 0; i < _vertices.Count; i++)
                {
                    _vertices[i] -= center;
                }
            }

            _mesh.Clear();
            _mesh.SetVertices(_vertices);
            _mesh.SetTriangles(_triangles, 0);
            _mesh.SetUVs(0, _uvs);
            _mesh.SetColors(_colors);

            _mesh.RecalculateNormals();

            // 메쉬를 캐시에 저장
            _meshCache[meshName] = _mesh;
            // 탠전트가 꼭 필요 없으면 아래는 빼도 된다. (조금 더 빨라짐)
            // _mesh.RecalculateTangents();
        }

        private void AddFace(Vector3 p, int dir, Color32 color)
        {
            var vc = _vertices.Count;

            for (var i = 0; i < 4; i++)
            {
                var dp = verticePositions[faceVertices[dir][i]];
                _vertices.Add(p + dp);
                _colors.Add(color);
            }

            for (var i = 0; i < 6; i++)
            {
                _triangles.Add(vc + triangleVertices[i]);
            }

            for (var i = 0; i < 4; i++)
            {
                _uvs.Add(new Vector2(dUV[i].x, dUV[i].y));
            }
        }
    }
}
