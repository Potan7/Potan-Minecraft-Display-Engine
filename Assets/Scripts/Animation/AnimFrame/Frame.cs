using System.Collections.Generic;
using BDObjectSystem;
using UnityEngine;
using UnityEngine.EventSystems;
using BDObjectSystem.Utility;
using UnityEngine.UI;
using Animation.UI;

namespace Animation.AnimFrame
{
    public class Frame : MonoBehaviour, IPointerDownHandler//, IPointerUpHandler
    {
        [Header("Frame Components")]
        public RectTransform rect;
        public Image outlineImage;
        public AnimObject animObject;

        private Color _initColor;
        private readonly Color _selectedColor = Color.yellow;

        [Header("Frame Info")]
        public bool isSelected;
        public int tick;
        public int interpolation;
        public string fileName;
        public TickLine tickLine;
        public RectTransform interpolationRect;

        [Header("BDObject Info")]
        public BdObject Info;
        public List<BdObject> leafObjects;

        private Timeline _timeline;


        public void Init(string initFileName, int initTick, int inter, BdObject info, AnimObject obj, Timeline timeLine)
        {
            //Debug.Log("tick : " + tick);
            fileName = initFileName;
            animObject = obj;
            _initColor = outlineImage.color;
            _timeline = timeLine;
            Info = info;
            tick = initTick;
            SetInter(inter);

            UpdatePos();
            _timeline.OnGridChanged += UpdatePos;

            //IDDataDict = BdObjectHelper.SetDisplayIDDictionary(info);
            leafObjects = BdObjectHelper.SetDisplayList(info);
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
            UpdateInterpolationBar();
        }

        public bool SetInter(int inter)
        {
            if (interpolation < 0)
                return false;

            interpolation = inter;
            UpdateInterpolationBar();

            return true;
        }

        private void UpdateInterpolationBar()
        {
            if (interpolation == 0)
            {
                interpolationRect.gameObject.SetActive(false);
            }
            else
            {
                interpolationRect.gameObject.SetActive(true);

                var line = _timeline.GetTickLine(tick + interpolation, false);
                line ??= _timeline.grid[_timeline.gridCount - 1];

                // 1. line의 World Position 구함
                Vector3 lineWorldPos = line.rect.position;

                // 2. interpolationRect의 부모 기준 Local Position으로 변환
                RectTransform parentRect = interpolationRect.parent as RectTransform;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect,
                    RectTransformUtility.WorldToScreenPoint(null, lineWorldPos),
                    null,
                    out Vector2 localPos);

                // 3. interpolationRect 위치 조정 (왼쪽 고정이니까 anchoredPosition은 그대로)
                float width = localPos.x - interpolationRect.anchoredPosition.x;
                width += line.rect.rect.width / 2; // line의 rect width 반영

                interpolationRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            }
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


        public void RemoveFrame()
        {
            _timeline.OnGridChanged -= UpdatePos;
            animObject.RemoveFrame(this);
        }


    }
}
