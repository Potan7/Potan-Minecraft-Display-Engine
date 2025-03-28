using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CameraMovement
{
    public class BdEngineStyleCameraMovement : MonoBehaviour
    {
        //public static bool CanMoveCamera { get; set; } = true;
        [Flags]
        public enum CameraStatus {
            None = 0,
            OnAnimPanel = 1 << 0,
            OnSettingPanel = 1 << 1,
            OnExportPanel = 1 << 2,
            OnDraggingPanel = 1 << 3,
        }
        public static CameraStatus CurrentCameraStatus { get; set; } = CameraStatus.None;

        [Header("References")]
        public Transform pivot; // ī�޶� �ٶ� �ǹ�

        [Header("Camera Movement Settings")]
        public float cameraRotateSpeed;
        public float rotationSpeedRange = 12f;

        public float panSpeed;
        public float panSpeedRange = -10f;

        public float zoomSpeed;
        public float zoomSpeedRange = 50f;
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
            if (CurrentCameraStatus != CameraStatus.None) return; // Only when no panel is open

            // Action�� ���� �� �б�
            var rotatePressed = _rotateAction.ReadValue<float>() > 0.5f;   // ���콺 ���� ��ư
            var panPressed = _panAction.ReadValue<float>() > 0.5f;         // ���콺 ������ ��ư
            var lookDelta = _lookDeltaAction.ReadValue<Vector2>();     // ���콺 �̵�
            var zoomValue = _zoomAction.ReadValue<float>();              // ���콺 ��

            // --- 1) ȸ�� ---
            if (rotatePressed && lookDelta.sqrMagnitude > 0.0001f)
            {
                RotateAroundPivot(lookDelta, Time.deltaTime);
            }

            // --- 2) �� ---
            if (panPressed && lookDelta.sqrMagnitude > 0.0001f)
            {
                PanCamera(lookDelta, Time.deltaTime);
            }

            // --- 3) �� ---
            if (Mathf.Abs(zoomValue) > 0.0001f)
            {
                ZoomCamera(zoomValue, Time.deltaTime);
            }
        }

        private void RotateAroundPivot(Vector2 delta, float dt)
        {
            var yaw = delta.x * cameraRotateSpeed * rotationSpeedRange * dt;
            var pitch = -delta.y * cameraRotateSpeed * rotationSpeedRange * dt; // ���� �̵��� ��(-)

            // 1) yaw : pivot ���� ���� Up
            transform.RotateAround(pivot.position, Vector3.up, yaw);

            // 2) pitch : ī�޶� ���� Right
            transform.RotateAround(pivot.position, transform.right, pitch);

            // 3) �Ÿ� ���� & pivot �ٶ󺸱�
            var direction = (transform.position - pivot.position).normalized;
            transform.position = pivot.position + direction * _currentDistance;
            transform.LookAt(pivot);
        }

        private void PanCamera(Vector2 delta, float dt)
        {
            var rightMovement = transform.right * (delta.x * panSpeed * panSpeedRange * dt);
            var upMovement = transform.up * (delta.y * panSpeed * panSpeedRange * dt);
            var panMovement = rightMovement + upMovement;

            transform.position += panMovement;
            pivot.position += panMovement;
        }

        private void ZoomCamera(float zoomValue, float dt)
        {
            _currentDistance -= zoomValue * zoomSpeed * zoomSpeedRange * dt;
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
