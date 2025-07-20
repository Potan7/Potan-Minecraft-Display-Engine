using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GameSystem
{
    public class BdEngineStyleCameraMovement : BaseManager
    {
        //public static bool CanMoveCamera { get; set; } = true;

        [Header("References")]
        public Transform pivot; // camera pivot point (target)

        [Header("Camera Movement Settings")]
        public bool enableCameraMovement = true; // Enable/Disable camera movement
        public float rotateSpeed;
        public float rotationSpeedRange = 15f;
        public float minRotationSpeed = 1f; // cameraRotateSpeed * rotationSpeedRange + minRotationSpeed

        public float panSpeed;
        public float panSpeedRange = -9f;
        public float minPanSpeed = -1f; // panSpeed * panSpeedRange + minPanSpeed

        public float zoomSpeed;
        public float zoomSpeedRange = 50f;
        public float minZoomSpeed = 1f; // zoomSpeed * zoomSpeedRange + minZoomSpeed
        public float minDistance = 2f;    // ī�޶�~�ǹ� �ּ� �Ÿ�
        public float maxDistance = 50f;   // ī�޶�~�ǹ� �ִ� �Ÿ�

        private float _currentDistance;    // ���� ī�޶�~�ǹ� �Ÿ�
        private Vector3 _pivotInitPos;     // �ǹ� �ʱ� ��ġ
        private float _initDistance;       // ī�޶� �ʱ� �Ÿ�

        [Header("Input Actions")]
        // MyCameraActions.inputactions (Asset) ����
        public InputActionAsset inputActions;

        // ���ο��� ã�Ƽ� �� Action Map �� Action ����
        private InputActionMap _cameraMap;
        private InputAction _rotateAction;
        private InputAction _panAction;
        private InputAction _lookDeltaAction;
        private InputAction _zoomAction;

        private void OnEnable()
        {
            // �ǹ��� ������ ��ũ��Ʈ �ߴ�
            if (pivot == null)
            {
                Debug.LogError("Pivot is not assigned.");
                enabled = false;
                return;
            }

            // �ʱ� �� ����
            _currentDistance = Vector3.Distance(transform.position, pivot.position);
            _initDistance = _currentDistance;
            _pivotInitPos = pivot.position;
            transform.LookAt(pivot);

            // --- 1) Action Map �������� ---
            //    (InputActionAsset �ȿ� "Camera" ���� �����ؾ� ��)
            _cameraMap = inputActions.FindActionMap("Camera", throwIfNotFound: true);

            // --- 2) Action ���� ã�� ---
            _rotateAction = _cameraMap.FindAction("Rotate", throwIfNotFound: true);      // Button
            _panAction = _cameraMap.FindAction("Pan", throwIfNotFound: true);           // Button
            _lookDeltaAction = _cameraMap.FindAction("LookDelta", throwIfNotFound: true); // Vector2
            _zoomAction = _cameraMap.FindAction("Zoom", throwIfNotFound: true);         // float

            // --- 3) Enable ---
            _cameraMap.Enable(); // or rotateAction.Enable(); panAction.Enable(); ...

            // ����: cameraMap.Enable() �� ȣ���ϸ� 
            //       cameraMap �ȿ� �ִ� ��� �׼��� �� ���� Enable �˴ϴ�.
        }

        private void OnDisable()
        {
            // Disable
            _cameraMap?.Disable();
            // �Ǵ� ���� �׼ǵ� rotateAction?.Disable(); ��
        }

        private void Update()
        {
            //if (!CanMoveCamera) return;
            if (!enableCameraMovement) return; // Only when no panel is open

            // Action의 현재 값 읽기
            var rotatePressed = _rotateAction.ReadValue<float>() > 0.5f;   // 마우스 우클릭 버튼
            var panPressed = _panAction.ReadValue<float>() > 0.5f;         // 마우스 휠클릭 버튼
            var lookDelta = _lookDeltaAction.ReadValue<Vector2>();     // 마우스 이동
            var zoomValue = _zoomAction.ReadValue<float>();              // 마우스 휠

            // --- 1) 회전 ---
            if (rotatePressed && lookDelta.sqrMagnitude > 0.0001f)
            {
                RotateAroundPivot(lookDelta);
            }

            // --- 2) 팬 ---
            if (panPressed && lookDelta.sqrMagnitude > 0.0001f)
            {
                PanCamera(lookDelta);
            }

            // --- 3) 줌 ---
            if (Mathf.Abs(zoomValue) > 0.0001f)
            {
                ZoomCamera(zoomValue);
            }
        }

        private void RotateAroundPivot(Vector2 delta)
        {
            // dt(Time.deltaTime)를 제거했으므로 speed는 이제 '감도' 역할을 합니다.
            var speed = rotateSpeed * rotationSpeedRange + minRotationSpeed;
            var yaw = delta.x * speed * 0.01f; // dt 대신 감도 조절을 위한 작은 상수를 곱합니다.
            var pitch = -delta.y * speed * 0.01f; // 상하 이동은 반대(-)

            // 1) yaw : pivot 기준 수직 Up
            transform.RotateAround(pivot.position, Vector3.up, yaw);

            // 2) pitch : 카메라 기준 Right
            transform.RotateAround(pivot.position, transform.right, pitch);

            // 3) 거리 유지 & pivot 바라보기
            var direction = (transform.position - pivot.position).normalized;
            transform.position = pivot.position + direction * _currentDistance;
            transform.LookAt(pivot);
        }

        private void PanCamera(Vector2 delta)
        {
            // dt(Time.deltaTime)를 제거했으므로 speed는 이제 '감도' 역할을 합니다.
            var speed = panSpeed * panSpeedRange + minPanSpeed;
            var rightMovement = transform.right * (delta.x * speed * 0.01f); // dt 대신 감도 조절을 위한 작은 상수를 곱합니다.
            var upMovement = transform.up * (delta.y * speed * 0.01f);
            var panMovement = rightMovement + upMovement;

            transform.position += panMovement;
            pivot.position += panMovement;
        }

        private void ZoomCamera(float zoomValue)
        {
            // dt(Time.deltaTime)를 제거했으므로 speed는 이제 '감도' 역할을 합니다.
            var speed = zoomSpeed * zoomSpeedRange + minZoomSpeed;
            _currentDistance -= zoomValue * speed * 0.1f; // dt 대신 감도 조절을 위한 작은 상수를 곱합니다.
            _currentDistance = Mathf.Clamp(_currentDistance, minDistance, maxDistance);

            var direction = (transform.position - pivot.position).normalized;
            transform.position = pivot.position + direction * _currentDistance;
        }
        
        public void ResetCamera()
        {
            pivot.position = _pivotInitPos;
            _currentDistance = _initDistance;

            transform.position = _pivotInitPos + new Vector3(0, 0, -_currentDistance);
            transform.LookAt(_pivotInitPos);
        }
    }
}
