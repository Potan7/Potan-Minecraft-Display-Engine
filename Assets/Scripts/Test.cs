using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

public class Test : MonoBehaviour
{

    MeshRenderer meshRenderer;
    MeshFilter meshFilter;
    Mesh mesh;

    public string textureName;
    public Texture2D texture;

    #region Voxel Data
    // 정육면체의 8개 꼭짓점의 상대좌표
    static readonly float3[] verticePositions =
    {
        new float3(-0.5f, 0.5f, -0.5f), new float3(-0.5f, 0.5f, 0.5f),      // 0, 1
        new float3(0.5f, 0.5f, 0.5f), new float3(0.5f, 0.5f, -0.5f),        // 2, 3
        new float3(-0.5f, -0.5f, -0.5f), new float3(-0.5f, -0.5f, 0.5f),    // 4, 5
        new float3(0.5f, -0.5f, 0.5f), new float3(0.5f, -0.5f, -0.5f),      // 6, 7
    };

    // 네모 하나가 그 방향에 따라 가지는 vertice들의, verticePositions에서의 인덱스
    static readonly int4[] faceVertices =
    {
        new int4(1, 2, 3, 0),   // up
        new int4(6, 5, 4, 7),   // down
        new int4(2, 1, 5, 6),   // front
        new int4(0, 3, 7, 4),   // back
        new int4(3, 2, 6, 7),   // right
        new int4(1, 0, 4, 5)    // left
    };

    // 삼
    static readonly int[] triangleVertices = { 0, 1, 2, 0, 2, 3 };

    static readonly int2[] dUV =
    {
        new int2(0, 1), // LT
        new int2(1, 1), // RT
        new int2(1, 0), // RB
        new int2(0, 0)  // LB
    };
    #endregion

    List<float3> vertices = new List<float3>();
    List<int> triangles = new List<int>();
    List<float2> uvs = new List<float2>();
    List<Color> colors = new List<Color>();

    void Start()
    {
        meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshFilter = gameObject.AddComponent<MeshFilter>();

        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        meshFilter.sharedMesh = mesh;

        meshRenderer.material = new Material(Shader.Find("Particles/Standard Unlit"));

        Invoke(nameof(Generate), 0.5f);
    }

    void Generate()
    {
        texture = DisplayObject.CreateTexture("item/" + textureName, null);

        int width = texture.width;
        int height = texture.height;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color pixelColor = texture.GetPixel(x, y);
                if (pixelColor.a == 0) continue;
                // 알파값이 0이 아닌 픽셀에 대해서만 생성
                AddFace(new int3(x, y, 0), 2, pixelColor);
                AddFace(new int3(x, y, 0), 3, pixelColor);

                if (x == 0 || texture.GetPixel(x - 1, y).a == 0)
                {
                    AddFace(new int3(x, y, 0), 5, pixelColor);
                }

                if (x == width - 1 || texture.GetPixel(x + 1, y).a == 0)
                {
                    AddFace(new int3(x, y, 0), 4, pixelColor);
                }

                if (y == 0 || texture.GetPixel(x, y - 1).a == 0)
                {
                    AddFace(new int3(x, y, 0), 1, pixelColor);
                }

                if (y == height - 1 || texture.GetPixel(x, y + 1).a == 0)
                {
                    AddFace(new int3(x, y, 0), 0, pixelColor);
                }
            }
        }

        meshFilter.sharedMesh.Clear();

        meshFilter.sharedMesh.SetVertices(vertices.ConvertAll(v => (Vector3)v));
        meshFilter.sharedMesh.SetTriangles(triangles, 0);
        meshFilter.sharedMesh.SetUVs(0, uvs.ConvertAll(v => new Vector2(v.x, v.y)));
        meshFilter.sharedMesh.SetColors(colors);

        meshFilter.sharedMesh.RecalculateNormals();
        meshFilter.sharedMesh.RecalculateNormals();
        meshFilter.sharedMesh.RecalculateTangents();

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
            float3 dp = verticePositions[faceVertices[dir][i]];
            vertices.Add(p + dp);
            color = new Color(color.r, color.g, color.b, 1);
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
