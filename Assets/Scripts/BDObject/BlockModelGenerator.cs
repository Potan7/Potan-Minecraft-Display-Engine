using UnityEngine;
using Newtonsoft.Json.Linq;
using System;
using Minecraft;

public class BlockModelGenerator : MonoBehaviour
{
    public MinecraftModelData modelData;
    public string modelName;
    public Color color = Color.white;

    public void SetModelByBlockState(JToken modelInfo)
    {
        // 모덜 정보 세팅
        string modelLocation;
        JObject modelObject;

        if (modelInfo.Type == JTokenType.Array)
        {
            modelObject = modelInfo[0] as JObject;
            modelLocation = modelInfo[0]["model"].ToString();
            //CustomLog.Log("Model : " + modelLocation);
        }
        else
        {
            modelObject = modelInfo as JObject;
            modelLocation = modelInfo["model"].ToString();
        }

        int xRot = modelObject.TryGetValue("x", out JToken xToken) ? xToken.Value<int>() : 0;
        int yRot = modelObject.TryGetValue("y", out JToken yToken) ? yToken.Value<int>() : 0;
        bool uvlock = modelObject.TryGetValue("uvlock", out JToken uvlockToken) ? uvlockToken.Value<bool>() : false;

        SetModel(modelLocation);

        // X축, Y축 회전을 명확히 설정
        Quaternion modelXRot = Quaternion.Euler(-xRot, 0, 0);
        Quaternion modelYRot = Quaternion.Euler(0, -yRot, 0);

        transform.localRotation = modelYRot * modelXRot;
    }

    public void SetModel(string modelLocation)
    {
        // 불러온 모델을 바탕으로 생성하기
        modelLocation = MinecraftFileManager.RemoveNamespace(modelLocation);
        modelData = MinecraftFileManager.GetModelData("models/" + modelLocation + ".json").UnpackParent();
        BDObjectManager bdManager = GameManager.GetManager<BDObjectManager>();

        //Debug.Log("Model Data: " + modelData);

        // 모델 데이터를 이용해서 블록을 생성
        int count = modelData.elements.Count;
        for (int i = 0; i < count; i++)
        {
            JObject element = modelData.elements[i];

            MeshRenderer cubeObject = Instantiate(bdManager.cubePrefab, transform);
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
            SetFaces(modelData, element, cubeObject);
        }
    }

    protected void SetRotation(JObject element, MeshRenderer cubeObject, Vector3 size)
    {
        if (!element.ContainsKey("rotation"))
        {
            return;
        }

        JObject rotation = element["rotation"] as JObject;

        // origin 값 확인 및 월드 좌표 변환
        Vector3 origin = new Vector3(
            rotation["origin"][0].Value<float>(),
            rotation["origin"][1].Value<float>(),
            rotation["origin"][2].Value<float>()
        ) / 16.0f;
        Vector3 worldOrigin = cubeObject.transform.parent.position + origin;

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

    protected void SetFaces(MinecraftModelData model, JObject element, MeshRenderer cubeObject)
    {
        if (!element.TryGetValue("faces", out JToken facesToken)) return;
        JObject faces = facesToken as JObject;

        Texture texture = null;
        bool isTextureAnimated = false;

        bool IsTransparented = false;

        ReadOnlySpan<string> NoTransparent = new[] { "bed", "fire", "banner" };
        for (int i = 0; i < NoTransparent.Length; i++)
        {
            if (modelName.Contains(NoTransparent[i]))
                IsTransparented = true;
        }

        // 각 면을 채우기
        foreach (var face in faces)
        {
            JObject faceData = face.Value as JObject;
            // 각 face의 텍스처 로드 및 설정
            var faceTexture = faceData["texture"];

            Enum.TryParse(face.Key, true, out MinecraftModelData.FaceDirection dir);
            int idx = (int)dir;

            string texturePath = DisplayObject.GetTexturePath(faceTexture.ToString(), model.textures);
            Texture2D blockTexture = CreateTexture(texturePath);
            bool IsAnimated = MinecraftFileManager.IsTextureAnimated(texturePath);

            // 투명도 체크
            if (!IsTransparented)
            {
                if (CheckForTransparency(blockTexture))
                {
                    IsTransparented = true;

                    // 모든 재질 변경하기
                    var cubeMaterials = cubeObject.materials;
                    int cnt = cubeObject.materials.Length;
                    Material tshader = GameManager.GetManager<BDObjectManager>().BDObjTransportMaterial;

                    for (int i = 0; i < cnt; i++)
                    {
                        cubeMaterials[i] = tshader;
                    }
                    cubeObject.materials = cubeMaterials;
                }
            }

            Material mat = cubeObject.materials[idx];

            if (IsAnimated)
            {
                // 애니메이션인 경우 첫번째 칸 선택
                float uvY = 16.0f * (16.0f / blockTexture.height);
                Vector4 uv = new Vector4(0, 0, 16, uvY);
                mat.SetVector("_UVFace", uv);
            }
            else if (faceData.ContainsKey("uv"))
            {
                // UV 설정: [xMin, yMin, xMax, yMax] (Minecraft 기준 16x16)
                JArray uvArray = faceData["uv"] as JArray;
                Vector4 uv = new Vector4(
                    uvArray[0].Value<float>(),
                    uvArray[1].Value<float>(),
                    uvArray[2].Value<float>(),
                    uvArray[3].Value<float>()
                );

                mat.SetVector("_UVFace", uv);
            }

            // rotation 적용: faceData의 uv 영역 내에서 회전할 각도 (0, 90, 180, 270)
            if (faceData.ContainsKey("rotation"))
            {
                int rotation = faceData["rotation"].Value<int>() % 360;
                if (rotation < 0)
                    rotation += 360;
                // 커스텀 쉐이더에서 uv 영역만 회전하도록 _Rotation 프로퍼티를 사용합니다.
                mat.SetFloat("_Rotation", -rotation);
            }
            else
            {
                mat.SetFloat("_Rotation", 0);
            }

            // 최종 텍스처 적용
            mat.mainTexture = blockTexture;

            if (texture == null)
            {
                texture = mat.mainTexture;
                isTextureAnimated = IsAnimated;
            }
        }

        // face에 명시되지 않은 면은 기본 텍스처로 채움
        const int faceCount = 6;
        for (int i = 0; i < faceCount; i++)
        {
            if (cubeObject.materials[i].mainTexture == null)
            {
                cubeObject.materials[i].mainTexture = texture;

                if (isTextureAnimated)
                {
                    float uvY = 16.0f * (16.0f / texture.height);
                    Vector4 uv = new Vector4(0, 0, 16, uvY);
                    cubeObject.materials[i].SetVector("_UVFace", uv);
                }
            }
        }
        /*
        foreach (MinecraftModelData.FaceDirection direction in Enum.GetValues(typeof(MinecraftModelData.FaceDirection)))
        {
            string key = direction.ToString();
            if (!faces.ContainsKey(key))
            {
                cubeObject.materials[(int)direction].mainTexture = texture;
            }
        }
        */

        // 레드스톤 와이어 특수 처리
        if (modelName.Contains("redstone_wire"))
        {
            //CustomLog.Log("Redstone wire");
            int cnt = cubeObject.materials.Length;
            for (int i = 0; i < cnt; i++)
            {
                cubeObject.materials[i].color = Color.red;
            }
        }
        else if (modelName.Contains("banner") && element.ContainsKey("color"))
        {
            
            int cnt = cubeObject.materials.Length;
            for (int i = 0; i < cnt; i++)
            {
                cubeObject.materials[i].color = color;
            }
        }
    }

    protected virtual Texture2D CreateTexture(string path)
    {
        return MinecraftFileManager.GetTextureFile(path);
    }

    // 투명한 부분이 있는지 확인
    protected virtual bool CheckForTransparency(Texture2D texture)
    {
        if (texture == null)
        {
            return false;
        }


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
