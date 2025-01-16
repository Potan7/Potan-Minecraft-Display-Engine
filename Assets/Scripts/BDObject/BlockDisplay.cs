using Newtonsoft.Json.Linq;
using UnityEngine;

public class BlockDisplay : MonoBehaviour
{
    public MinecraftModelData modelData;
    public GameObject modelElementParent;

    public void LoadBlockModel(string name, string state)
    {
        //Debug.Log(name + ", " + state);

        // 블록 스테이트를 불러와서
        JObject blockState = MinecraftFileManager.GetJSONData("blockstates/" + name + ".json");

        // variants 형식일 경우
        if (blockState.ContainsKey("variants"))
        {
            JObject variants = blockState["variants"] as JObject;
            //Debug.Log("Variants : " + variants.ToString());
            // 블록 스테이트에 해당하는 모델을 불러옴
            if (variants.ContainsKey(state))
            {

                var modelInfo = variants[state];
                string modelLocation;

                JObject modelObject;

                if (modelInfo.Type == JTokenType.Array)
                {
                    modelObject = modelInfo[0] as JObject;
                    modelLocation = modelInfo[0]["model"].ToString();
                }
                else
                {
                    modelObject = modelInfo as JObject;
                    modelLocation = modelInfo["model"].ToString();
                }

                modelLocation = MinecraftFileManager.RemoveNamespace(modelLocation);

                int xRot = modelObject.TryGetValue("x", out JToken xToken) ? xToken.Value<int>() : 0;
                int yRot = modelObject.TryGetValue("y", out JToken yToken) ? yToken.Value<int>() : 0;
                bool uvlock = modelObject.TryGetValue("uvlock", out JToken uvlockToken) ? uvlockToken.Value<bool>() : false;

                // 불러온 모델을 바탕으로 생성하기
                //Debug.Log("model location : " + modelLocation);
                modelData = MinecraftFileManager.GetModelData("models/" + modelLocation + ".json").UnpackParent();

                SetModelByMinecraftModel(modelData);

                //Quaternion modelXRot = Quaternion.Euler(xRot, 0, 0);
                //Quaternion modelYRot = Quaternion.Euler(0, yRot, 0);

                modelElementParent.transform.Rotate(new Vector3(xRot, yRot, 0));

                // 회전 후 Pivot을 꼭짓점으로 이동
                AlignBlockDisplayToAABBCorner();

            }
            else
            {
                Debug.LogError("State not found: " + state);
            }
        }
    }

    void SetModelByMinecraftModel(MinecraftModelData model)
    {
        //Debug.Log(model.ToString());

        if (model.elements == null)
        {
            // 셜커상자, 침대 등등

        }

        modelElementParent = new GameObject("Model");
        modelElementParent.transform.SetParent(transform);
        modelElementParent.transform.localPosition = Vector3.zero;
        // Model -> Block들
        // Block Element 따라 생성 -> Model의 pivot을 좌측 하단으로 이동
        // defaultSize : 0.5


        // 회전 : xRot, yRot, rotaiton

        // 모델 데이터를 이용해서 블록을 생성
        foreach (var element in model.elements)
        {
            MeshRenderer cubeObject = Instantiate(Resources.Load<MeshRenderer>("minecraft/block"), modelElementParent.transform);
            //cubeObject.transform.localPosition = new Vector3(0.5f, 0.5f, 0.5f);

            // 큐브의 위치와 크기를 설정
            Vector3 from = new Vector3(
                element["from"][0].Value<float>(),
                element["from"][1].Value<float>(),
                element["from"][2].Value<float>());
            Vector3 to = new Vector3(
                element["to"][0].Value<float>(),
                element["to"][1].Value<float>(),
                element["to"][2].Value<float>());

            Vector3 size = (to - from) / 16.0f * cubeObject.transform.localScale.x;
            Vector3 center = from / 16.0f + size / 2.0f;

            //cubeObject.transform.localPosition = center;

            cubeObject.transform.localScale = size;
            cubeObject.transform.localPosition = center;

            // 큐브의 텍스쳐를 설정
            if (element.ContainsKey("faces"))
            {
                JObject faces = element["faces"] as JObject;
                foreach (var face in faces)
                {

                    // 각 텍스처 로드 및 설정
                    var faceTexture = face.Value["texture"];
                    int idx = MinecraftModelData.faceToTextureName[face.Key];
                    //cubeObject.materials[idx].shader = BDObjManager.BDObjShader;
                    cubeObject.materials[idx].mainTexture = CreateTexture(faceTexture.ToString(), model.textures);
                }
            }

            //큐브의 회전을 설정
            if (element.ContainsKey("rotation"))
            {
                JObject rotation = element["rotation"] as JObject;
                Vector3 origin = new Vector3(
                    rotation["origin"][0].Value<float>(),
                    rotation["origin"][1].Value<float>(),
                    rotation["origin"][2].Value<float>()
                    ) / 16.0f;
                Vector3 axis = rotation["axis"].ToString() switch
                {
                    "x" => Vector3.right,
                    "y" => Vector3.up,
                    "z" => Vector3.forward,
                    _ => Vector3.zero
                };

                float angle = rotation["angle"].Value<float>();

                // 큐브를 중심에서 회전
                cubeObject.transform.RotateAround(cubeObject.transform.position + origin, axis, angle);
            }
        }
    }

    void AlignBlockDisplayToAABBCorner()
    {
        // 1. AABB 계산
        Bounds aabb = new Bounds();
        bool isInitialized = false;

        foreach (Transform child in modelElementParent.transform)
        {
            MeshRenderer renderer = child.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                if (!isInitialized)
                {
                    aabb = renderer.bounds;
                    isInitialized = true;
                }
                else
                {
                    aabb.Encapsulate(renderer.bounds);
                }
            }
        }

        // 2. AABB의 좌하단 뒤쪽 꼭짓점 계산
        Vector3 corner = aabb.min;

        // 3. Model 이동 (BlockDisplay를 Pivot으로 만듦)
        Vector3 offset = transform.position - corner;
        modelElementParent.transform.position += offset;
    }

    Texture2D CreateTexture(string path, JObject textures)
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
