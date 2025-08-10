using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GameSystem
{
    public class BdEngineStyleCameraMovement : BaseManager
    {
        [Header("References")]
        public Transform pivot;

        [Header("Camera Movement Settings")]
        public bool enableCameraMovement = true;
        
        // 개선: 직관적인 감도(Sensitivity) 변수로 변경
        [Space]
        [Tooltip("카메라 회전 감도")]
        public float rotateSensitivity = 1.0f;
        [Tooltip("카메라 이동(Pan) 감도")]
        public float panSensitivity = 1.0f;
        [Tooltip("카메라 줌 감도")]
        public float zoomSensitivity = 1.0f;

        [Space]
        [Tooltip("카메라 상하 회전 최소/최대 각도")]
        public Vector2 pitchClamp = new Vector2(-89f, 89f);
        [Tooltip("카메라와 피봇 사이의 최소/최대 거리")]
        public Vector2 distanceClamp = new Vector2(2f, 50f);

        // 카메라의 현재 상태를 저장하는 변수들
        private float _currentDistance;
        private float _yaw;   // 수평 회전 (Y축 기준)
        private float _pitch; // 수직 회전 (X축 기준)

        // 초기 상태 저장을 위한 변수들
        private Vector3 _initialPivotPosition;
        private float _initialDistance;
        private float _initialYaw;
        private float _initialPitch;

        [Header("Input Actions")]
        public InputActionAsset inputActions;
        private InputActionMap _cameraMap;
        private InputAction _rotateAction;
        private InputAction _panAction;
        private InputAction _lookDeltaAction;
        private InputAction _zoomAction;

        private void OnEnable()
        {
            if (pivot == null)
            {
                Debug.LogError("Pivot is not assigned.");
                enabled = false;
                return;
            }

            // --- 초기 상태 저장 ---
            _currentDistance = Vector3.Distance(transform.position, pivot.position);
            Vector3 initialAngles = transform.eulerAngles;
            _yaw = initialAngles.y;
            _pitch = initialAngles.x;

            _initialPivotPosition = pivot.position;
            _initialDistance = _currentDistance;
            _initialYaw = _yaw;
            _initialPitch = _pitch;

            // --- Input System 설정 ---
            _cameraMap = inputActions.FindActionMap("Camera", throwIfNotFound: true);
            _rotateAction = _cameraMap.FindAction("Rotate", throwIfNotFound: true);
            _panAction = _cameraMap.FindAction("Pan", throwIfNotFound: true);
            _lookDeltaAction = _cameraMap.FindAction("LookDelta", throwIfNotFound: true);
            _zoomAction = _cameraMap.FindAction("Zoom", throwIfNotFound: true);
            _cameraMap.Enable();
        }

        private void OnDisable()
        {
            _cameraMap?.Disable();
        }

        private void LateUpdate() // 카메라 움직임은 LateUpdate에서 처리하는 것이 좋습니다.
        {
            if (!enableCameraMovement) return;

            var rotatePressed = _rotateAction.IsPressed();
            var panPressed = _panAction.IsPressed();
            var lookDelta = _lookDeltaAction.ReadValue<Vector2>();
            var zoomValue = _zoomAction.ReadValue<float>();

            if (rotatePressed && lookDelta.sqrMagnitude > 0.01f)
            {
                RotateCamera(lookDelta);
            }
            else if (panPressed && lookDelta.sqrMagnitude > 0.01f)
            {
                PanCamera(lookDelta);
            }

            if (Mathf.Abs(zoomValue) > 0.01f)
            {
                ZoomCamera(zoomValue);
            }
        }

        private void RotateCamera(Vector2 delta)
        {
            // 개선: 오일러 각도를 직접 제어하여 안정성 확보
            float yawDelta = delta.x * rotateSensitivity * 0.1f;
            float pitchDelta = delta.y * rotateSensitivity * 0.1f;

            _yaw += yawDelta;
            _pitch -= pitchDelta; // 마우스 상하 이동은 반대 방향

            // 개선: Pitch 각도를 제한하여 카메라가 뒤집히는 것을 방지
            _pitch = Mathf.Clamp(_pitch, pitchClamp.x, pitchClamp.y);

            UpdateCameraTransform();
        }

        private void PanCamera(Vector2 delta)
        {
            // 개선: 감도 계산 단순화 및 거리 비례 이동
            // 카메라가 멀리 있을수록 더 빨리 움직여 자연스러운 느낌을 줍니다.
            float panFactor = _currentDistance * 0.001f;
            Vector3 right = transform.right * -delta.x * panSensitivity * panFactor;
            Vector3 up = transform.up * -delta.y * panSensitivity * panFactor;

            Vector3 movement = right + up;
            transform.position += movement;
            pivot.position += movement;
        }

        private void ZoomCamera(float zoomValue)
        {
            // 개선: 감도 계산 단순화
            _currentDistance -= zoomValue * zoomSensitivity * 0.1f;
            _currentDistance = Mathf.Clamp(_currentDistance, distanceClamp.x, distanceClamp.y);

            UpdateCameraTransform();
        }

        /// <summary>
        /// 현재 _yaw, _pitch, _currentDistance 값을 기반으로 카메라의 위치와 회전을 업데이트합니다.
        /// </summary>
        private void UpdateCameraTransform()
        {
            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0);
            Vector3 position = pivot.position - (rotation * Vector3.forward * _currentDistance);

            transform.SetPositionAndRotation(position, rotation);
        }

        public void ResetCamera()
        {
            pivot.position = _initialPivotPosition;
            _currentDistance = _initialDistance;
            _yaw = _initialYaw;
            _pitch = _initialPitch;

            UpdateCameraTransform();
        }
    }
}
