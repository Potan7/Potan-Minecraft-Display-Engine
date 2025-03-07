using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

//[System.Serializable]
public class BDObject
{
    public string name;
    public string nbt;
    public bool isBlockDisplay = false;
    public bool isItemDisplay = false;
    public bool isTextDisplay = false;
    public float[] transforms = null;
    public JObject options = null;

    [JsonIgnore]
    public string ID;

    public BDObject[] children = null;

    [JsonExtensionData]
    public Dictionary<string, object> ExtraData;

    [OnDeserialized]
    public void OnDeserialized(StreamingContext context)
    {
        var uuid = BDObjectHelper.GetUUID(nbt);
        if (!string.IsNullOrEmpty(uuid))
        {
            ID = uuid;
            return;
        }

        string tag = BDObjectHelper.GetTags(nbt);
        if (!string.IsNullOrEmpty(tag))
        {
            ID = tag;
            return;
        }
    }
}

