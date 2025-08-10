using System.Collections.Generic;
using System.Linq;
using BDObjectSystem.Utility;

namespace BDObjectSystem
{
    /// <summary>
    /// 런타임에서 사용되는 객체로, 데이터(BdObjectData)를 기반으로 추가 로직과 캐싱된 상태를 가집니다.
    /// </summary>
    public class BdObject
    {
        public enum DisplayType
        {
            None,
            BlockDisplay,
            ItemDisplay,
            TextDisplay
        }

        public BdObjectData Data { get; } // 원본 데이터

        public float[] Transforms => Data.transforms;
        public string Name => Data.name;
        public string Nbt => Data.nbt;
        public bool IsBlockDisplay => Data.isBlockDisplay;
        public bool IsItemDisplay => Data.isItemDisplay;
        public bool IsTextDisplay => Data.isTextDisplay;
        public Dictionary<string, object> ExtraData => Data.ExtraData;

        // --- 런타임 속성 및 관계 ---
        public BdObject Parent { get; set; }
        public BdObject[] Children { get; private set; }

        // --- 캐싱된 속성 ---
        private string _id;
        public string ID => GetID();

        public bool IsDisplay => Data.isBlockDisplay || Data.isItemDisplay || Data.isTextDisplay;
        public DisplayType Type
        {
            get
            {
                if (Data.isBlockDisplay) return DisplayType.BlockDisplay;
                if (Data.isItemDisplay) return DisplayType.ItemDisplay;
                if (Data.isTextDisplay) return DisplayType.TextDisplay;
                return DisplayType.None; // 기본값은 None으로 설정
            }
        }
        public bool IsHeadDisplay { get; private set; }

        private bool _isNameParsed = false;
        private string _parsedName;
        private string _parsedState;

        public string ParsedName { get { ParseNameIfNeeded(); return _parsedName; } }
        public string ParsedState { get { ParseNameIfNeeded(); return _parsedState; } }

        /// <summary>
        /// 데이터 모델(BdObjectData)을 기반으로 런타임 객체(BdObject)를 생성합니다.
        /// </summary>
        public BdObject(BdObjectData data, BdObject parent = null)
        {
            Data = data;
            Parent = parent;

            // 자식 객체들도 재귀적으로 생성
            if (data.children != null)
            {
                Children = data.children.Select(childData => new BdObject(childData, this)).ToArray();
            }

            // 역직렬화 시점에 수행하던 초기화 로직
            Initialize();
        }

        internal void Initialize()
        {
            // IsHeadDisplay 값 계산
            IsHeadDisplay = Data.isItemDisplay && (Data.name?.Contains("player_head") ?? false);

            // ID 값 초기화 (UUID 또는 Tag 우선)
            var uuid = BdObjectHelper.GetUuid(Data.nbt);
            if (!string.IsNullOrEmpty(uuid))
            {
                _id = uuid;
                return;
            }
            var tag = BdObjectHelper.GetTags(Data.nbt);
            if (!string.IsNullOrEmpty(tag))
            {
                _id = tag;
            }

            
        }

        private void ParseNameIfNeeded()
        {
            if (_isNameParsed) return;

            if (string.IsNullOrEmpty(Data.name))
            {
                _parsedName = null;
                _parsedState = null;
            }
            else
            {
                var typeStart = Data.name.IndexOf('[');
                if (typeStart == -1)
                {
                    _parsedName = Data.name;
                    _parsedState = string.Empty;
                }
                else
                {
                    _parsedName = Data.name[..typeStart];
                    _parsedState = Data.name[typeStart..].Replace("[", "").Replace("]", "");
                }
            }
            _isNameParsed = true;
        }

        public string GetHeadTexture()
        {
            if (IsHeadDisplay)
            {
                return Data.ExtraData.GetValueOrDefault("defaultTextureValue", string.Empty) as string;
            }
            return string.Empty; // player_head가 아닌 경우 빈 문자열 반환
        }

        private string GetID()
        {
            if (!string.IsNullOrEmpty(_id)) return _id;

            if (Children == null || Children.Length == 0)
            {
                _id = string.Empty;
            }
            else
            {
                var childIds = Children.Select(child => child.GetID()).ToList();
                childIds.Sort();
                _id = $"[{string.Join(",", childIds)}]";
            }
            return _id;
        }

        public string GetEntityType()
        {
            return Data.isBlockDisplay ? "block_display" :
                   Data.isItemDisplay ? "item_display" :
                   Data.isTextDisplay ? "text_display" : null;
        }
        
        /// <summary>
        /// 이 BdObject의 깊은 복사본을 생성합니다.
        /// 복제된 객체는 원본과 상태를 공유하지 않으며, Parent 속성은 null로 초기화됩니다.
        /// </summary>
        /// <returns>완전히 새로운 BdObject 인스턴스입니다.</returns>
        public BdObject Clone()
        {
            // 1. 데이터의 깊은 복사본을 만듭니다.
            BdObjectData clonedData = Data.Clone();

            // 2. 복제된 데이터를 사용하여 새로운 BdObject 런타임 인스턴스를 생성합니다.
            // 생성자에서 자식 객체 생성 및 초기화가 모두 처리됩니다.
            return new BdObject(clonedData);
        }
    }
}

