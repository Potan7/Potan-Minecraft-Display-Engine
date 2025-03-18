using System.Collections.Generic;
using System.Linq;
using BDObjectSystem;
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

        public bool isSelected;
        public int tick;
        public int interpolation;
        public string fileName;

        public BdObject Info;
        public Dictionary<string, BdObject> IDDataDict;
        private Timeline _timeline;
        
        public TickLine tickLine;

        public void Init(string initFileName, int initTick, int inter, BdObject info, AnimObject obj, Timeline timeLine)
        {
            //Debug.Log("tick : " + tick);
            fileName = initFileName;
            animObject = obj;
            _initColor = outlineImage.color;
            Info = info;
            tick = initTick;
            SetInter(inter);
            _timeline = timeLine;

            UpdatePos();
            _timeline.OnGridChanged += UpdatePos;

            IDDataDict = BdObjectHelper.SetDisplayIDDictionary(info);
            // IDDataDict = BdObjectHelper.SetDictionary(
            //     Info,
            //     bdObject => bdObject,
            //     bdObject => bdObject.Children ?? Enumerable.Empty<BdObject>()
            // );
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

        // 변경된 Grid에 맞추어 위치 변경 
        private void UpdatePos()
        {
            var line = _timeline.GetTickLine(tick, false);
            if (line is null)
            {
                gameObject.SetActive(false);
            }
            else
            {
                gameObject.SetActive(true);
                //var pos = line.rect.anchoredPosition.x;
                //rect.anchoredPosition = new Vector2(line.rect.anchoredPosition.x, rect.anchoredPosition.y);
                rect.position = new Vector2(line.rect.position.x, rect.position.y);
                tickLine = line;
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
            var line = _timeline.GetTickLine(mouse);

            SetTick(line.Tick);

            if (Input.GetMouseButtonUp(0))
            {
                isSelected = false;
                outlineImage.color = _initColor;
            }
        }
        

        // ������ ����
        public void RemoveFrame()
        {
            _timeline.OnGridChanged -= UpdatePos;
            animObject.RemoveFrame(this);
        }

    
    }
}
