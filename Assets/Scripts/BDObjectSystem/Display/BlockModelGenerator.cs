using System;
using GameSystem;
using Minecraft;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace BDObjectSystem.Display
{
    public class BlockModelGenerator : MonoBehaviour
    {
        public MinecraftModelData ModelData;
        public string modelName;
        public Color color = Color.white;

        public void SetModelByBlockState(JToken modelInfo)
        {
            // Model Setting
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

            var xRot = modelObject.TryGetValue("x", out var xToken) ? xToken.Value<int>() : 0;
            var yRot = modelObject.TryGetValue("y", out var yToken) ? yToken.Value<int>() : 0;
            //var uvlock = modelObject.TryGetValue("uvlock", out var uvlockToken) && uvlockToken.Value<bool>();

            SetModel(modelLocation);

            // Roate the model
            var modelXRot = Quaternion.Euler(-xRot, 0, 0);
            var modelYRot = Quaternion.Euler(0, -yRot, 0);

            transform.localRotation = modelYRot * modelXRot;
        }

        public void SetModel(string modelLocation)
        {
            // �ҷ��� ���� �������� �����ϱ�
            modelLocation = MinecraftFileManager.RemoveNamespace(modelLocation);
            ModelData = MinecraftFileManager.GetModelData("models/" + modelLocation + ".json").UnpackParent();
            //Debug.Log(ModelData);
            var bdManager = GameManager.GetManager<BdObjectManager>();

            //Debug.Log("Model Data: " + modelData);

            // �� �����͸� �̿��ؼ� ������ ����
            var count = ModelData.Elements.Count;
            for (var i = 0; i < count; i++)
            {
                var element = ModelData.Elements[i];

                var cubeObject = Instantiate(bdManager.cubePrefab, transform);
                //cubeObject.transform.localPosition = new Vector3(0.5f, 0.5f, 0.5f);

                // ť���� ��ġ�� ũ�⸦ ����
                var from = new Vector3(
                    element["from"][0].Value<float>(),
                    element["from"][1].Value<float>(),
                    element["from"][2].Value<float>());
                var to = new Vector3(
                    element["to"][0].Value<float>(),
                    element["to"][1].Value<float>(),
                    element["to"][2].Value<float>());

                var size = (to - from) / 32.0f;
                var center = (from + to) / 32.0f;

                // ť���� ũ�� ����
                cubeObject.transform.localScale = size;

                // ť���� ȸ���� ����
                SetRotation(element, cubeObject, size);
                // ���� ��ġ ����
                cubeObject.transform.localPosition = center - new Vector3(0.5f, 0.5f, 0.5f);

                // ť���� �ؽ��ĸ� ����
                SetFaces(ModelData, element, cubeObject);
            }
        }

        private static void SetRotation(JObject element, MeshRenderer cubeObject, Vector3 size)
        {
            if (!element.TryGetValue("rotation", out var value))
            {
                return;
            }

            var rotation = value as JObject;

            // origin �� Ȯ�� �� ���� ��ǥ ��ȯ
            var origin = new Vector3(
                rotation["origin"][0].Value<float>(),
                rotation["origin"][1].Value<float>(),
                rotation["origin"][2].Value<float>()
            ) / 16.0f;
            var worldOrigin = cubeObject.transform.parent.position + origin;

            // ȸ���� �� ���� ����
            var axis = rotation["axis"].ToString() switch
            {
                "x" => Vector3.right,
                "y" => Vector3.up,
                "z" => Vector3.forward,
                _ => Vector3.zero
            };
            var angle = rotation["angle"].Value<float>();

            // ȸ�� ����
            cubeObject.transform.RotateAround(worldOrigin, axis, angle);

            // ������ ������ (rescale �ɼ� ����)
            if (rotation.TryGetValue("rescale", out var rescaleToken) && rescaleToken.Value<bool>())
            {
                var scaleFactor = Mathf.Sqrt(2.0f); // �밢�� ���� ����
                cubeObject.transform.localScale = size * scaleFactor;
            }
        }

        private void SetFaces(MinecraftModelData model, JObject element, MeshRenderer cubeObject)
        {
            if (!element.TryGetValue("faces", out var facesToken)) return;
            var faces = facesToken as JObject;

            Texture texture = null;
            var isTextureAnimated = false;

            // 만약 반투명한 블록이면 재질 변경
            ReadOnlySpan<string> transparent = new[] { "glass", "honey_block", "slime_block" };
            for (var i = 0; i < transparent.Length; i++)
            {
                if (!modelName.Contains(transparent[i])) continue;
                
                var cubeMaterials = cubeObject.materials;
                var cnt = cubeObject.materials.Length;
                var tshader = GameManager.GetManager<BdObjectManager>().bdObjTransportMaterial;

                for (var j = 0; j < cnt; j++)
                {
                    cubeMaterials[j] = tshader;
                }
                cubeObject.materials = cubeMaterials;
                break;
            }

            const string uvFace = "_UVFace";
            const string matRotation = "_Rotation";
            // �� ���� ä���
            foreach (var face in faces)
            {
                var faceData = face.Value as JObject;
                // �� face�� �ؽ�ó �ε� �� ����
                var faceTexture = faceData["texture"];

                Enum.TryParse(face.Key, true, out MinecraftModelData.FaceDirection dir);
                var idx = (int)dir;

                var texturePath = DisplayObject.GetTexturePath(faceTexture.ToString(), model.Textures);
                var blockTexture = CreateTexture(texturePath);
                var isAnimated = MinecraftFileManager.IsTextureAnimated(texturePath);

                //// ������ üũ
                //if (!IsTransparented)
                //{
                //    if (CheckForTransparency(blockTexture))
                //    {
                //        IsTransparented = true;

                //        // ��� ���� �����ϱ�
                //        var cubeMaterials = cubeObject.materials;
                //        int cnt = cubeObject.materials.Length;
                //        Material tshader = GameManager.GetManager<BDObjectManager>().BDObjTransportMaterial;

                //        for (int i = 0; i < cnt; i++)
                //        {
                //            cubeMaterials[i] = tshader;
                //        }
                //        cubeObject.materials = cubeMaterials;
                //    }
                //}

                var mat = cubeObject.materials[idx];

                if (isAnimated)
                {
                    // Animated Texture
                    //Debug.Log("Animated Texture: " + blockTexture.name);
                    var uvY = 16.0f * (16.0f / blockTexture.height);
                    var uv = new Vector4(0, 0, 16, uvY);
                    mat.SetVector(uvFace, uv);
                }
                else if (faceData.TryGetValue("uv", out var value))
                {
                    // UV ����: [xMin, yMin, xMax, yMax] (Minecraft ���� 16x16)
                    var uvArray = value as JArray;
                    var uv = new Vector4(
                        uvArray[0].Value<float>(),
                        uvArray[1].Value<float>(),
                        uvArray[2].Value<float>(),
                        uvArray[3].Value<float>()
                    );

                    mat.SetVector(uvFace, uv);
                }

                // rotation ����: faceData�� uv ���� ������ ȸ���� ���� (0, 90, 180, 270)
                if (faceData.ContainsKey("rotation"))
                {
                    var rotation = faceData["rotation"].Value<int>() % 360;
                    if (rotation < 0)
                        rotation += 360;
                    // Ŀ���� ���̴����� uv ������ ȸ���ϵ��� _Rotation ������Ƽ�� ����մϴ�.
                    mat.SetFloat(matRotation, -rotation);
                }
                else
                {
                    mat.SetFloat(matRotation, 0);
                }

                // ���� �ؽ�ó ����
                mat.mainTexture = blockTexture;

                if (!texture)
                {
                    texture = mat.mainTexture;
                    isTextureAnimated = isAnimated;
                }
            }

            // face�� ���õ��� ���� ���� �⺻ �ؽ�ó�� ä��
            const int faceCount = 6;
            for (var i = 0; i < faceCount; i++)
            {
                const string uvVector = "_UVFace";
                if (!cubeObject.materials[i].mainTexture)
                {
                    cubeObject.materials[i].mainTexture = texture;

                    if (isTextureAnimated)
                    {
                        var uvY = 16.0f * (16.0f / texture.height);
                        var uv = new Vector4(0, 0, 16, uvY);
                        cubeObject.materials[i].SetVector(uvVector, uv);
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

            // ���彺�� ���̾� Ư�� ó��
            if (modelName.Contains("redstone_wire"))
            {
                //CustomLog.Log("Redstone wire");
                var cnt = cubeObject.materials.Length;
                for (var i = 0; i < cnt; i++)
                {
                    cubeObject.materials[i].color = Color.red;
                }
            }
            else if (modelName.Contains("banner") && element.ContainsKey("color"))
            {
            
                var cnt = cubeObject.materials.Length;
                for (var i = 0; i < cnt; i++)
                {
                    cubeObject.materials[i].color = color;
                }
            }
        }

        protected virtual Texture2D CreateTexture(string path)
        {
            return MinecraftFileManager.GetTextureFile(path);
        }

        // ������ �κ��� �ִ��� Ȯ��
        //protected virtual bool CheckForTransparency(Texture2D texture)
        //{
        //    if (texture == null)
        //    {
        //        return false;
        //    }


        //    Color[] pixels = texture.GetPixels();

        //    foreach (Color pixel in pixels)
        //    {
        //        if (pixel.a < 1.0f && pixel.a > 0.1f)
        //        {
        //            return true; // ������ �ȼ� ����
        //        }
        //    }
        //    return false; // ������ ������
        //}
    }
}
