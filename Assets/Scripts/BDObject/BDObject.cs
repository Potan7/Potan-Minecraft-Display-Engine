using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using UnityEngine;

public class BDObject
{
    public string name;
    public string nbt;
    public bool isBlockDisplay = false;
    public bool isItemDisplay = false;
    public bool isTextDisplay = false;
    public float[] transforms = null;
    public JObject options = null;

    public BDObject[] children = null;

    [JsonExtensionData]
    public Dictionary<string, object> ExtraData;
}

