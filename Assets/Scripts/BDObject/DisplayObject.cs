using Newtonsoft.Json.Linq;
using UnityEngine;

public abstract class DisplayObject : MonoBehaviour
{
    public MinecraftModelData modelData;
    public string modelName;
    public Bounds AABBBound { get; private set; }

    public abstract DisplayObject LoadDisplayModel(string name, string state);

    public GameObject SetModel(string modelLocation)
    {
        // 불러온 모델을 바탕으로 생성하기
        modelLocation = MinecraftFileManager.RemoveNamespace(modelLocation);

        //Debug.Log("model location : " + modelLocation);
        modelData = MinecraftFileManager.GetModelData("models/" + modelLocation + ".json").UnpackParent();

        GameObject modelElementParent = new GameObject("Model");
        modelElementParent.transform.SetParent(transform);
        modelElementParent.transform.localPosition = Vector3.zero;

        SetModelObject(modelData, modelElementParent);
        return modelElementParent;
    }

    protected abstract void SetModelObject(MinecraftModelData modelData, GameObject modelElementParent);

    protected void SetAABBBounds()
    {
        // 1. AABB 계산
        AABBBound = new Bounds();
        
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
        int count = renderers.Length;
        for (int i = 0; i < count; i++)
        {
            MeshRenderer renderer = renderers[i];
            if (i == 0)
            {
                AABBBound = renderer.bounds;
            }
            else
            {
                AABBBound.Encapsulate(renderer.bounds);
            }
        }
    }

    // 텍스쳐 생성
    public static Texture2D CreateTexture(string path, JObject textures)
    {
        if (path[0] == '#')
        {
            return CreateTexture(textures[path.Substring(1)].ToString(), textures);
        }

        string texturePath = "textures/" + MinecraftFileManager.RemoveNamespace(path) + ".png";
        //Debug.Log(texturePath);
        Texture2D texture = MinecraftFileManager.GetTextureFile(texturePath);
        if (texture == null)
        {
            Debug.LogError("Texture not found: " + texturePath);
            return null;
        }
        return texture;
    }
}
