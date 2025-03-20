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

        #region Voxel Data
        // ������ü�� 8�� �������� �����ǥ (������ -0.5 ~ 0.5)
        private static readonly float3[] verticePositions =
        {
            new(-0.5f,  0.5f, -0.5f), new(-0.5f,  0.5f,  0.5f), // 0, 1
            new( 0.5f,  0.5f,  0.5f), new( 0.5f,  0.5f, -0.5f), // 2, 3
            new(-0.5f, -0.5f, -0.5f), new(-0.5f, -0.5f,  0.5f), // 4, 5
            new( 0.5f, -0.5f,  0.5f), new( 0.5f, -0.5f, -0.5f) // 6, 7
        };

        // �� ���� ������ ������ �ε��� (verticePositions �迭 ����)
        private static readonly int4[] faceVertices =
        {
            new(1, 2, 3, 0),   // up
            new(6, 5, 4, 7),   // down
            new(2, 1, 5, 6),   // front
            new(0, 3, 7, 4),   // back
            new(3, 2, 6, 7),   // right
            new(1, 0, 4, 5)    // left
        };

        // �ﰢ�� �ε��� (���簢�� ���� �� ���� �ﰢ������ ����)
        private static readonly int[] triangleVertices = { 0, 1, 2, 0, 2, 3 };

        // UV ��ǥ (���� ��, ������ ��, ������ �Ʒ�, ���� �Ʒ�)
        private static readonly int2[] dUV =
        {
            new(0, 1),
            new(1, 1),
            new(1, 0),
            new(0, 0)
        };
        #endregion

        private List<float3> _vertices = new();
        private List<int> _triangles = new();
        private List<float2> _uvs = new();
        private List<Color> _colors = new();

        public void Init(Texture2D layer0, Texture2D layer1 = null)
        {
            _mesh = new Mesh();
            meshFilter.sharedMesh = _mesh;

            layer0Textures = layer0;
            layer1Textures = layer1;

            Generate();
        }

        private Color GetPixel(int x, int y)
        {
            if (!layer1Textures) return layer0Textures.GetPixel(x, y);

            var color = layer1Textures.GetPixel(x, y);
            if (color.a == 0)
            {
                color = layer0Textures.GetPixel(x, y);
            }
            return color;
        }

        private void Generate()
        {
            var width = layer0Textures.width;
            var height = layer0Textures.height;

            // �ؽ�ó�� �� �ȼ��� ��ȸ�Ͽ� ���İ� 0�� �ƴ� �ȼ��� ���� ���� ���� ����
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var pixelColor = GetPixel(x, y);
                    if (pixelColor.a == 0) continue;

                    // ��/�ڸ��� ������ �߰�
                    AddFace(new int3(x, y, 0), 2, pixelColor);
                    AddFace(new int3(x, y, 0), 3, pixelColor);

                    // �ֺ��� �ȼ��� ������ �ش� ������ ���� �߰�
                    if (x == 0 || GetPixel(x - 1, y).a == 0)
                        AddFace(new int3(x, y, 0), 5, pixelColor);

                    if (x == width - 1 || GetPixel(x + 1, y).a == 0)
                        AddFace(new int3(x, y, 0), 4, pixelColor);

                    if (y == 0 || GetPixel(x, y - 1).a == 0)
                        AddFace(new int3(x, y, 0), 1, pixelColor);

                    if (y == height - 1 || GetPixel(x, y + 1).a == 0)
                        AddFace(new int3(x, y, 0), 0, pixelColor);
                }
            }

            // === �޽��� ������ ��� �߾����� ������ ===
            if (_vertices.Count > 0)
            {
                // �ּ�, �ִ� ��ǥ ���
                var min = new float3(float.MaxValue, float.MaxValue, float.MaxValue);
                var max = new float3(float.MinValue, float.MinValue, float.MinValue);
                foreach (var v in _vertices)
                {
                    min = math.min(min, v);
                    max = math.max(max, v);
                }
                // �߾Ӱ� ���
                var center = (min + max) / 2f;
                // ��� ������ �߾� �������� �̵�
                for (var i = 0; i < _vertices.Count; i++)
                {
                    _vertices[i] -= center;
                }
            }
            // ====================================

            meshFilter.sharedMesh.Clear();
            meshFilter.sharedMesh.SetVertices(_vertices.ConvertAll(v => (Vector3)v));
            meshFilter.sharedMesh.SetTriangles(_triangles, 0);
            meshFilter.sharedMesh.SetUVs(0, _uvs.ConvertAll(v => new Vector2(v.x, v.y)));
            meshFilter.sharedMesh.SetColors(_colors);

            meshFilter.sharedMesh.RecalculateNormals();
            meshFilter.sharedMesh.RecalculateTangents();

            // ���� �� ����Ʈ �ʱ�ȭ
            _vertices.Clear();
            _triangles.Clear();
            _uvs.Clear();
            _colors.Clear();
        }

        private void AddFace(int3 p, int dir, Color color)
        {
            var vc = _vertices.Count;

            for (var i = 0; i < 4; i++)
            {
                // ������ ���� ũ�� ��� voxelScale�� ���� ũ�⸦ ����.
                var dp = verticePositions[faceVertices[dir][i]];// * voxelScale;
                _vertices.Add(p + dp);
                // ���İ��� 1�� ����� �������� �ʰ� ����
                _colors.Add(color);
            }

            for (var i = 0; i < 6; i++)
            {
                _triangles.Add(vc + triangleVertices[i]);
            }

            for (var i = 0; i < 4; i++)
            {
                _uvs.Add(new float2(dUV[i].x, dUV[i].y));
            }
        }
    }
}
