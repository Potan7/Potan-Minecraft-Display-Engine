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

            // source(부모)의 프로퍼티를 순회하며 target(자식)에 없는 것만 추가합니다.
            foreach (var property in source.Properties())
            {
                // 자식에 이미 동일한 키가 있으면 부모의 값으로 덮어쓰지 않고, 자식의 값을 유지합니다.
                if (!target.ContainsKey(property.Name))
                {
                    target.Add(property.Name, property.Value);
                }
            }
        }

        private static void MergeList(ref List<JObject> target, List<JObject> source)
        {
            if (source == null) return;

            // 자식에 Element가 없음 -> 부모의 Element를 통째로 복사
            // 자식에 Element가 있으면 부모의 Element는 무시
            if (target == null)
            {
                target = new List<JObject>(source);
                return;
            }
        }
    }
}
