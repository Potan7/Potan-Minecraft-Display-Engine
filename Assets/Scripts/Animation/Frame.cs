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

    public BDObject info;

    public void Init(int tick, BDObject Info, AnimObject obj)
    {
        //Debug.Log("tick : " + tick);
        animObject = obj;
        initColor = outlineImage.color;
        info = Info;
        Tick = tick;
        SetInter(GameManager.Instance.Setting.DefaultInterpolation);

        UpdatePos(tick);
        GameManager.GetManager<AnimManager>().Timeline.OnGridChanged += () => UpdatePos(Tick);
    }

    public int SetTick(int tick)
    {
        if (animObject.ChangePos(this, Tick, tick))
        {
            Tick = tick;
            UpdatePos(tick);
            return tick;
        }
        else
        {
            return Tick;
        }
    }

    private void UpdatePos(int tick)
    {
        var line = GameManager.GetManager<AnimManager>().Timeline.GetTickLine(tick, false);
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

    public void SetInter(int inter)
    {
        interpolation = inter;
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
}
