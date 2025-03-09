using UnityEngine;
using UnityEngine.InputSystem;

public class BDEngineStyleCameraMovement : MonoBehaviour
{
    public static bool CanMoveCamera { get; set; } = true;

    [Header("References")]
    public Transform pivot; // 카메라가 바라볼 피벗

    [Header("Camera Movement Settings")]
    public float rotationSpeed = 90f; // 회전 속도 (도/초)
    public float panSpeed = 5f;       // 팬 속도 (유닛/초)
    public float zoomSpeed = 10f;     // 줌 속도
    public float minDistance = 2f;    // 카메라~피벗 최소 거리
    public float maxDistance = 50f;   // 카메라~피벗 최대 거리

    private float currentDistance;    // 현재 카메라~피벗 거리
    private Vector3 pivotInitPos;     // 피벗 초기 위치
    private float initDistance;       // 카메라 초기 거리

    [Header("Input Actions")]
    // MyCameraActions.inputactions (Asset) 참조
    public InputActionAsset inputActions;

    // 내부에서 찾아서 쓸 Action Map 및 Action 참조
    private InputActionMap cameraMap;
    private InputAction rotateAction;
    private InputAction panAction;
    private InputAction lookDeltaAction;
    private InputAction zoomAction;

    private void OnEnable()
    {
        // 피벗이 없으면 스크립트 중단
        if (pivot == null)
        {
            Debug.LogError("Pivot is not assigned.");
            enabled = false;
            return;
        }

        // 초기 값 세팅
        currentDistance = Vector3.Distance(transform.position, pivot.position);
        initDistance = currentDistance;
        pivotInitPos = pivot.position;
        transform.LookAt(pivot);

        // --- 1) Action Map 가져오기 ---
        //    (InputActionAsset 안에 "Camera" 맵이 존재해야 함)
        cameraMap = inputActions.FindActionMap("Camera", throwIfNotFound: true);

        // --- 2) Action 각각 찾기 ---
        rotateAction = cameraMap.FindAction("Rotate", throwIfNotFound: true);      // Button
        panAction = cameraMap.FindAction("Pan", throwIfNotFound: true);           // Button
        lookDeltaAction = cameraMap.FindAction("LookDelta", throwIfNotFound: true); // Vector2
        zoomAction = cameraMap.FindAction("Zoom", throwIfNotFound: true);         // float

        // --- 3) Enable ---
        cameraMap.Enable(); // or rotateAction.Enable(); panAction.Enable(); ...

        // 참고: cameraMap.Enable() 를 호출하면 
        //       cameraMap 안에 있는 모든 액션이 한 번에 Enable 됩니다.
    }

    private void OnDisable()
    {
        // Disable
        cameraMap?.Disable();
        // 또는 개별 액션들 rotateAction?.Disable(); 등
    }

    void Update()
    {
        if (!CanMoveCamera) return;

        // Action의 현재 값 읽기
        bool rotatePressed = rotateAction.ReadValue<float>() > 0.5f;   // 마우스 왼쪽 버튼
        bool panPressed = panAction.ReadValue<float>() > 0.5f;         // 마우스 오른쪽 버튼
        Vector2 lookDelta = lookDeltaAction.ReadValue<Vector2>();     // 마우스 이동
        float zoomValue = zoomAction.ReadValue<float>();              // 마우스 휠

        // --- 1) 회전 ---
        if (rotatePressed && lookDelta.sqrMagnitude > 0.0001f)
        {
            RotateAroundPivot(lookDelta, Time.deltaTime);
        }

        // --- 2) 팬 ---
        if (panPressed && lookDelta.sqrMagnitude > 0.0001f)
        {
            PanCamera(lookDelta, Time.deltaTime);
        }

        // --- 3) 줌 ---
        if (Mathf.Abs(zoomValue) > 0.0001f)
        {
            ZoomCamera(zoomValue, Time.deltaTime);
        }
    }

    private void RotateAroundPivot(Vector2 delta, float dt)
    {
        float yaw = delta.x * rotationSpeed * dt;
        float pitch = -delta.y * rotationSpeed * dt; // 위로 이동시 음(-)

        // 1) yaw : pivot 기준 전역 Up
        transform.RotateAround(pivot.position, Vector3.up, yaw);

        // 2) pitch : 카메라 기준 Right
        transform.RotateAround(pivot.position, transform.right, pitch);

        // 3) 거리 유지 & pivot 바라보기
        Vector3 direction = (transform.position - pivot.position).normalized;
        transform.position = pivot.position + direction * currentDistance;
        transform.LookAt(pivot);
    }

    private void PanCamera(Vector2 delta, float dt)
    {
        Vector3 rightMovement = transform.right * (delta.x * panSpeed * dt);
        Vector3 upMovement = transform.up * (delta.y * panSpeed * dt);
        Vector3 panMovement = rightMovement + upMovement;

        transform.position += panMovement;
        pivot.position += panMovement;
    }

    private void ZoomCamera(float zoomValue, float dt)
    {
        currentDistance -= zoomValue * zoomSpeed * dt;
        currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);

        Vector3 direction = (transform.position - pivot.position).normalized;
        transform.position = pivot.position + direction * currentDistance;
    }

    public void ResetCamera()
    {
        pivot.position = pivotInitPos;
        currentDistance = initDistance;

        transform.position = pivotInitPos + new Vector3(0, 0, -currentDistance);
        transform.LookAt(pivotInitPos);
    }
}
