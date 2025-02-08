using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

public class BDObject
{
    public string name;
    public string nbt;
    public bool isBlockDisplay = false;
    public bool isItemDisplay = false;
    public float[] transforms = null;
    public BDObject[] children = null;

    [JsonExtensionData]
    public Dictionary<string, object> ExtraData;
}

