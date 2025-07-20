using GameSystem;
using TransformHandles;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class HandleManager : BaseManager
{
    private Handle _mainHandle;
    private Transform _selectedTarget;

    public bool IsHandleInteracting { get; private set; }

    // 마우스 클릭 시작 위치를 저장할 변수
    private Vector2 _clickStartPosition;
    // 드래그로 간주할 최소 거리 (픽셀 단위)
    private const float DRAG_THRESHOLD = 5f;

    /// <summary>
    /// 지정된 타겟을 선택하고 핸들을 활성화합니다.
    /// </summary>
    /// <param name="target">선택할 오브젝트의 Transform</param>
    public void SelectTarget(Transform target)
    {
        // 이미 같은 타겟이 선택된 경우 아무것도 하지 않음
        if (_selectedTarget == target)
        {
            return;
        }

        if (_selectedTarget != null && _mainHandle != null)
        {
            // 이전에 선택된 타겟의 핸들을 제거
            TransformHandleManager.Instance.RemoveTarget(_selectedTarget, _mainHandle);
            _mainHandle = null;
        }

        if (_mainHandle == null)
        {
            // 새로운 타겟에 대한 핸들을 생성
            _mainHandle = TransformHandleManager.Instance.CreateHandle(target);
            _mainHandle.OnInteractionStartEvent += OnHandleInteractionStart;
            _mainHandle.OnInteractionEndEvent += OnHandleInteractionEnd;
        }
        else
        {
            TransformHandleManager.Instance.AddTarget(target, _mainHandle);
        }

        _selectedTarget = target;
    }

    /// <summary>
    /// 현재 활성화된 핸들을 제거하고 선택을 해제합니다.
    /// </summary>
    public void DeselectAll()
    {
        if (_mainHandle != null && _selectedTarget != null)
        {
            TransformHandleManager.Instance.RemoveTarget(_selectedTarget, _mainHandle);
            _mainHandle = null;
            _selectedTarget = null;
        }
    }

    private void Update()
    {
        // 마우스 왼쪽 버튼을 눌렀을 때, 시작 위치 기록
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            _clickStartPosition = Mouse.current.position.ReadValue();
        }

        // 마우스 왼쪽 버튼을 뗐을 때, 드래그가 아니었다면 선택 해제 로직 실행
        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            // 핸들 조작 중이거나 UI 위에 마우스가 있으면 아무것도 하지 않음
            if (IsHandleInteracting || EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            // 마우스를 누른 위치와 뗀 위치의 거리를 계산
            float mouseDragDistance = Vector2.Distance(_clickStartPosition, Mouse.current.position.ReadValue());

            // 거리가 지정된 임계값(THRESHOLD)보다 작으면 '클릭'으로 간주
            if (mouseDragDistance < DRAG_THRESHOLD)
            {
                Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
                // Raycast를 실행해서 아무것도 맞지 않았다면 허공을 클릭한 것
                if (!Physics.Raycast(ray))
                {
                    DeselectAll();
                }
            }
        }
    }

    private void OnHandleInteractionStart(Handle handle)
    {
        GameManager.GetManager<BdEngineStyleCameraMovement>().enableCameraMovement = false;
        IsHandleInteracting = true;
    }

    private void OnHandleInteractionEnd(Handle handle)
    {
        GameManager.GetManager<BdEngineStyleCameraMovement>().enableCameraMovement = true;
        IsHandleInteracting = false;
    }
}