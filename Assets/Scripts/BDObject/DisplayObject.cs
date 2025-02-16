using Newtonsoft.Json.Linq;
using UnityEngine;

public abstract class DisplayObject : MonoBehaviour
{
    public MinecraftModelData modelData;
    public string modelName;

    public Bounds AABBBound { get; private set; }

    public abstract void LoadDisplayModel(string name, string state);

    protected void SetAABBBounds()
    {
        // 1. AABB 계산
        AABBBound = new Bounds();

        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
        int count = renderers.Length;
        for (int i = 0; i < count; i++)
        {
            if (i == 0)
            {
                AABBBound = renderers[i].localBounds;
            }
            else
            {
                AABBBound.Encapsulate(renderers[i].localBounds);
            }
        }
    }

    public static string GetTexturePath(string path, JObject textures)
    {
        if (path[0] == '#')
        {
            return GetTexturePath(textures[path.Substring(1)].ToString(), textures);
        }
        return "textures/" + MinecraftFileManager.RemoveNamespace(path) + ".png";
    }

    // 텍스쳐 생성
    //public static Texture2D CreateTexture(string path)
    //{
    //    Texture2D texture = MinecraftFileManager.GetTextureFile(path);
    //    if (texture == null)
    //    {
    //        CustomLog.LogError("Texture not found: " + path);
    //        return null;
    //    }
    //    return texture;
    //}

    public static Texture2D MergeTextures(Texture2D baseTexture, Texture2D overlayTexture)
    {
        int width = Mathf.Max(baseTexture.width, overlayTexture.width);
        int height = Mathf.Max(baseTexture.height, overlayTexture.height);

        RenderTexture rt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
        RenderTexture.active = rt;

        Material blendMaterial = new Material(Shader.Find("Hidden/BlendShader"));
        Graphics.Blit(baseTexture, rt, blendMaterial);
        Graphics.Blit(overlayTexture, rt, blendMaterial, 1);

        Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, false);
        result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        result.Apply();

        RenderTexture.active = null;
        rt.Release();

        return result;
    }
}
