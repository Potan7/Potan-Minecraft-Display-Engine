using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Frame : MonoBehaviour, IPointerDownHandler//, IPointerUpHandler
{
    public RectTransform rect;
    public Image outlineImage;
    public AnimObject animObject;

    Color initColor;
    Color selectedColor = Color.yellow;

    public bool IsSelected = false;
    public int Tick;
    public int interpolation;
    public string fileName;

    public BDObject info;
    public Dictionary<string, BDObject> IDDataDict;

    public void Init(string FileName, int tick, int inter, BDObject Info, AnimObject obj)
    {
        //Debug.Log("tick : " + tick);
        fileName = FileName;
        animObject = obj;
        initColor = outlineImage.color;
        info = Info;
        Tick = tick;
        SetInter(inter);

        UpdatePos();
        GameManager.GetManager<AnimManager>().Timeline.OnGridChanged += UpdatePos;

        IDDataDict = BDObjectHelper.SetDictionary(
            info,
            obj => obj,
            obj => obj.children ?? Enumerable.Empty<BDObject>()
            );
    }

    public int SetTick(int tick)
    {
        // 0이면 고정
        if (Tick == 0)
            return Tick;
        if (animObject.ChangePos(this, Tick, tick))
        {
            Tick = tick;
            UpdatePos();
            return tick;
        }
        else
        {
            return Tick;
        }
    }

    // 위치 업데이트
    private void UpdatePos()
    {
        var line = GameManager.GetManager<AnimManager>().Timeline.GetTickLine(Tick, false);
        if (line == null)
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
            IsSelected = true;
            outlineImage.color = selectedColor;
        }
        else
        {
            GameManager.GetManager<ContextMenuManager>().ShowContextMenu(this);
        }
    }

    private void Update()
    {
        if (IsSelected)
        {
            Vector2 mouse = Input.mousePosition;
            var line = GameManager.GetManager<AnimManager>().Timeline.GetTickLine(mouse);

            SetTick(line.Tick);

            if (Input.GetMouseButtonUp(0))
            {
                IsSelected = false;
                outlineImage.color = initColor;
            }
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            IsSelected = false;
            outlineImage.color = initColor;
        }
    }

    // 프레임 제거
    public void RemoveFrame()
    {
        GameManager.GetManager<AnimManager>().Timeline.OnGridChanged -= UpdatePos;
        animObject.RemoveFrame(this);
    }

    
}
