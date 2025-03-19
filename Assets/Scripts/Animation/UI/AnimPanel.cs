using System.Collections;
using CameraMovement;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Animation.UI
{
    public class AnimPanel : MonoBehaviour
    {
        private AnimManager _manager;

        public DragPanel dragPanel;
        public RectTransform animPanel;
        private Vector2 _initPos;
        private bool _isHiding;
        public bool isMouseEnter;

        public TextMeshProUGUI totalTickText;
        public TMP_InputField tickField;
        public TMP_InputField tickSpeedField;

        public Image playPauseButton;
        public Sprite playSprite;
        public Sprite pauseSprite;

        private void Start()
        {
            _manager = GetComponent<AnimManager>();
            animPanel = dragPanel.animPanel;
            _initPos = new Vector2(0, 225);
            AnimManager.TickChanged += AnimManager_TickChanged;

            _isHiding = true;
            dragPanel.SetDragPanel(!_isHiding);
            dragPanel.SetPanelSize(0);
        }

        private void Update()
        {
            // 마우스가 패널 안에 있으면 카메라 이동 불가능
            if (RectTransformUtility.RectangleContainsScreenPoint(
                    animPanel, Input.mousePosition, null
                ))
            {
                isMouseEnter = true;
                BdEngineStyleCameraMovement.CanMoveCamera = false;
            }
            else
            {
                isMouseEnter = false;
                BdEngineStyleCameraMovement.CanMoveCamera = true;

            }
        }

        public void OnTickFieldEndEdit(string value)
        {
            if (int.TryParse(value, out var t))
                _manager.Tick = t;
            else
                tickField.text = _manager.Tick.ToString();
        }

        public void OnTickSpeedFieldEndEdit(string value)
        {
            if (float.TryParse(value, out var t))
                _manager.TickSpeed = t;
            else
                tickSpeedField.text = _manager.TickSpeed.ToString();
        }

        private void AnimManager_TickChanged(int obj)
        {
            tickField.text = obj.ToString();
        }

        public void Stop()
        {
            _manager.IsPlaying = false;
            playPauseButton.sprite = playSprite;
            _manager.Tick = 0;
        }

        public void PlayPause()
        {

            if (_manager.IsPlaying)
            {
                playPauseButton.sprite = playSprite;
            }
            else
            {
                playPauseButton.sprite = pauseSprite;
            }
            _manager.IsPlaying = !_manager.IsPlaying;
        }

        // 패널 토글 버튼 
        public void TogglePanel()
        {
            if (_isHiding)
            {
                // 위로 올리기
                StopAllCoroutines();
                StartCoroutine(MovePanelCoroutine(_initPos.y));
            }
            else
            {
                // 아래로 내리기 
                StopAllCoroutines();
                StartCoroutine(MovePanelCoroutine(0));
            }
            dragPanel.SetDragPanel(_isHiding);

            _isHiding = !_isHiding;
        }

        private IEnumerator MovePanelCoroutine(float targetY)
        {
            var pos = dragPanel.rect.position.y;

            float time = 0;
            while (time < 1f)
            {
                pos = Mathf.Lerp(pos, targetY, 0.03f);
                dragPanel.SetPanelSize(pos);
                time += Time.deltaTime;
                yield return null;
            }
            dragPanel.SetPanelSize(targetY);
        }

        //public void OnAnimPanelPointer(bool IsEnter)
        //{
        //    BDEngineStyleCameraMovement.CanMoveCamera = !IsEnter;
        //    IsMouseEnter = IsEnter;
        //}

        public void OnScrollWheel(InputAction.CallbackContext callback)
        {
            if (!isMouseEnter) return;

            var scroll = callback.ReadValue<Vector2>();

            switch (scroll.y)
            {
                case > 0.1f:
                    _manager.timeline.ChangeGrid(5);
                    break;
                case < -0.1f when _manager.timeline.gridCount > 20:
                    _manager.timeline.ChangeGrid(-5);
                    break;
            }

        }

        public void MoveTickLeft(InputAction.CallbackContext callback)
        {
            if (callback.started)
                _manager.TickAdd(-1);
            
        }

        public void MoveTickRight(InputAction.CallbackContext callback)
        {
            if (callback.started)
                _manager.TickAdd(1);
        }
    }
}
