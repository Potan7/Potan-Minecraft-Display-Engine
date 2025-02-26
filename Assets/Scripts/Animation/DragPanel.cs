using UnityEngine;
using UnityEngine.EventSystems;

public class DragPanel : MonoBehaviour, 
    IPointerDownHandler, IPointerMoveHandler, IPointerUpHandler
{
    bool isDragging = false;
    public RectTransform AnimPanel;
    public RectTransform rect;

    public void OnPointerDown(PointerEventData eventData)
    {
        isDragging = true;
        BDEngineStyleCameraMovement.CanMoveCamera = false;
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (isDragging)
        {
            SetPanelSize(eventData.position);
        }
    }

    public void SetPanelSize(Vector2 pos)
    {
        AnimPanel.offsetMax = new Vector2(AnimPanel.offsetMax.x, -(1080 - pos.y));
        rect.position = new Vector3(rect.position.x, pos.y, rect.position.z);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;
        BDEngineStyleCameraMovement.CanMoveCamera = true;
    }
}
