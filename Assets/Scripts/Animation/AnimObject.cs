using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class AnimObject : MonoBehaviour
{
    public RectTransform rect;
    public TextMeshProUGUI title;
    public List<Frame> frames;

    AnimObjList manager;

    public void Init(string name, AnimObjList list)
    {
        title.text = name;
        manager = list;

        frames[0].Init(0, 0, this);
    }

    public void OnEventTriggerClick(BaseEventData eventData)
    {
        PointerEventData pointerData = eventData as PointerEventData;

        if (pointerData.button == PointerEventData.InputButton.Right)
        {
            //Debug.Log("Right Click");
            var line = manager.Timeline.GetTickLine(pointerData.position);
            GameManager.GetManager<ContextMenuManager>().ShowContextMenu(this, line.Tick);
        }
    }

    public void AddFrame(BDObject[] obj, int tick)
    {
        var frame = Instantiate(manager.framePrefab, transform.GetChild(0));
        frame.Init(tick, 0, this);
        frames.Add(frame);
    }
}
