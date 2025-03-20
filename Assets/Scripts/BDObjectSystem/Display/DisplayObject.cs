using Minecraft;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace BDObjectSystem.Display
{
    public abstract class DisplayObject : MonoBehaviour
    {
        public string modelName;

        /*public static Texture2D MergeTextures(Texture2D baseTexture, Texture2D overlayTexture)
        {
            var width = Mathf.Max(baseTexture.width, overlayTexture.width);
            var height = Mathf.Max(baseTexture.height, overlayTexture.height);

            var rt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
            RenderTexture.active = rt;

            var blendMaterial = new Material(Shader.Find("Hidden/BlendShader"));
            Graphics.Blit(baseTexture, rt, blendMaterial);
            Graphics.Blit(overlayTexture, rt, blendMaterial, 1);

            var result = new Texture2D(width, height, TextureFormat.RGBA32, false);
            result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            result.Apply();

            RenderTexture.active = null;
            rt.Release();

            return result;
        }*/

        public static string GetTexturePath(string path, JObject textures)
        {
            while (true)
            {
                if (path[0] == '#')
                {
                    path = textures[path[1..]].ToString();
                    continue;
                }

                return MinecraftFileManager.RemoveNamespace(path) + ".png";
            }
        }
    }

    public abstract class ModelDisPlayObject : DisplayObject
    {
        public MinecraftModelData ModelData;

        public Bounds AABBBound { get; private set; }

        public abstract void LoadDisplayModel(string mName, string state);

        protected void SetAABBBounds()
        {
            // 1. AABB ���
            AABBBound = new Bounds();

            var renderers = GetComponentsInChildren<MeshRenderer>();
            var count = renderers.Length;
            for (var i = 0; i < count; i++)
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



        // �ؽ��� ����
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
    }
}