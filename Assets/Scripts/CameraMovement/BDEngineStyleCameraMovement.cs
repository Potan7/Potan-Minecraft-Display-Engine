using UnityEngine;
using UnityEngine.InputSystem;

public class BDEngineStyleCameraMovement : MonoBehaviour
{
    public static bool CanMoveCamera { get; set; } = true;

    [Header("Camera Movement Settings")]
    public Transform pivot;               // The pivot point for camera rotation
    public float rotationSpeed = 5f;      // Rotation speed
    public float panSpeed = 5f;           // Pan (move) speed
    public float zoomSpeed = 10f;         // Zoom speed
    public float minDistance = 2f;        // Minimum distance from the pivot
    public float maxDistance = 50f;       // Maximum distance from the pivot

    private float currentDistance;        // Current distance from the pivot
    private Vector3 lastMousePosition;    // Last recorded mouse position

    private Vector3 pivotInitPos;         // Initial position of the pivot
    private float initDistance;           // Initial camera-pivot distance

    void Start()
    {

        if (pivot == null)
        {
            Debug.LogError("Pivot transform is not assigned.");
            enabled = false;
            return;
        }

        // Calculate the initial distance between the camera and the pivot
        currentDistance = Vector3.Distance(transform.position, pivot.position);
        pivotInitPos = pivot.position;
        initDistance = currentDistance;

        // Initialize the last mouse position
        lastMousePosition = Input.mousePosition;
    }

    void Update()
    {
        lastMousePosition = Input.mousePosition;

        if (pivot == null || !CanMoveCamera)
            return;

        HandleMouseInput();
    }

    public void HandleMouseInput()
    {
        // Left mouse button rotates the camera around the pivot
        if (Input.GetMouseButton(0))
        {
            RotateAroundPivot();
        }

        // Right mouse button pans the camera (moves both camera and pivot)
        if (Input.GetMouseButton(1))
        {
            PanCamera();
        }

        // Use the mouse scroll wheel to zoom in/out
        //ZoomCamera();
    }

    void RotateAroundPivot()
    {
        Vector3 mouseDelta = Input.mousePosition - lastMousePosition;
        if (mouseDelta.sqrMagnitude < 0.0001f)
            return; // No significant movement

        // Calculate yaw (horizontal) and pitch (vertical) angles
        float yaw = mouseDelta.x * rotationSpeed;
        float pitch = -mouseDelta.y * rotationSpeed;

        // Rotate the camera around the pivot using the global up vector for yaw
        transform.RotateAround(pivot.position, Vector3.up, yaw);
        // Rotate around the camera's right vector for pitch
        transform.RotateAround(pivot.position, transform.right, pitch);

        // Maintain the camera's distance from the pivot
        Vector3 direction = (transform.position - pivot.position).normalized;
        transform.position = pivot.position + direction * currentDistance;
        // Always look at the pivot
        transform.LookAt(pivot);
    }

    void PanCamera()
    {
        Vector3 mouseDelta = Input.mousePosition - lastMousePosition;
        if (mouseDelta.sqrMagnitude < 0.0001f)
            return; // No significant movement

        // Calculate pan movement; using Time.deltaTime helps smooth out the movement
        Vector3 rightMovement = transform.right * mouseDelta.x * panSpeed * Time.deltaTime;
        Vector3 upMovement = transform.up * mouseDelta.y * panSpeed * Time.deltaTime;
        Vector3 panMovement = rightMovement + upMovement;

        // Move both the camera and the pivot together
        transform.position += panMovement;
        pivot.position += panMovement;
    }

    public void ZoomCamera(InputAction.CallbackContext callback)
    {
        if (!CanMoveCamera)
            return;
        float scrollDelta = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Approximately(scrollDelta, 0f))
            return;

        // Adjust the current distance based on scroll input (do not multiply by deltaTime)
        currentDistance -= scrollDelta * zoomSpeed;
        currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);

        // Update camera position based on the new distance
        Vector3 direction = (transform.position - pivot.position).normalized;
        transform.position = pivot.position + direction * currentDistance;
    }

    public void ResetCamera()
    {
        // Reset pivot and distance to their initial values
        pivot.position = pivotInitPos;
        currentDistance = initDistance;
        transform.position = pivotInitPos + new Vector3(0, 0, -currentDistance);
        transform.LookAt(pivotInitPos);
    }
}
