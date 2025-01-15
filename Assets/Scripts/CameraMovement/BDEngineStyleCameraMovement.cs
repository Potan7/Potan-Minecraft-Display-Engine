using UnityEngine;

public class BDEngineStyleCameraMovement : MonoBehaviour
{
    public Transform pivot;               // 카메라가 회전할 중심점
    public float rotationSpeed = 5f;      // 회전 속도
    public float panSpeed = 5f;           // 상하좌우 이동 속도
    public float zoomSpeed = 10f;         // 줌 속도
    public float minDistance = 2f;        // 중심점에서 최소 거리
    public float maxDistance = 50f;       // 중심점에서 최대 거리

    private float currentDistance;        // 현재 중심점과의 거리
    private Vector3 lastMousePosition;    // 마우스 마지막 위치

    private Vector3 pivotInitPos;
    private float InitDistance;

    void Start()
    {
        // 초기 거리 설정
        currentDistance = Vector3.Distance(transform.position, pivot.position);

        pivotInitPos = pivot.position;
        InitDistance = currentDistance;
    }

    void Update()
    {
        // 마우스 입력 처리
        HandleMouseInput();

        if (Input.GetKeyDown(KeyCode.F))
        {
            // 카메라와 pivot 초기 위치로 이동
            pivot.position = pivotInitPos;
            currentDistance = InitDistance;
            transform.position = pivotInitPos + new Vector3(0, 0, -currentDistance);
            transform.LookAt(pivotInitPos);
        }
    }

    void HandleMouseInput()
    {
        // 마우스 왼쪽 클릭: 회전
        if (Input.GetMouseButton(0))
        {
            RotateAroundPivot();
        }

        // 마우스 오른쪽 클릭: 상하좌우 이동
        if (Input.GetMouseButton(1))
        {
            PanCamera();
        }

        // 마우스 휠: 전진/후진
        ZoomCamera();
    }

    void RotateAroundPivot()
    {
        Vector3 mouseDelta = Input.mousePosition - lastMousePosition;

        // 마우스 이동량에 따라 회전 각도 계산
        float yaw = mouseDelta.x * rotationSpeed * Time.deltaTime;   // Y축 회전
        float pitch = -mouseDelta.y * rotationSpeed * Time.deltaTime; // X축 회전

        // 중심점을 기준으로 회전
        transform.RotateAround(pivot.position, Vector3.up, yaw);        // Y축 회전
        transform.RotateAround(pivot.position, transform.right, pitch); // X축 회전

        // 카메라와 중심점의 거리 유지
        Vector3 direction = (transform.position - pivot.position).normalized;
        transform.position = pivot.position + direction * currentDistance;

        // 카메라가 항상 중심점을 바라보도록 설정
        transform.LookAt(pivot);
    }

    void PanCamera()
    {
        Vector3 mouseDelta = Input.mousePosition - lastMousePosition;

        // 마우스 이동량에 따라 이동량 계산
        Vector3 right = transform.right * mouseDelta.x * panSpeed * Time.deltaTime;
        Vector3 up = transform.up * mouseDelta.y * panSpeed * Time.deltaTime;

        // 카메라 위치를 이동
        transform.position += right + up;

        // 중심점도 함께 이동
        pivot.position += right + up;
    }

    void ZoomCamera()
    {
        float scrollDelta = Input.GetAxis("Mouse ScrollWheel");

        // 거리 변경
        currentDistance -= scrollDelta * zoomSpeed * Time.deltaTime;
        currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);

        // 카메라 위치 업데이트
        Vector3 direction = (transform.position - pivot.position).normalized;
        transform.position = pivot.position + direction * currentDistance;
    }

    void LateUpdate()
    {
        // 마우스 마지막 위치 업데이트
        lastMousePosition = Input.mousePosition;
    }
}
