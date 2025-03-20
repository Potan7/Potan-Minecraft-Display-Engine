using System.Collections.Generic;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.Serialization;

namespace Minecraft
{
    [System.Serializable]
    public class MinecraftModelData
    {
        public enum FaceDirection
        {
            [UsedImplicitly] Up = 0,
            [UsedImplicitly] North = 5,
            [UsedImplicitly] West = 4,
            [UsedImplicitly] Down = 3,
            [UsedImplicitly] East = 2,
            [UsedImplicitly] South = 1
        }
        
        public string Parent;
        
        //public string gui_light;
        //public JObject display;
        public JObject Textures;
        public List<JObject> Elements;

        //public JArray texture_size;

        // ReSharper disable Unity.PerformanceAnalysis
        public MinecraftModelData UnpackParent()
        {

            if (string.IsNullOrEmpty(Parent)) return this;

            if (Parent == "builtin/generated") return this;

            var parentData =
                MinecraftFileManager.GetModelData("models/" + MinecraftFileManager.RemoveNamespace(Parent) + ".json")
                .UnpackParent();

            MergeJObject(ref Textures, parentData.Textures);
            MergeList(ref Elements, parentData.Elements);

            Parent = null;
            return this;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        private static void MergeJObject(ref JObject target, JObject source)
        {
            if (source == null) return;

            target ??= new JObject();

            foreach (var property in source.Properties())
            {

                // �ڽİ� �θ� ��ġ�� �ڽ��� �켱����
                if (target.ContainsKey(property.Name) == false)
                {
                    target[property.Name] = property.Value;
                }
            }
        }

        private static void MergeList(ref List<JObject> target, List<JObject> source)
        {
            if (source == null) return;

            target ??= new List<JObject>();

            target.AddRange(source);
        }
    }
}
