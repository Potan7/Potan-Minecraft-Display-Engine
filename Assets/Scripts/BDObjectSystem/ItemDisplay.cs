using System;
using System.Linq;
using Manager;
using Minecraft;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace BDObjectSystem
{
    public class ItemDisplay : ModelDisPlayObject
    {
        public ItemModelGenerator itemModel;
        private JObject _currentItemState;

        public override void LoadDisplayModel(string mName, string state)
        {
            // items ������ ������ �����ͼ�
            modelName = mName;
            var itemState = MinecraftFileManager.GetJsonData("items/" + mName + ".json");

            if (!itemState.ContainsKey("model"))
            {
                CustomLog.LogError("Model not found: " + mName);
                return;
            }

            // �� ������ ������
            _currentItemState = itemState.GetValue("model") as JObject;

            CheckModelType(_currentItemState);
        }

        private void CheckModelType(JObject model)
        {
            //CustomLog.Log(model["type"] + " : " + model);
            switch (model["type"].ToString())
            {
                case "minecraft:model":
                    TypeModel(model["model"].ToString());
                    break;
                case "minecraft:condition":
                    CheckModelType(model["on_false"] as JObject);
                    break;
                case "minecraft:select":
                    TypeSelect(model);
                    break;
                case "minecraft:special":
                    TypeSpecial(model);
                    break;
                case "minecraft:composite":
                    var parts = model["models"] as JArray;
                    var cnt = parts.Count();
                    for (var i = 0; i < cnt; i++)
                    {
                        CheckModelType(parts[i] as JObject);
                    }
                    break;
                case "minecraft:range_dispatch":

                    CheckModelType(model["entries"][0]["model"] as JObject);
                    break;
                default:
                    CustomLog.LogError("Unknown model type: " + model["type"]);
                    break;
            }
        }

        private void TypeModel(string model)
        {
            //Debug.Log("Model: " + model);
            //string model = itemState["model"].ToString();
            if (model.StartsWith("minecraft:block/"))
            {
                GenerateUsingBlockModel(model);
            }
            else
            {
                SetItemModel(model);
            }
        }

        private void GenerateUsingBlockModel(string model, Color co)
        {
            var bd = Instantiate(GameManager.GetManager<BdObjectManager>().blockPrefab, transform);
            bd.modelName = model;
            bd.color = co;
            bd.SetModel(model);
        }

        private void GenerateUsingBlockModel(string model)
        {
            GenerateUsingBlockModel(model, Color.white);
        }

        private void TypeSelect(JObject itemState)
        {
            // gui�� ���� ���� ã�Ƽ� ����
            var cases = itemState["cases"] as JArray;
            foreach (var item in cases)
            {
                var caseItem = item as JObject;
                if (!caseItem.ContainsKey("when"))
                {
                    CheckModelType(caseItem["model"] as JObject);
                    return;
                }

                if (caseItem["when"] is JArray)
                {
                    var cnt = caseItem["when"].Count();
                    for (var i = 0; i < cnt; i++)
                    {
                        var when = caseItem["when"][i].ToString();
                        if (when == "gui")
                        {
                            CheckModelType(caseItem["model"] as JObject);
                            return;
                        }
                    }
                }
                else
                {
                    var when = caseItem["when"].ToString();
                    if (when == "gui")
                    {
                        CheckModelType(caseItem["model"] as JObject);
                        return;
                    }
                }
            }

            CheckModelType(itemState["fallback"] as JObject);
        }

        private void TypeSpecial(JObject itemState)
        {
            var specialModel = itemState["model"] as JObject;
            var baseModel = itemState["base"].ToString();
            //Debug.Log("Base: " + baseModel);

            switch (specialModel["type"].ToString())
            {
                case "minecraft:bed":
                case "minecraft:chest":
                case "minecraft:shulker_box":
                case "minecraft:conduit":
                case "minecraft:decorated_pot":
                    GenerateUsingBlockModel(baseModel.Replace("item/", "block/"));
                    break;
                case "minecraft:banner":
                    GenerateUsingBlockModel(
                        "block/" + specialModel["type"],
                        MinecraftColorExtensions.ToColorEnum(specialModel["color"].ToString()).ToColor()
                    );

                    break;
                case "minecraft:head":
                    var head = Instantiate(GameManager.GetManager<BdObjectManager>().headPrefab, transform);
                    head.GenerateHead(specialModel["kind"].ToString());
                    break;
                case "minecraft:shield":
                    //CustomLog.Log("Shield: " + baseModel);
                    GenerateUsingBlockModel(baseModel);
                    break;

            }

            /*
         * ���, ����ü, ���ڱ�, ���� : �� ����, ���� ���÷��� �ʵ� �� ����
         * ħ��, ����, ��Ŀ ���� : �������� �ѱ�� (Done)
         * �Ӹ� : �� ����, ���� ���÷��� �ʵ� �� ���� (�÷��̾� �Ӹ��� profile ó�� �ؾ���)
         * ǥ����, �Ŵ޸� ǥ���� : �� ����, ���� ���÷��� �ʵ� �� ���� �ٵ� BDEngine�� ������ ����(???)
         */
        }


        private void SetItemModel(string modelLocation)
        {
            // �ҷ��� ���� �������� �����ϱ�
            modelLocation = MinecraftFileManager.RemoveNamespace(modelLocation);

            ModelData = MinecraftFileManager.GetModelData("models/" + modelLocation + ".json").UnpackParent();

            //CustomLog.Log("Model Data: " + modelData);
            var layer0 = GetTexturePath(ModelData.Textures["layer0"].ToString(), ModelData.Textures);
            var texture = MinecraftFileManager.GetTextureFile(layer0);
            Texture2D texture2 = null;

            if (ModelData.Textures.TryGetValue("layer1", out var dataTexture))
            {
                var layer1 = GetTexturePath(dataTexture.ToString(), ModelData.Textures);
                texture2 = MinecraftFileManager.GetTextureFile(layer1);
            }

            if (_currentItemState.TryGetValue("tints", out var value))
            {
                SetTint(texture, value[0] as JObject);

                if (texture2)
                {
                    SetTint(texture2, value[1] as JObject);
                }
            }

            itemModel = Instantiate(GameManager.GetManager<BdObjectManager>().itemPrefab, transform);
            itemModel.Init(texture, texture2);
        }

        private static void SetTint(Texture2D texture, JObject tint)
        {
            Debug.Log("Tint: " + tint);
            if (tint["type"].ToString() == "minecraft:constant")
            {
                // ���� ��ȯ
                var packedValue = int.Parse(tint["value"].ToString());

                // ��Ȯ�� RGB �� ���� (��Ʈ ����ũ ����)
                var r = ((packedValue >> 16) & 0xFF) / 255f;
                var g = ((packedValue >> 8) & 0xFF) / 255f;
                var b = (packedValue & 0xFF) / 255f;

                var color = new Color(r, g, b, 1.0f);
                // CustomLog.Log("Color: " + color);

                // �ؽ�ó�� ���� ����
                var pixels = texture.GetPixels();
                for (var i = 0; i < pixels.Length; i++)
                {
                    if (pixels[i].a == 0) continue;
                    pixels[i] = Color.Lerp(pixels[i], color, 0.9f);
                }
                texture.SetPixels(pixels);
                texture.Apply();
            }



        }
    }
}
