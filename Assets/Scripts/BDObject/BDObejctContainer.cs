using Newtonsoft.Json.Linq;
using UnityEngine;

public class BDObejctContainer : MonoBehaviour
{
    public BDObject BDObject;
    public MinecraftModelData modelData;
    public Matrix4x4 transformation;

    public BDObejctContainer Init(BDObject bdObject)
    {
        BDObject = bdObject;

        // 디스플레이라면
        if (bdObject.isBlockDisplay || bdObject.isItemDisplay)
        {
            //var block = Resources.Load<GameObject>("Prefab/Block");
            //var blockObj = Instantiate(block, transform);
            //blockObj.transform.localPosition = new Vector3(0.5f, 0.5f, 0.5f);

            // 이름과 상태 분리
            int typeStart = bdObject.name.IndexOf('[');
            if (typeStart == -1)
            {
                typeStart = bdObject.name.Length;
            }
            string name = bdObject.name.Substring(0, typeStart);
            string state = bdObject.name.Substring(typeStart);
            state = state.Replace("[", "").Replace("]", "");

            // 블록 디스플레이일 때
            if (bdObject.isBlockDisplay)
            {
                LoadBlockModel(name, state);
            }
            // 아이템 디스플레이일 때
            else
            {
                LoadItemModel(name, state);
            }
        }

        // 변환 행렬을 적용
        transformation = AffineTransformation.GetMatrix(BDObject.transforms);
        AffineTransformation.ApplyMatrixToTransform(transform, transformation);

        // 자식 오브젝트를 추가
        var BDObjectManager = GameManager.GetManager<BDObjectManager>();
        if (bdObject.children != null)
        {            
            foreach (var child in BDObject.children)
            {
                BDObjectManager.AddObject(transform, child);
            }
        }
        return this;
    }

    void LoadBlockModel(string name, string state)
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

                modelData.xRotation = xRot;
                modelData.yRotation = yRot;
                modelData.uvLock = uvlock;

                SetModelByMinecraftModel(modelData);
            }
            else
            {
                Debug.LogError("State not found: " + state);
            }
        }
    }

    void LoadItemModel(string name, string state)
    {
        
    }

    void SetModelByMinecraftModel(MinecraftModelData model)
    {
        Debug.Log(model.ToString());

        if (model.elements == null)
        {
            var defModel = MinecraftFileManager.GetModelData("models/block/cube.json");
            model.elements = defModel.elements;

        }

        // 모델 데이터를 이용해서 블록을 생성
        foreach (var element in model.elements)
        {
            GameObject cubeObject = Instantiate(Resources.Load<GameObject>("minecraft/block"), transform);
            var cube = cubeObject.GetComponentInChildren<MeshRenderer>();
            //cubeObject.transform.localPosition = new Vector3(0.5f, 0.5f, 0.5f);

            // 큐브의 위치와 크기를 설정
            Vector3 from = new Vector3(element["from"][0].Value<float>(), element["from"][1].Value<float>(), element["from"][2].Value<float>());
            Vector3 to = new Vector3(element["to"][0].Value<float>(), element["to"][1].Value<float>(), element["to"][2].Value<float>());

            Vector3 size = (to - from) / 16.0f;
            //Vector3 center = from + size / 2;

            //cubeObject.transform.localPosition = center;
            cubeObject.transform.localPosition = Vector3.zero;
            cubeObject.transform.localScale = size;

            // 큐브의 텍스쳐를 설정
            if (element.ContainsKey("faces"))
            {
                JObject faces = element["faces"] as JObject;
                foreach (var face in faces)
                {

                    // 각 텍스처 로드 및 설정
                    var faceTexture = face.Value["texture"];
                    int idx = MinecraftModelData.faceToTextureName[face.Key];
                    cube.materials[idx].mainTexture = CreateTexture(faceTexture.ToString(), model.textures);
                }
            }

            Quaternion xRot = Quaternion.Euler(model.xRotation, 0, 0);
            Quaternion yRot = Quaternion.Euler(0, model.yRotation, 0);

            cube.transform.localRotation = yRot * xRot;

            // 큐브의 회전을 설정
            //if (element.ContainsKey("rotation"))
            //{
            //    JObject rotation = element["rotation"] as JObject;
            //    Vector3 origin = new Vector3(rotation["origin"][0].Value<float>(), rotation["origin"][1].Value<float>(), rotation["origin"][2].Value<float>());
            //    Vector3 axis = new Vector3(rotation["axis"][0].Value<float>(), rotation["axis"][1].Value<float>(), rotation["axis"][2].Value<float>());
            //    float angle = rotation["angle"].Value<float>();
            //    //cubeObject.transform.RotateAround(center + origin, axis, angle);
            //    cube.transform.RotateAround(cube.transform.position + origin, axis, angle);
            //}
        }
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
