using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;

public class BlockDisplay : DisplayObject
{
    public BlockModelGenerator modelElementParent;

    public override void LoadDisplayModel(string name, string state)
    {
        //CustomLog.Log(name + ", " + state);

        // 블록 스테이트를 불러와서
        modelName = name;
        JObject blockState = MinecraftFileManager.GetJSONData("blockstates/" + name + ".json");
        //CustomLog.Log("BlockState : " + blockState.ToString());
        //CustomLog.Log("State : " + state);

        // variants 형식일 경우
        if (blockState.ContainsKey("variants"))
        {
            JObject variants = blockState["variants"] as JObject;
            //CustomLog.Log("Variants : " + variants.ToString());
            // 블록 스테이트에 해당하는 모델을 불러옴
            if (variants.ContainsKey(state))
            {
                modelElementParent.SetModelByBlockState(variants[state]);
            }
            else
            {
                CustomLog.LogError("State not found: " + state);
            }
        }
        else if (blockState.ContainsKey("multipart"))
        {
            // multipart 형식일 경우
            var multipart = blockState["multipart"] as JArray;
            //CustomLog.Log("Multipart : " + multipart.ToString());

            for (int i = 0; i < multipart.Count; i++)
            {
                //CustomLog.Log("Part : " + multipart[i].ToString());
                JObject partObject = multipart[i] as JObject;

                bool check = true;

                if (partObject.ContainsKey("when"))
                {
                    check = CheckState(partObject["when"] as JObject, state);
                }

                if (check)
                    modelElementParent.SetModelByBlockState(partObject["apply"]);
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
        //CustomLog.Log("Item : " + checks.ToString());
        //CustomLog.Log("State : " + state);

        if (string.IsNullOrEmpty(state))
        {
            return false;
        }

        string[] stateSplit = state.Split(',');
        Dictionary<string, string> checkState = new Dictionary<string, string>();
        int count = stateSplit.Length;
        for (int i = 0; i < count; i++)
        {
            //CustomLog.Log("Split : " + stateSplit[i]);

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

}
