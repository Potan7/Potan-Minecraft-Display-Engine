using System.Collections;
using CameraMovement;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Animation
{
    public class AnimPanel : MonoBehaviour
    {
        private AnimManager _manager;

        public DragPanel dragPanel;
        public RectTransform animPanel;
        private Vector2 _initPos;
        private bool _isHiding;
        [FormerlySerializedAs("IsMouseEnter")] public bool isMouseEnter;

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
            dragPanel.SetPanelSize(0);
        }

        private void Update()
        {
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

        public void TogglePanel()
        {
            if (_isHiding)
            {
                StopAllCoroutines();
                StartCoroutine(MovePanelCoroutine(_initPos.y));
            }
            else
            {
                StopAllCoroutines();
                StartCoroutine(MovePanelCoroutine(0));
            }

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
