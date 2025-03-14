using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

//[System.Serializable]
namespace BDObject
{
    public class BdObject
    {
        public string Name;
        public string Nbt;
        public bool IsBlockDisplay = false;
        public bool IsItemDisplay = false;
        public bool IsTextDisplay = false;
        public float[] Transforms = null;
        public JObject Options = null;

        [JsonIgnore]
        public string ID;

        public BdObject[] Children = null;

        [JsonExtensionData]
        public Dictionary<string, object> ExtraData;

        [OnDeserialized]
        public void OnDeserialized(StreamingContext context)
        {
            var uuid = BdObjectHelper.GetUuid(Nbt);
            if (!string.IsNullOrEmpty(uuid))
            {
                ID = uuid;
                return;
            }

            var tag = BdObjectHelper.GetTags(Nbt);
            if (!string.IsNullOrEmpty(tag))
            {
                ID = tag;
                return;
            }

            ID = GetID();
        }

        public string GetID()
        {
            if (!string.IsNullOrEmpty(ID)) return ID;

            if (Children == null) ID = Name;
            else
            {
                var childSum = "";
                foreach (var child in Children)
                {
                    childSum += child.GetID();
                }

                if (Children.Length <= 1) childSum += Name;
                ID = childSum;
            }
            
            return ID;
        }
    }
}

