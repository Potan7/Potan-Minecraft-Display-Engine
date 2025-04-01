using GameSystem;
using Riten.Native.Cursors.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

namespace Animation.UI
{
    public class DragPanel : MonoBehaviour, 
        IPointerDownHandler//, IPointerMoveHandler//, IPointerUpHandler
    {
        private bool _isDragging;
        [FormerlySerializedAs("AnimPanel")] public RectTransform animPanel;
        public RectTransform rect;

        public RectTransform canvasRectTransform;

        private OnHoverCursor _onHoverCursor;
        private bool isOnOff;

        private float _lastHeight;
        private float _lastPanelSize;

        private void Start()
        {
            _lastHeight = canvasRectTransform.rect.height;
            _onHoverCursor = GetComponent<OnHoverCursor>();
        }

        public void SetDragPanel(bool isOn)
        {
            isOnOff = isOn;
            _onHoverCursor.enabled = isOn;
        }

        private void Update()
        {
            if (!Mathf.Approximately(_lastHeight, canvasRectTransform.rect.height))
            {
                //Debug.Log("Canvas Height Changed");
                SetPanelSize(_lastPanelSize);
                _lastHeight = canvasRectTransform.rect.height;
            }

            if (_isDragging)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRectTransform,  // ĵ������ RectTransform
                    Input.mousePosition,                   // ���콺 ��ġ (Screen Space)
                    null,                    // ���� ����ϴ� ī�޶�
                    out var localPoint                         // ��ȯ�� UI ��ǥ
                );
                //Debug.Log(localPoint);
                //Debug.Log(canvasRectTransform.rect.height);

                SetPanelSize(canvasRectTransform.rect.height / 2 + localPoint.y);

                if (Input.GetMouseButtonUp(0))
                {
                    _isDragging = false;
                    UIManager.CurrentUIStatus &= ~UIManager.UIStatus.OnDraggingPanel;
                }
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!isOnOff) return;

            _isDragging = true;
            UIManager.CurrentUIStatus |= UIManager.UIStatus.OnDraggingPanel;
        }

        public void SetPanelSize(float y)
        {
            var height = -(canvasRectTransform.rect.height - y);
            animPanel.offsetMax = new Vector2(animPanel.offsetMax.x, height);
            _lastPanelSize = y;

            //rect.position = new Vector3(rect.position.x, y, rect.position.z);
        }

        //public void OnPointerUp(PointerEventData eventData)
        //{
        //    isDragging = false;
        //    BDEngineStyleCameraMovement.CanMoveCamera = true;
        //}
    }
}
