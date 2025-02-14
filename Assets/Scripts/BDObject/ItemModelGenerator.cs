using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public class ItemModelGenerator : MonoBehaviour
{
    public MeshRenderer meshRenderer;
    public MeshFilter meshFilter;
    Mesh mesh;

    public Texture2D layer0Textures;
    public Texture2D layer1Textures = null;

    #region Voxel Data
    // 정육면체의 8개 꼭짓점의 상대좌표 (원래는 -0.5 ~ 0.5)
    static readonly float3[] verticePositions =
    {
        new float3(-0.5f,  0.5f, -0.5f), new float3(-0.5f,  0.5f,  0.5f), // 0, 1
        new float3( 0.5f,  0.5f,  0.5f), new float3( 0.5f,  0.5f, -0.5f), // 2, 3
        new float3(-0.5f, -0.5f, -0.5f), new float3(-0.5f, -0.5f,  0.5f), // 4, 5
        new float3( 0.5f, -0.5f,  0.5f), new float3( 0.5f, -0.5f, -0.5f), // 6, 7
    };

    // 각 면이 가지는 정점의 인덱스 (verticePositions 배열 기준)
    static readonly int4[] faceVertices =
    {
        new int4(1, 2, 3, 0),   // up
        new int4(6, 5, 4, 7),   // down
        new int4(2, 1, 5, 6),   // front
        new int4(0, 3, 7, 4),   // back
        new int4(3, 2, 6, 7),   // right
        new int4(1, 0, 4, 5)    // left
    };

    // 삼각형 인덱스 (정사각형 면을 두 개의 삼각형으로 분할)
    static readonly int[] triangleVertices = { 0, 1, 2, 0, 2, 3 };

    // UV 좌표 (왼쪽 위, 오른쪽 위, 오른쪽 아래, 왼쪽 아래)
    static readonly int2[] dUV =
    {
        new int2(0, 1),
        new int2(1, 1),
        new int2(1, 0),
        new int2(0, 0)
    };
    #endregion

    List<float3> vertices = new List<float3>();
    List<int> triangles = new List<int>();
    List<float2> uvs = new List<float2>();
    List<Color> colors = new List<Color>();

    public void Init(Texture2D layer0, Texture2D layer1 = null)
    {
        mesh = new Mesh();
        meshFilter.sharedMesh = mesh;

        layer0Textures = layer0;
        layer1Textures = layer1;

        Generate();
    }

    Color GetPixel(int x, int y)
    {
        if (layer1Textures == null) return layer0Textures.GetPixel(x, y);

        Color color = layer1Textures.GetPixel(x, y);
        if (color.a == 0)
        {
            color = layer0Textures.GetPixel(x, y);
        }
        return color;
    }

    public void Generate()
    {
        int width = layer0Textures.width;
        int height = layer0Textures.height;

        // 텍스처의 각 픽셀을 순회하여 알파가 0이 아닌 픽셀에 대해 복셀 블록 생성
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color pixelColor = GetPixel(x, y);
                if (pixelColor.a == 0) continue;

                // 앞/뒤면은 무조건 추가
                AddFace(new int3(x, y, 0), 2, pixelColor);
                AddFace(new int3(x, y, 0), 3, pixelColor);

                // 주변에 픽셀이 없으면 해당 방향의 면을 추가
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

        // === 메시의 정점을 모두 중앙으로 오프셋 ===
        if (vertices.Count > 0)
        {
            // 최소, 최대 좌표 계산
            float3 min = new float3(float.MaxValue, float.MaxValue, float.MaxValue);
            float3 max = new float3(float.MinValue, float.MinValue, float.MinValue);
            foreach (var v in vertices)
            {
                min = math.min(min, v);
                max = math.max(max, v);
            }
            // 중앙값 계산
            float3 center = (min + max) / 2f;
            // 모든 정점을 중앙 기준으로 이동
            for (int i = 0; i < vertices.Count; i++)
            {
                vertices[i] -= center;
            }
        }
        // ====================================

        meshFilter.sharedMesh.Clear();
        meshFilter.sharedMesh.SetVertices(vertices.ConvertAll(v => (Vector3)v));
        meshFilter.sharedMesh.SetTriangles(triangles, 0);
        meshFilter.sharedMesh.SetUVs(0, uvs.ConvertAll(v => new Vector2(v.x, v.y)));
        meshFilter.sharedMesh.SetColors(colors);

        meshFilter.sharedMesh.RecalculateNormals();
        meshFilter.sharedMesh.RecalculateTangents();

        // 생성 후 리스트 초기화
        vertices.Clear();
        triangles.Clear();
        uvs.Clear();
        colors.Clear();
    }

    void AddFace(int3 p, int dir, Color color)
    {
        int vc = vertices.Count;

        for (int i = 0; i < 4; i++)
        {
            // 원래의 블록 크기 대신 voxelScale을 곱해 크기를 줄임.
            float3 dp = verticePositions[faceVertices[dir][i]];// * voxelScale;
            vertices.Add(p + dp);
            // 알파값을 1로 만들어 투명하지 않게 설정
            colors.Add(color);
        }

        for (int i = 0; i < 6; i++)
        {
            triangles.Add(vc + triangleVertices[i]);
        }

        for (int i = 0; i < 4; i++)
        {
            uvs.Add(new float2(dUV[i].x, dUV[i].y));
        }
    }
}
