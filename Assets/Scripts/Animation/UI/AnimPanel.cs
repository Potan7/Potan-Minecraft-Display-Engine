using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Animation.AnimFrame;
using BDObjectSystem;
using Cysharp.Threading.Tasks;
using GameSystem;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Animation.UI
{
    public class AnimPanel : MonoBehaviour
    {
        #region Variables
        private AnimManager _manager;

        public DragPanel dragPanel;
        public RectTransform animPanel;
        private Vector2 _initPos;
        private bool _isHiding;
        public bool isMouseEnter;
        public TMP_InputField tickField;
        public TMP_InputField tickSpeedField;

        public Image playPauseButton;
        public Sprite playSprite;
        public Sprite pauseSprite;

        public bool IsMiddleClicked;
        // 누적값 변수 선언 (픽셀 단위)
        private float accumulatedDelta = 0f;

        int tickMove = 0;
        private float _continuousTickMoveTimer = 0f; // 시간 누적을 위한 변수

        public RectTransform specificScrollViewRect; // 스크롤뷰

        // StringBuilder를 재사용 (GC 할당 방지)
        private readonly StringBuilder _stringBuilder = new StringBuilder(16);

        #endregion

        #region Unity Methods

        private void Start()
        {
            _manager = GetComponent<AnimManager>();

            animPanel = dragPanel.animPanel;
            _initPos = new Vector2(0, 225);
            AnimManager.TickChanged += AnimManager_TickChanged;

            _isHiding = false;
            dragPanel.SetDragPanel(!_isHiding);
            dragPanel.SetPanelSize(_initPos.y);
        }

        private void Update()
        {
            isMouseEnter = RectTransformUtility.RectangleContainsScreenPoint(
                    animPanel, Input.mousePosition, null
                );

            // Boxing 방지: HasFlag 대신 비트 연산 사용
            var hasAnimUIPanel = (UIManager.CurrentUIStatus & UIManager.UIStatus.OnAnimUIPanel) != 0;

            if (!isMouseEnter && hasAnimUIPanel)
            {
                UIManager.SetUIStatus(UIManager.UIStatus.OnAnimUIPanel, false);
            }
            else if (isMouseEnter && !hasAnimUIPanel)
            {
                UIManager.SetUIStatus(UIManager.UIStatus.OnAnimUIPanel, true);
            }

            if (Mouse.current.middleButton.wasPressedThisFrame)
            {
                // 마우스 버튼이 눌리면 IsMiddleClicked를 true로 설정
                IsMiddleClicked = true;
            }
            // 예: Update() 내에서 처리
            if (isMouseEnter && IsMiddleClicked)
            {
                // 마우스 이동량 (픽셀 단위) 읽기
                var mouseDelta = Mouse.current.delta.ReadValue();
                float move = mouseDelta.x; // 좌우 이동량

                // 누적 값에 현재 프레임의 이동량을 더합니다.
                accumulatedDelta += move;

                // 임계치 (threshold): 누적 이동량이 몇 픽셀 이상이면 한 칸 이동
                float threshold = 10f; // 필요에 따라 조정

                // 누적 이동량이 threshold 이상일 경우 처리 (양방향)
                if (Mathf.Abs(accumulatedDelta) >= threshold)
                {
                    // 넘은 픽셀 수만큼 몇 칸 이동할지 정하기
                    // 예를 들어, 10픽셀당 2 tick 이동
                    int ticksToMove = (int)(accumulatedDelta / threshold);

                    // 현재 시작 tick 가져오기
                    int startTick = _manager.timeline.startTick;

                    // 음수, 양수에 따라 새로운 tick 계산 (예외 처리 필요 시 추가)
                    int newTick = startTick + ticksToMove;
                    // 예: newTick이 0보다 작으면 0으로 제한
                    newTick = Mathf.Max(newTick, 0);

                    // Debug.Log($"Tick: {_manager.Tick} / StartTick: {startTick} / TicksToMove: {ticksToMove} / NewTick: {newTick}");

                    // 그리드 업데이트 (tick 텍스트 수정)
                    _manager.timeline.SetTickTexts(newTick);
                    _manager.Tick = _manager.Tick;

                    // 이미 적용한 픽셀 값만큼 누적 값을 차감
                    accumulatedDelta -= ticksToMove * threshold;

                }

                // 휠 버튼이 해제되었으면 상태와 누적값 초기화
                if (Mouse.current.middleButton.isPressed == false)
                {
                    IsMiddleClicked = false;
                    accumulatedDelta = 0f;
                }
            }

            if (tickMove != 0)
            {
                _continuousTickMoveTimer += Time.deltaTime;
                // AnimManager로부터 TickUnit 및 TickSpeed를 가져와 tickInterval 계산
                float tickInterval = 0f;
                if (_manager.TickSpeed > 0) // 0으로 나누는 것을 방지
                {
                    tickInterval = _manager.TickUnit / _manager.TickSpeed;
                }

                if (tickInterval > 0) // 유효한 간격일 때만 처리
                {
                    while (_continuousTickMoveTimer >= tickInterval)
                    {
                        _continuousTickMoveTimer -= tickInterval;
                        _manager.TickAdd(tickMove * _manager.TickUnit);
                    }
                }
            }
            else
            {
                _continuousTickMoveTimer = 0f; // 이동이 멈추면 타이머 초기화
            }

        }

        void OnDestroy()
        {
            AnimManager.TickChanged -= AnimManager_TickChanged;
        }
        #endregion

        #region Unity Events

        private void AnimManager_TickChanged(float obj)
        {
            // StringBuilder 사용으로 Boxing 및 GC 할당 방지
            _stringBuilder.Clear();
            _stringBuilder.AppendFormat("{0:F2}", obj);
            tickField.text = _stringBuilder.ToString();
        }

        public void OnTickFieldEndEdit(string value)
        {
            if (int.TryParse(value, out var t))
            {
                _manager.Tick = t;
            }
            else
            {
                // Boxing 방지
                _stringBuilder.Clear();
                _stringBuilder.Append(_manager.Tick);
                tickField.text = _stringBuilder.ToString();
            }
        }

        public void OnTickSpeedFieldEndEdit(string value)
        {
            if (float.TryParse(value, out var t))
            {
                _manager.TickSpeed = t;
            }
            else
            {
                // Boxing 방지
                _stringBuilder.Clear();
                _stringBuilder.Append(_manager.TickSpeed);
                tickSpeedField.text = _stringBuilder.ToString();
            }
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
                // StopAllCoroutines();
                // StartCoroutine(MovePanelCoroutine(_initPos.y));
                MovePanelAsync(_initPos.y).Forget();
            }
            else
            {
                // 아래로 내리기 
                // StopAllCoroutines();
                // StartCoroutine(MovePanelCoroutine(0));
                MovePanelAsync(0).Forget();
            }
            dragPanel.SetDragPanel(_isHiding);

            _isHiding = !_isHiding;
        }

        private async UniTask MovePanelAsync(float targetY)
        {
            float pos = dragPanel.rect.position.y;
            float time = 0f;

            while (time < 1f)
            {
                // 매 프레임 0.03 비율로 Lerp
                pos = Mathf.Lerp(pos, targetY, 0.03f);
                dragPanel.SetPanelSize(pos);

                time += Time.deltaTime;

                // 다음 프레임까지 대기
                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            // 마지막으로 정확히 targetY 설정
            dragPanel.SetPanelSize(targetY);
        }
        #endregion

        #region Input Actions

        public void OnScrollWheel(InputAction.CallbackContext callback)
        {
            if (!isMouseEnter) return;

            bool isMouseOverSpecificScrollView = RectTransformUtility.RectangleContainsScreenPoint(
                specificScrollViewRect, Mouse.current.position.ReadValue(), null // UI 카메라가 필요하면 여기에 전달
            );

            if (isMouseOverSpecificScrollView)
            {
                // 스크롤이 특정 스크롤뷰 위에 있다면 무시
                return;
            }

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
            {
                _manager.TickAdd(-1);
            }
            else if (callback.performed)
            {
                tickMove = -1;
            }
            else
            {
                tickMove = 0;
            }

        }

        public void MoveTickRight(InputAction.CallbackContext callback)
        {
            if (callback.started)
            {
                _manager.TickAdd(1);
            }
            else if (callback.performed)
            {
                tickMove = 1;
            }
            else
            {
                tickMove = 0;
            }
        }


        public void OnResetButton()
        {
            // 애니메이션 초기화
            Stop();

            // 모든 모델 초기화
            GameManager.GetManager<BdObjectManager>().ClearAllObject();

            // 모든 트랙 제거
            GameManager.GetManager<AnimObjList>().ResetAnimObject();

            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void OnPressDeleteKey(InputAction.CallbackContext callback)
        {
            if (!callback.performed) return;

            var contextMenu = GameManager.GetManager<ContextMenuManager>();
            if (contextMenu.currentType == ContextMenuManager.ContextMenuType.Frame && contextMenu.isMenuActive == true)
            {
                contextMenu.OnFrameRemoveButton();
                return;
            }
            // else if (GameManager.GetManager<AnimObjList>().selectedFrames.Count > 0)
            var objList = GameManager.GetManager<AnimObjList>();
            if (objList.selectedFrames.Count > 0)
            {
                // 선택된 프레임 삭제
                var frames = new List<Frame>(objList.selectedFrames);
                foreach (var frame in frames)
                {
                    frame.RemoveFrame();
                }
                objList.selectedFrames.Clear();
            }
        }

        public void OnPressAddKey(InputAction.CallbackContext callback)
        {
            if (!callback.performed) return;

            var contextMenu = GameManager.GetManager<ContextMenuManager>();
            if (contextMenu.currentType == ContextMenuManager.ContextMenuType.NewFrame && contextMenu.isMenuActive == true)
            {
                contextMenu.OnAddFrameButtonClicked();
            }
        }

        public void OnPressDuplicateKey(InputAction.CallbackContext callback)
        {
            if (!callback.performed) return;

            GameManager.GetManager<AnimObjList>().DuplicateSelectedFrames();
        }
        #endregion
    }
}
