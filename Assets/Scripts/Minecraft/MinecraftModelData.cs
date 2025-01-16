using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

[SerializeField]
public class MinecraftModelData
{
    public static Dictionary<string, int> faceToTextureName = new Dictionary<string, int>
    {
        { "north", 1 },
        { "south", 5 },
        { "west", 2 },
        { "east", 4 },
        { "up", 0 },
        { "down", 3 }
    };

    public string parent;
    public string gui_light;
    public JObject display;
    public JObject textures;
    public List<JObject> elements;

    public MinecraftModelData UnpackParent()
    {

        if (string.IsNullOrEmpty(parent))
        {
            return this;
        }

        MinecraftModelData parentData =
            MinecraftFileManager.GetModelData("models/" + MinecraftFileManager.RemoveNamespace(parent) + ".json")
            .UnpackParent();

        if (string.IsNullOrEmpty(gui_light))
        {
            gui_light = parentData.gui_light;
        }

        MergeJObject(ref display, parentData.display);
        MergeJObject(ref textures, parentData.textures);
        MergeList(ref elements, parentData.elements);

        parent = null;
        return this;
    }

    override public string ToString()
    {
        return JsonConvert.SerializeObject(this, Formatting.Indented);
    }

    private void MergeJObject(ref JObject target, JObject source)
    {
        if (source == null) return;

        if (target == null)
        {
            target = new JObject();
        }

        foreach (var property in source.Properties())
        {

            // 자식과 부모가 겹치면 자식이 우선순위
            if (target.ContainsKey(property.Name) == false)
            {
                target[property.Name] = property.Value;
            }
        }
    }

    private void MergeList(ref List<JObject> target, List<JObject> source)
    {
        if (source == null) return;

        if (target == null)
        {
            target = new List<JObject>();
        }

        target.AddRange(source);
    }
}