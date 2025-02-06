using Mono.Cecil.Cil;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class ItemDisplay : DisplayObject
{
    public override DisplayObject LoadDisplayModel(string name, string state)
    {
        // items 폴더의 정보를 가져와서
        modelName = name;
        JObject itemState = MinecraftFileManager.GetJSONData("items/" + name + ".json");

        if (!itemState.ContainsKey("model"))
        {
            Debug.LogError("Model not found: " + name);
            return this;
        }

        // 모델 정보를 가져옴
        JObject model = itemState.GetValue("model") as JObject;

        switch (model["type"].ToString())
        {
            case "minecraft:model":
                TypeModel(model);
                break;
            case "minecraft:condition":
                TypeCondition(model);
                break;
            case "minecraft:select":
                TypeSelect(model);
                break;
            case "minecraft:special":
                TypeSpecial(model);
                break;
            default:
                TypeOther(model);
                break;
        }
        return this;
    }

    void TypeModel(JObject itemState)
    {
        Debug.Log("model: " + itemState["model"]);
        string model = itemState["model"].ToString();
        if (model.StartsWith("minecraft:block/"))
        {
            var bd = gameObject.AddComponent<BlockDisplay>();
            bd.modelName = model;
            bd.SetModel(model);
        }
        else
        {
            SetModel(model);
        }
    }

    void TypeCondition(JObject itemState)
    {

    }

    void TypeSelect(JObject itemState)
    {

    }

    void TypeSpecial(JObject itemState)
    {

    }

    void TypeOther(JObject itemState)
    {
        // Range, Empty, bundle, composite
    }

    protected override void SetModelObject(MinecraftModelData modelData, GameObject modelElementParent)
    {
        //Debug.Log("SetModelObject: " + modelData.ToString());

        Texture2D texture = CreateTexture(modelData.textures["layer0"].ToString(), modelData.textures);



        Generate3DModel(texture, 0.1f);
    }

    void Generate3DModel(Texture2D spriteTexture, float thickness = 0.1f)
    {
        if (spriteTexture == null) return;

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        int width = spriteTexture.width;
        int height = spriteTexture.height;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color pixelColor = spriteTexture.GetPixel(x, y);

                if (pixelColor.a > 0.1f) // 투명도가 낮은 픽셀만 3D 변환
                {
                    int vertexIndex = vertices.Count;

                    // 앞면 (XY 평면)
                    vertices.Add(new Vector3(x, y, 0)); // 좌상
                    vertices.Add(new Vector3(x + 1, y, 0)); // 우상
                    vertices.Add(new Vector3(x + 1, y - 1, 0)); // 우하
                    vertices.Add(new Vector3(x, y - 1, 0)); // 좌하

                    triangles.AddRange(new int[] { vertexIndex, vertexIndex + 1, vertexIndex + 2, vertexIndex, vertexIndex + 2, vertexIndex + 3 });

                    uvs.Add(new Vector2((float)x / width, (float)y / height));
                    uvs.Add(new Vector2((float)(x + 1) / width, (float)y / height));
                    uvs.Add(new Vector2((float)(x + 1) / width, (float)(y - 1) / height));
                    uvs.Add(new Vector2((float)x / width, (float)(y - 1) / height));

                    // 뒷면 (Z축 방향으로 두께 추가)
                    int backIndex = vertices.Count;
                    vertices.Add(new Vector3(x, y, -thickness)); // 좌상
                    vertices.Add(new Vector3(x + 1, y, -thickness)); // 우상
                    vertices.Add(new Vector3(x + 1, y - 1, -thickness)); // 우하
                    vertices.Add(new Vector3(x, y - 1, -thickness)); // 좌하

                    triangles.AddRange(new int[] { backIndex, backIndex + 2, backIndex + 1, backIndex, backIndex + 3, backIndex + 2 });

                    uvs.AddRange(uvs.GetRange(vertexIndex, 4)); // 동일한 UV 맵 적용

                    // 옆면 생성 (두께 적용, 색상은 주변 픽셀 색 기반)
                    CreateSideFaces(vertices, triangles, x, y, thickness, pixelColor);
                }
            }
        }

        Mesh mesh = new Mesh
        {
            vertices = vertices.ToArray(),
            triangles = triangles.ToArray(),
            uv = uvs.ToArray()
        };
        mesh.RecalculateNormals();

        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshRenderer>().material = new Material(Shader.Find("Standard"))
        {
            mainTexture = spriteTexture
        };
    }

    void CreateSideFaces(List<Vector3> vertices, List<int> triangles, int x, int y, float thickness, Color pixelColor)
    {
        // 왼쪽
        vertices.Add(new Vector3(x, y, 0));
        vertices.Add(new Vector3(x, y, -thickness));
        vertices.Add(new Vector3(x, y - 1, -thickness));
        vertices.Add(new Vector3(x, y - 1, 0));

        triangles.AddRange(new int[] { vertices.Count - 4, vertices.Count - 3, vertices.Count - 2, vertices.Count - 4, vertices.Count - 2, vertices.Count - 1 });

        // 오른쪽
        vertices.Add(new Vector3(x + 1, y, 0));
        vertices.Add(new Vector3(x + 1, y, -thickness));
        vertices.Add(new Vector3(x + 1, y - 1, -thickness));
        vertices.Add(new Vector3(x + 1, y - 1, 0));

        triangles.AddRange(new int[] { vertices.Count - 4, vertices.Count - 2, vertices.Count - 3, vertices.Count - 4, vertices.Count - 1, vertices.Count - 2 });

        // 위쪽
        vertices.Add(new Vector3(x, y, 0));
        vertices.Add(new Vector3(x, y, -thickness));
        vertices.Add(new Vector3(x + 1, y, -thickness));
        vertices.Add(new Vector3(x + 1, y, 0));

        triangles.AddRange(new int[] { vertices.Count - 4, vertices.Count - 3, vertices.Count - 2, vertices.Count - 4, vertices.Count - 2, vertices.Count - 1 });

        // 아래쪽
        vertices.Add(new Vector3(x, y - 1, 0));
        vertices.Add(new Vector3(x, y - 1, -thickness));
        vertices.Add(new Vector3(x + 1, y - 1, -thickness));
        vertices.Add(new Vector3(x + 1, y - 1, 0));

        triangles.AddRange(new int[] { vertices.Count - 4, vertices.Count - 2, vertices.Count - 3, vertices.Count - 4, vertices.Count - 1, vertices.Count - 2 });
    }
}
