using System;
using GameSystem;
using Minecraft;
using Newtonsoft.Json.Linq;

namespace BDObjectSystem.Display
{
    public class BlockDisplay : ModelDisPlayObject
    {
        public BlockModelGenerator modelElementParent;


        public override void LoadDisplayModel(string mName, string state)
        {
            //CustomLog.Log(name + ", " + state);

            // ���� ������Ʈ�� �ҷ��ͼ�
            modelName = mName;
            modelElementParent.modelName = mName;
            var blockState = MinecraftFileManager.GetJsonData("blockstates/" + mName + ".json");
            //CustomLog.Log("BlockState : " + blockState.ToString());
            //CustomLog.Log("State : " + state);

            // variants ������ ���
            if (blockState.TryGetValue("variants", out var variant))
            {
                //CustomLog.Log("Variants : " + variants.ToString());

                // ���� ������Ʈ�� �ش��ϴ� ���� �ҷ���
                modelElementParent.SetModelByBlockState(((JObject)variant)[state]);
            }
            else if (blockState.TryGetValue("multipart", out var multi))
            {
                // multipart ������ ���
                var multipart = multi as JArray;
                //CustomLog.Log("Multipart : " + multipart.ToString());

                var cnt = multipart.Count;
                var needNewOne = false;
                for (var i = 0; i < cnt; i++)
                {
                    //CustomLog.Log("Part : " + multipart[i].ToString());
                    var partObject = multipart[i] as JObject;

                    var check = true;
                    if (partObject.TryGetValue("when", out var value))
                    {
                        check = CheckState(value as JObject, state);
                    }

                    if (check)
                    {
                        if (!needNewOne)
                        { 
                            modelElementParent.SetModelByBlockState(partObject["apply"]);
                            needNewOne = true;
                        }
                        else
                        {
                            var newModel = Instantiate(GameManager.GetManager<BdObjectManager>().blockPrefab, transform);
                            newModel.modelName = modelElementParent.modelName;
                            newModel.SetModelByBlockState(partObject["apply"]);
                        }

                        //CustomLog.Log("Part : " + partObject["apply"].ToString());
                    }
                }

            }
            else
            {
                CustomLog.LogError("Unknown blockstate format");
            }

            SetAABBBounds();

            ModelData = modelElementParent.ModelData;
            modelName = modelElementParent.modelName;
            //CustomLog.Log("AABB : " + AABBBound.ToString());
            //CustomLog.Log("AABB : " + AABBBound.min);
        }

        private static bool CheckState(JObject when, string state)
        {
            if (when.TryGetValue("OR", out var orValue))
            {
                var or = orValue as JArray;
                for (var i = 0; i < or.Count; i++)
                {
                    if (CheckStateName(or[i] as JObject, state))
                    {
                        return true;
                    }
                }
                return false;
            }

            if (when.TryGetValue("AND", out var andValue))
            {
                var and = andValue as JArray;
                for (var i = 0; i < and.Count; i++)
                {
                    if (!CheckStateName(and[i] as JObject, state))
                    {
                        return false;
                    }
                }
                return true;
            }

            return CheckStateName(when, state);
        }

        private static bool CheckStateName(JObject checks, string state)
        {
            //CustomLog.Log("Item : " + checks.ToString());
            //CustomLog.Log("State : " + state);

            if (string.IsNullOrEmpty(state)) return false;

            // State 분리
            var stateSplit = state.Split(',');
            var stateCount = stateSplit.Length;

            foreach (var item in checks)
            {
                var matched = false;

                for (var i = 0; i < stateCount; i++)
                {
                    var split = stateSplit[i].Split('=');
                    
                    if (split[0] != item.Key) continue; // 비교할 key랑 다르면 스킵 
                    
                    var itemSplit = item.Value.ToString().Split('|');
                    
                    foreach (var t in itemSplit)
                    {
                        // ReSharper disable once InvertIf
                        if (t == split[1])
                        {
                            matched = true;
                            break;
                        }
                    }
                    break; // Ű�� ���ٸ� �� �κ��� ����.
                }

                if (!matched)
                    return false;
            }
            return true;
        }

    }
}
