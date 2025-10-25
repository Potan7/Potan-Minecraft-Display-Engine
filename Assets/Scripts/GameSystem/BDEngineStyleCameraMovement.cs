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
        
        [Space]
        [Tooltip("카메라 회전 감도")]
        public float rotateSensitivity = 1.0f;
        [Tooltip("카메라 이동(Pan) 감도")]
        public float panSensitivity = 1.0f;
        [Tooltip("카메라 줌 감도")]
        public float zoomSensitivity = 1.0f;

        [Space]
        [Header("Smoothing Settings")]
        [Tooltip("카메라 움직임 부드러움 정도 (0=즉시, 1=매우 부드럽게)")]
        [Range(0f, 0.95f)]
        public float smoothing = 0.8f;
        [Tooltip("관성 효과 적용 여부")]
        public bool enableInertia = true;
        [Tooltip("관성 감쇠 속도")]
        [Range(0.1f, 0.99f)]
        public float inertiaDamping = 0.9f;

        [Space]
        [Header("Zoom Settings")]
        [Tooltip("줌 시 피봇을 향해 이동")]
        public bool zoomToPivot = true;
        [Tooltip("줌 가속도")]
        public float zoomAcceleration = 1.5f;

        [Space]
        [Tooltip("카메라 상하 회전 최소/최대 각도")]
        public Vector2 pitchClamp = new Vector2(-89f, 89f);
        [Tooltip("카메라와 피봇 사이의 최소/최대 거리")]
        public Vector2 distanceClamp = new Vector2(2f, 50f);

        // 현재 상태
        private float _currentDistance;
        private float _yaw;
        private float _pitch;

        // 목표 상태 (부드러운 움직임용)
        private float _targetYaw;
        private float _targetPitch;
        private float _targetDistance;
        private Vector3 _targetPivotPosition;

        // 관성 변수
        private Vector2 _rotationVelocity;
        private Vector2 _panVelocity;
        private float _zoomVelocity;

        // 초기 상태
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
        private InputAction _focusAction;

        private void OnEnable()
        {
            if (pivot == null)
            {
                Debug.LogError("Pivot is not assigned.");
                enabled = false;
                return;
            }

            // 초기 상태 저장
            _currentDistance = Vector3.Distance(transform.position, pivot.position);
            Vector3 initialAngles = transform.eulerAngles;
            _yaw = _targetYaw = initialAngles.y;
            _pitch = _targetPitch = initialAngles.x;
            _targetDistance = _currentDistance;
            _targetPivotPosition = pivot.position;

            _initialPivotPosition = pivot.position;
            _initialDistance = _currentDistance;
            _initialYaw = _yaw;
            _initialPitch = _pitch;

            // Input System 설정
            _cameraMap = inputActions.FindActionMap("Camera", throwIfNotFound: true);
            _rotateAction = _cameraMap.FindAction("Rotate", throwIfNotFound: true);
            _panAction = _cameraMap.FindAction("Pan", throwIfNotFound: true);
            _lookDeltaAction = _cameraMap.FindAction("LookDelta", throwIfNotFound: true);
            _zoomAction = _cameraMap.FindAction("Zoom", throwIfNotFound: true);
            
            // 포커스 액션 (선택사항 - 없으면 null)
            _focusAction = _cameraMap.FindAction("Focus");
            if (_focusAction != null)
            {
                _focusAction.performed += OnFocusPerformed;
            }

            _cameraMap.Enable();
        }

        private void OnDisable()
        {
            if (_focusAction != null)
            {
                _focusAction.performed -= OnFocusPerformed;
            }
            _cameraMap?.Disable();
        }

        private void LateUpdate()
        {
            if (!enableCameraMovement) return;

            var rotatePressed = _rotateAction.IsPressed();
            var panPressed = _panAction.IsPressed();
            var lookDelta = _lookDeltaAction.ReadValue<Vector2>();
            var zoomValue = _zoomAction.ReadValue<float>();

            // 입력 처리
            if (rotatePressed && lookDelta.sqrMagnitude > 0.01f)
            {
                RotateCamera(lookDelta);
            }
            else if (panPressed && lookDelta.sqrMagnitude > 0.01f)
            {
                PanCamera(lookDelta);
            }
            else if (enableInertia)
            {
                // 관성 적용
                ApplyRotationInertia();
                ApplyPanInertia();
            }

            if (Mathf.Abs(zoomValue) > 0.01f)
            {
                ZoomCamera(zoomValue);
            }
            else if (enableInertia)
            {
                ApplyZoomInertia();
            }

            // 부드러운 움직임 적용
            SmoothUpdate();
        }

        private void RotateCamera(Vector2 delta)
        {
            Vector2 rotationDelta = new Vector2(
                delta.x * rotateSensitivity * 0.1f,
                -delta.y * rotateSensitivity * 0.1f
            );

            if (enableInertia)
            {
                _rotationVelocity = rotationDelta;
            }

            _targetYaw += rotationDelta.x;
            _targetPitch += rotationDelta.y;
            _targetPitch = Mathf.Clamp(_targetPitch, pitchClamp.x, pitchClamp.y);
        }

        private void ApplyRotationInertia()
        {
            if (_rotationVelocity.sqrMagnitude < 0.001f)
            {
                _rotationVelocity = Vector2.zero;
                return;
            }

            _targetYaw += _rotationVelocity.x;
            _targetPitch += _rotationVelocity.y;
            _targetPitch = Mathf.Clamp(_targetPitch, pitchClamp.x, pitchClamp.y);

            _rotationVelocity *= inertiaDamping;
        }

        private void PanCamera(Vector2 delta)
        {
            float panFactor = _currentDistance * 0.001f;
            Vector2 panDelta = new Vector2(
                -delta.x * panSensitivity * panFactor,
                -delta.y * panSensitivity * panFactor
            );

            if (enableInertia)
            {
                _panVelocity = panDelta;
            }

            Vector3 right = transform.right * panDelta.x;
            Vector3 up = transform.up * panDelta.y;
            _targetPivotPosition += right + up;
        }

        private void ApplyPanInertia()
        {
            if (_panVelocity.sqrMagnitude < 0.0001f)
            {
                _panVelocity = Vector2.zero;
                return;
            }

            Vector3 right = transform.right * _panVelocity.x;
            Vector3 up = transform.up * _panVelocity.y;
            _targetPivotPosition += right + up;

            _panVelocity *= inertiaDamping;
        }

        private void ZoomCamera(float zoomValue)
        {
            float zoomDelta = -zoomValue * zoomSensitivity * 0.1f * zoomAcceleration;

            if (enableInertia)
            {
                _zoomVelocity = zoomDelta;
            }

            if (zoomToPivot)
            {
                // 피봇을 향해 줌
                Vector3 toPivot = _targetPivotPosition - transform.position;
                float currentDist = toPivot.magnitude;
                _targetDistance = Mathf.Clamp(currentDist + zoomDelta, distanceClamp.x, distanceClamp.y);
            }
            else
            {
                _targetDistance = Mathf.Clamp(_targetDistance + zoomDelta, distanceClamp.x, distanceClamp.y);
            }
        }

        private void ApplyZoomInertia()
        {
            if (Mathf.Abs(_zoomVelocity) < 0.001f)
            {
                _zoomVelocity = 0f;
                return;
            }

            _targetDistance = Mathf.Clamp(_targetDistance + _zoomVelocity, distanceClamp.x, distanceClamp.y);
            _zoomVelocity *= inertiaDamping;
        }

        private void SmoothUpdate()
        {
            float t = 1f - Mathf.Pow(smoothing, Time.deltaTime * 60f); // 프레임레이트 독립적

            _yaw = Mathf.LerpAngle(_yaw, _targetYaw, t);
            _pitch = Mathf.LerpAngle(_pitch, _targetPitch, t);
            _currentDistance = Mathf.Lerp(_currentDistance, _targetDistance, t);
            pivot.position = Vector3.Lerp(pivot.position, _targetPivotPosition, t);

            UpdateCameraTransform();
        }

        private void UpdateCameraTransform()
        {
            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0);
            Vector3 position = pivot.position - (rotation * Vector3.forward * _currentDistance);

            transform.SetPositionAndRotation(position, rotation);
        }

        private void OnFocusPerformed(InputAction.CallbackContext context)
        {
            // 레이캐스트로 클릭한 오브젝트를 피봇으로 설정
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
            {
                FocusOnPoint(hit.point);
            }
        }

        public void FocusOnPoint(Vector3 point)
        {
            _targetPivotPosition = point;
        }

        public void ResetCamera()
        {
            _targetPivotPosition = pivot.position = _initialPivotPosition;
            _targetDistance = _currentDistance = _initialDistance;
            _targetYaw = _yaw = _initialYaw;
            _targetPitch = _pitch = _initialPitch;

            // 관성 초기화
            _rotationVelocity = Vector2.zero;
            _panVelocity = Vector2.zero;
            _zoomVelocity = 0f;

            UpdateCameraTransform();
        }
    }
}
