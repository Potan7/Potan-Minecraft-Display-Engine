using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using BDObjectSystem.Utility;
using UnityEngine;

//[System.Serializable]
namespace BDObjectSystem
{
    public class BdObject
    {
        // JSON Property
        public string Name;
        public string Nbt;
        public bool IsBlockDisplay;
        public bool IsItemDisplay;
        public bool IsTextDisplay;
        public float[] Transforms;

        public JObject Options;
        public BdObject[] Children;
        
        [JsonExtensionData]
        public Dictionary<string, object> ExtraData;

        // Additional Property
        [JsonIgnore] private string _id;
        public string ID => GetID();
        // [field: JsonIgnore] public string ID { get; set; }

        [JsonIgnore]
        public BdObject Parent;
        
        public bool IsDisplay => IsBlockDisplay || IsItemDisplay || IsTextDisplay;

        [OnDeserialized]
        public void OnDeserialized(StreamingContext context)
        {
            var uuid = BdObjectHelper.GetUuid(Nbt);
            if (!string.IsNullOrEmpty(uuid))
            {
                _id = uuid;
                return;
            }

            var tag = BdObjectHelper.GetTags(Nbt);
            if (!string.IsNullOrEmpty(tag))
            {
                _id = tag;
            }
        }

        private string GetID()
        {
            if (!string.IsNullOrEmpty(_id)) return _id;
        
            if (Children == null) _id = Name;
            else
            {
                var childSum = new StringBuilder();
                foreach (var child in Children)
                {
                    childSum.Append(child.ID);
                    // childSum += child.GetID();
                }

                if (Children.Length <= 1) childSum.Append("g");
                _id = childSum.ToString();
            }
            
            return _id;
        }
    }
}

