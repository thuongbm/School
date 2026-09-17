using UnityEngine;

/// <summary>
/// Third-person orbit camera.
/// - Giữ chuột PHẢI (RMB) để xoay quanh nhân vật
/// - Scroll wheel để zoom in/out
/// - Cursor luôn hiển thị để click UI
/// - Nhân vật di chuyển theo hướng camera nhìn
/// </summary>
public class FollowCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    public Vector3 pivotOffset = new Vector3(0f, 1.4f, 0f);

    [Header("Distance")]
    public float distance    = 5f;
    public float minDistance = 1.5f;
    public float maxDistance = 12f;
    public float zoomSpeed   = 4f;

    [Header("Rotation (giữ RMB)")]
    public float sensitivityX = 200f;
    public float sensitivityY = 150f;
    public float minPitch = -20f;
    public float maxPitch =  60f;

    [Header("Smoothing")]
    public float posSmooth = 12f;
    public float rotSmooth = 12f;

    [Header("Collision")]
    public LayerMask collisionMask = ~0;
    public float collisionRadius = 0.2f;

    // ── State ──
    float _yaw;
    float _pitch;
    float _currentDist;
    Vector3    _smoothPos;
    Quaternion _smoothRot;

    // ── Init ──
    void Start()
    {
        _yaw   = transform.eulerAngles.y;
        _pitch = transform.eulerAngles.x;
        _currentDist = distance;
        _smoothPos = transform.position;
        _smoothRot = transform.rotation;

        // Cursor luôn hiển thị để click được UI icon
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // ── Xoay: CHỈ khi giữ RMB ──
        if (Input.GetMouseButton(1))
        {
            _yaw   += Input.GetAxis("Mouse X") * sensitivityX * Time.deltaTime;
            _pitch -= Input.GetAxis("Mouse Y") * sensitivityY * Time.deltaTime;
            _pitch  = Mathf.Clamp(_pitch, minPitch, maxPitch);
        }

        // ── Zoom ──
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        _currentDist -= scroll * zoomSpeed;
        _currentDist  = Mathf.Clamp(_currentDist, minDistance, maxDistance);

        // ── Vị trí camera ──
        Quaternion targetRot = Quaternion.Euler(_pitch, _yaw, 0f);
        Vector3    pivot     = target.position + pivotOffset;

        float   safeDist = GetSafeDistance(pivot, targetRot, _currentDist);
        Vector3 finalPos = pivot - targetRot * Vector3.forward * safeDist;

        // ── Smooth ──
        _smoothPos = Vector3.Lerp(_smoothPos, finalPos, posSmooth * Time.deltaTime);
        _smoothRot = Quaternion.Slerp(_smoothRot, targetRot, rotSmooth * Time.deltaTime);

        transform.position = _smoothPos;
        transform.rotation = _smoothRot;
    }

    float GetSafeDistance(Vector3 pivot, Quaternion rot, float wantedDist)
    {
        Vector3 dir = -(rot * Vector3.forward);
        if (Physics.SphereCast(pivot, collisionRadius, dir, out RaycastHit hit,
                               wantedDist, collisionMask, QueryTriggerInteraction.Ignore))
            return Mathf.Max(hit.distance - collisionRadius, minDistance);
        return wantedDist;
    }

    public Vector3 CameraForwardFlat =>
        Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;

    public Vector3 CameraRightFlat =>
        Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
}
