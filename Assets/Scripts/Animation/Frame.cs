using System.Collections.Generic;
using System.Linq;
using BDObject;
using Manager;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Animation
{
    public class Frame : MonoBehaviour, IPointerDownHandler//, IPointerUpHandler
    {
        public RectTransform rect;
        public Image outlineImage;
        public AnimObject animObject;

        private Color _initColor;
        private readonly Color _selectedColor = Color.yellow;

        [FormerlySerializedAs("IsSelected")] public bool isSelected;
        [FormerlySerializedAs("Tick")] public int tick;
        public int interpolation;
        public string fileName;

        public BdObject Info;
        public Dictionary<string, BdObject> IDDataDict;

        public void Init(string initFileName, int initTick, int inter, BdObject info, AnimObject obj)
        {
            //Debug.Log("tick : " + tick);
            fileName = initFileName;
            animObject = obj;
            _initColor = outlineImage.color;
            Info = info;
            tick = initTick;
            SetInter(inter);

            UpdatePos();
            GameManager.GetManager<AnimManager>().timeline.OnGridChanged += UpdatePos;

            IDDataDict = BdObjectHelper.SetDictionary(
                Info,
                bdObject => bdObject,
                bdObject => bdObject.Children ?? Enumerable.Empty<BdObject>()
            );
        }

        public int SetTick(int newTick)
        {
            // 0�̸� ����
            if (tick == 0)
                return tick;
            if (!animObject.ChangePos(this, tick, newTick)) return tick;
            
            tick = newTick;
            UpdatePos();
            return newTick;

        }

        // ��ġ ������Ʈ
        private void UpdatePos()
        {
            var line = GameManager.GetManager<AnimManager>().timeline.GetTickLine(tick, false);
            if (!line)
            {
                gameObject.SetActive(false);
            }
            else
            {
                gameObject.SetActive(true);
                rect.anchoredPosition = new Vector2(line.rect.anchoredPosition.x, rect.anchoredPosition.y);
            }
        }

        public bool SetInter(int inter)
        {
            interpolation = inter;
            return true;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                isSelected = true;
                outlineImage.color = _selectedColor;
            }
            else
            {
                GameManager.GetManager<ContextMenuManager>().ShowContextMenu(this);
            }
        }

        private void Update()
        {
            if (!isSelected) return;
            
            Vector2 mouse = Input.mousePosition;
            var line = GameManager.GetManager<AnimManager>().timeline.GetTickLine(mouse);

            SetTick(line.Tick);

            if (Input.GetMouseButtonUp(0))
            {
                isSelected = false;
                outlineImage.color = _initColor;
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            
            isSelected = false;
            outlineImage.color = _initColor;
        }

        // ������ ����
        public void RemoveFrame()
        {
            GameManager.GetManager<AnimManager>().timeline.OnGridChanged -= UpdatePos;
            animObject.RemoveFrame(this);
        }

    
    }
}
