using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class BlockDisplay : MonoBehaviour
{
    public MinecraftModelData modelData;
    string blockName;
    Bounds AABBBound;

    // blockstate 읽기 
    public Bounds LoadBlockModel(string name, string state)
    {
        //Debug.Log(name + ", " + state);

        // 블록 스테이트를 불러와서
        blockName = name;
        JObject blockState = MinecraftFileManager.GetJSONData("blockstates/" + name + ".json");
        //Debug.Log("BlockState : " + blockState.ToString());
        //Debug.Log("State : " + state);

        // variants 형식일 경우
        if (blockState.ContainsKey("variants"))
        {
            JObject variants = blockState["variants"] as JObject;
            //Debug.Log("Variants : " + variants.ToString());
            // 블록 스테이트에 해당하는 모델을 불러옴
            if (variants.ContainsKey(state))
            {
                SetModel(variants[state]);
            }
            else
            {
                Debug.LogError("State not found: " + state);
            }
        }
        else if (blockState.ContainsKey("multipart"))
        {
            // multipart 형식일 경우
            var multipart = blockState["multipart"] as JArray;
            //Debug.Log("Multipart : " + multipart.ToString());

            for (int i = 0; i < multipart.Count; i++)
            {
                Debug.Log("Part : " + multipart[i].ToString());
                JObject partObject = multipart[i] as JObject;

                bool check = true;

                if (partObject.ContainsKey("when"))
                {
                    check = CheckState(partObject["when"] as JObject, state);
                }

                if (check)
                    SetModel(partObject["apply"]);
            }

        }
        else
        {
            Debug.LogError("Unknown blockstate format");
        }

        SetAABBBounds();
        return AABBBound;
    }

    private bool CheckState(JObject when, string state)
    {
        if (when.ContainsKey("OR"))
        {
            var OR = when["OR"] as JArray;
            for (int i = 0; i < OR.Count; i++)
            {
                if (CheckStateName(OR[i] as JObject, state))
                {
                    return true;
                }
            }
            return false;
        }
        else if (when.ContainsKey("AND"))
        {
            var AND = when["AND"] as JArray;
            for (int i = 0; i < AND.Count; i++)
            {
                if (CheckStateName(AND[i] as JObject, state) == false)
                {
                    return false;
                }
            }
            return true;
        }
        else
        {
            return CheckStateName(when, state);
        }
    }

    private bool CheckStateName(JObject checks, string state)
    {
        //Debug.Log("Item : " + checks.ToString());
        //Debug.Log("State : " + state);

        if (string.IsNullOrEmpty(state))
        {
            return false;
        }

        string[] stateSplit = state.Split(',');
        Dictionary<string, string> checkState = new Dictionary<string, string>();
        int count = stateSplit.Length;
        for (int i = 0; i < count; i++)
        {
            //Debug.Log("Split : " + stateSplit[i]);

            string[] split = stateSplit[i].Split('=');
            checkState.Add(split[0], split[1]);
        }

        foreach (var item in checks)
        {
            string compare = checkState.TryGetValue(item.Key, out string value) ? value : "";
            string[] itemSplit = item.Value.ToString().Split('|');

            for (int i = 0; i < itemSplit.Length; i++)
            {
                if (itemSplit[i] == compare)
                {
                    return true;
                }
            }
        }
        return false;

    }

    private void SetModel(JToken modelInfo)
    {
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

        GameObject modelElementParent = new GameObject("Model");
        modelElementParent.transform.SetParent(transform);
        modelElementParent.transform.localPosition = Vector3.zero;

        SetModelByMinecraftModel(modelData, modelElementParent);

        // X축, Y축 회전을 명확히 설정
        Quaternion modelXRot = Quaternion.Euler(xRot, 0, 0);
        Quaternion modelYRot = Quaternion.Euler(0, yRot, 0);

        //Vector3 xAxis = Vector3.right;
        //Vector3 yAxis = Vector3.up;
        //Vector3 center = new Vector3(0.5f, 0.5f, 0.5f);
        //Vector3 modelCenter = modelElementParent.transform.TransformPoint(center);

        //Debug.Log("Before rotation: " + modelElementParent.transform.eulerAngles);

        modelElementParent.transform.localRotation = modelYRot * modelXRot;

        //modelElementParent.transform.RotateAround(modelCenter, yAxis, -yRot);
        //modelElementParent.transform.RotateAround(modelCenter, xAxis, -xRot);

        //Debug.Log("After rotation: " + modelElementParent.transform.eulerAngles);
    }

    // blockmodel로 생성하기
    void SetModelByMinecraftModel(MinecraftModelData model, GameObject modelElementParent)
    {
        Debug.Log(model.ToString());

        if (model.elements == null)
        {
            // 셜커상자, 침대 등등

        }



        // 모델 데이터를 이용해서 블록을 생성
        int count = model.elements.Count;
        for (int i = 0; i < count; i++)
        {
            JObject element = model.elements[i];
            
            MeshRenderer cubeObject = Instantiate(Resources.Load<MeshRenderer>("minecraft/block"), modelElementParent.transform);
            cubeObject.transform.localPosition = Vector3.zero;
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

            Vector3 size = (to - from) / 32.0f;
            Vector3 center = (from + to) / 32.0f;

            // 큐브의 크기 적용
            cubeObject.transform.localScale = size;

            // 큐브의 회전을 설정
            SetRotation(element, cubeObject, size);

            // 최종 위치 설정
            cubeObject.transform.localPosition = center - new Vector3(0.5f, 0.5f, 0.5f);

            // 큐브의 텍스쳐를 설정
            SetFaces(model, element, cubeObject);
        }
    }

    private void SetRotation(JObject element, MeshRenderer cubeObject, Vector3 size)
    {
        if (element.ContainsKey("rotation"))
        {
            JObject rotation = element["rotation"] as JObject;

            // origin 값 확인 및 월드 좌표 변환
            Vector3 origin = new Vector3(
                rotation["origin"][0].Value<float>(),
                rotation["origin"][1].Value<float>(),
                rotation["origin"][2].Value<float>()
            ) / 16.0f;
            Vector3 worldOrigin = cubeObject.transform.parent.TransformPoint(origin);

            // 회전축 및 각도 설정
            Vector3 axis = rotation["axis"].ToString() switch
            {
                "x" => Vector3.right,
                "y" => Vector3.up,
                "z" => Vector3.forward,
                _ => Vector3.zero
            };
            float angle = rotation["angle"].Value<float>();

            // 회전 적용
            cubeObject.transform.RotateAround(worldOrigin, axis, angle);

            // 스케일 재조정 (rescale 옵션 적용)
            if (rotation.TryGetValue("rescale", out JToken rescaleToken) && rescaleToken.Value<bool>())
            {
                float scaleFactor = Mathf.Sqrt(2.0f); // 대각선 길이 보정
                cubeObject.transform.localScale = size * scaleFactor;
            }
        }
    }

    private void SetFaces(MinecraftModelData model, JObject element, MeshRenderer cubeObject)
    {
        if (!element.ContainsKey("faces"))
        {
            return;
        }

        Texture texture = null;
        JObject faces = element["faces"] as JObject;
        bool IsTransparented = false;
        foreach (var face in faces)
        {
            JObject faceData = face.Value as JObject;
            // 각 텍스처 로드 및 설정
            var faceTexture = faceData["texture"];
            int idx = MinecraftModelData.faceToTextureName[face.Key];

            Texture2D blockTexture = CreateTexture(faceTexture.ToString(), model.textures);

            if (!IsTransparented)
            {            
                bool isTransparent = CheckForTransparency(blockTexture);

                if (isTransparent)
                {
                    Material[] materials = new Material[cubeObject.materials.Length];
                    int count = cubeObject.materials.Length;
                    for (int i = 0; i < count; i++)
                    {
                        materials[i] = GameManager.GetManager<BDObjectManager>().BDObjTransportMaterial;

                        materials[i].mainTexture = cubeObject.materials[i].mainTexture;
                        materials[i].mainTextureOffset = cubeObject.materials[i].mainTextureOffset;
                        materials[i].mainTextureScale = cubeObject.materials[i].mainTextureScale;
                    }
                    cubeObject.materials = materials;

                    IsTransparented = true;
                }
            }

            Material mat = cubeObject.materials[idx];

            if (faceData.ContainsKey("uv"))
            {
                //Debug.Log("UV Found " + face.Key);
                // UV 설정
                JArray uvArray = faceData["uv"] as JArray;
                Vector4 uv = new Vector4(
                    uvArray[0].Value<float>(), // xMin
                    uvArray[1].Value<float>(), // yMin
                    uvArray[2].Value<float>(), // xMax
                    uvArray[3].Value<float>()  // yMax
                );

                // UV 변환
                float textureSize = 16.0f; // Minecraft 텍스처 기준
                Vector2 uvMin = new Vector2(uv.x / textureSize, uv.y / textureSize);
                Vector2 uvMax = new Vector2(uv.z / textureSize, uv.w / textureSize);
                Vector2 uvOffset = uvMin;
                Vector2 uvScale = uvMax - uvMin;

                mat.mainTextureOffset = uvOffset;
                mat.mainTextureScale = uvScale;
            }
            mat.mainTexture = blockTexture;


            if (texture == null)
            {
                texture = mat.mainTexture;
            }
        }

        // 빈공간 채우기
        foreach (var items in MinecraftModelData.faceToTextureName)
        {
            if (!faces.ContainsKey(items.Key))
            {
                cubeObject.materials[items.Value].mainTexture = texture;
            }
        }

        // 레드스톤 와이어 특수 처리
        if (blockName.StartsWith("redstone_wire"))
        {
            Debug.Log("Redstone wire");
            int cnt = cubeObject.materials.Length;
            for (int i = 0; i < cnt; i++)
            {
                cubeObject.materials[i].color = Color.red;
            }
        }
    }

    void SetAABBBounds()
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

        //bool isInitialized = false;
        //int count = modelElementParent.transform.childCount;
        //for (int i = 0; i < count; i++)
        //{
        //    Transform child = modelElementParent.transform.GetChild(i);

        //    MeshRenderer renderer = child.GetComponent<MeshRenderer>();
        //    if (renderer != null)
        //    {
        //        if (!isInitialized)
        //        {
        //            aabb = renderer.bounds;
        //            isInitialized = true;
        //        }
        //        else
        //        {
        //            aabb.Encapsulate(renderer.bounds);
        //        }
        //    }
        //}
    }

    // 텍스쳐 생성
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

    bool CheckForTransparency(Texture2D texture)
    {
        Color[] pixels = texture.GetPixels();

        foreach (Color pixel in pixels)
        {
            if (pixel.a < 1.0f)
            {
                return true; // 투명 또는 반투명 픽셀 존재
            }
        }
        return false; // 완전히 불투명
    }
}
