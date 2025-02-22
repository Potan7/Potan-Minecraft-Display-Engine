using Newtonsoft.Json.Linq;
using UnityEngine;
using Minecraft;

public class BlockDisplay : ModelDisPlayObject
{
    public BlockModelGenerator modelElementParent;


    public override void LoadDisplayModel(string name, string state)
    {
        //CustomLog.Log(name + ", " + state);

        // 블록 스테이트를 불러와서
        modelName = name;
        modelElementParent.modelName = name;
        JObject blockState = MinecraftFileManager.GetJSONData("blockstates/" + name + ".json");
        //CustomLog.Log("BlockState : " + blockState.ToString());
        //CustomLog.Log("State : " + state);

        // variants 형식일 경우
        if (blockState.TryGetValue("variants", out JToken variant))
        {
            //CustomLog.Log("Variants : " + variants.ToString());

            // 블록 스테이트에 해당하는 모델을 불러옴
            modelElementParent.SetModelByBlockState((variant as JObject)[state]);
        }
        else if (blockState.TryGetValue("multipart", out JToken multi))
        {
            // multipart 형식일 경우
            var multipart = multi as JArray;
            //CustomLog.Log("Multipart : " + multipart.ToString());

            int cnt = multipart.Count;
            bool needNewOne = false;
            for (int i = 0; i < cnt; i++)
            {
                //CustomLog.Log("Part : " + multipart[i].ToString());
                JObject partObject = multipart[i] as JObject;

                bool check = true;
                if (partObject.TryGetValue("when", out JToken value))
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
                        BlockModelGenerator newModel = Instantiate(GameManager.GetManager<BDObjectManager>().blockPrefab, transform);
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

        modelData = modelElementParent.modelData;
        modelName = modelElementParent.modelName;
        //CustomLog.Log("AABB : " + AABBBound.ToString());
        //CustomLog.Log("AABB : " + AABBBound.min);
    }

    private bool CheckState(JObject when, string state)
    {
        if (when.TryGetValue("OR", out JToken ORValue))
        {
            var OR = ORValue as JArray;
            for (int i = 0; i < OR.Count; i++)
            {
                if (CheckStateName(OR[i] as JObject, state))
                {
                    return true;
                }
            }
            return false;
        }
        else if (when.TryGetValue("AND", out JToken ANDValue))
        {
            var AND = ANDValue as JArray;
            for (int i = 0; i < AND.Count; i++)
            {
                if (!CheckStateName(AND[i] as JObject, state))
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
        //CustomLog.Log("Item : " + checks.ToString());
        //CustomLog.Log("State : " + state);

        if (string.IsNullOrEmpty(state))
        {
            return false;
        }

        string[] stateSplit = state.Split(',');
        int stateCount = stateSplit.Length;

        foreach (var item in checks)
        {
            bool matched = false;

            for (int i = 0; i < stateCount; i++)
            {
                string[] split = stateSplit[i].Split('=');
                if (split[0] == item.Key)   // 키가 같은지 확인
                {
                    string[] itemSplit = item.Value.ToString().Split('|');
                    for (int j = 0; j < itemSplit.Length; j++)
                    {
                        if (itemSplit[j] == split[1])
                        {
                            matched = true;
                            break;
                        }
                    }
                    break; // 키가 같다면 이 부분은 끝남.
                }
            }

            if (!matched)
                return false;
        }
        return true;
    }

}
