using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BDObject
{
    public string name;
    public string nbt;
    public bool isBlockDisplay = false;
    public bool isItemDisplay = false;
    public List<float> transforms;
    public List<BDObject> children;

    [JsonExtensionData]
    public Dictionary<string, object> ExtraData;



}

