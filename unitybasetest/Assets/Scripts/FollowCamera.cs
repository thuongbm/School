using UnityEngine;

/// <summary>
/// Third-person orbit camera.
/// - Kéo chuột phải / giữ RMB để xoay quanh nhân vật (pitch + yaw)
/// - Scroll wheel để zoom in/out
/// - Nhân vật luôn di chuyển theo hướng camera nhìn
/// Gắn vào Main Camera, kéo Player vào Target.
/// </summary>
public class FollowCamera : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Transform của nhân vật cần theo dõi")]
    public Transform target;

    [Tooltip("Offset điểm nhìn tính từ gốc nhân vật (để nhìn vào ngực/đầu)")]
    public Vector3 pivotOffset = new Vector3(0f, 1.4f, 0f);

    [Header("Distance")]
    public float distance    = 5f;
    public float minDistance = 1.5f;
    public float maxDistance = 12f;
    public float zoomSpeed   = 4f;

    [Header("Rotation")]
    public float sensitivityX = 200f;
    public float sensitivityY = 150f;

    [Tooltip("Giới hạn góc nhìn trên/dưới")]
    public float minPitch = -20f;
    public float maxPitch =  60f;

    [Header("Smoothing")]
    public float posSmooth = 12f;
    public float rotSmooth = 12f;

    [Header("Collision")]
    [Tooltip("Tránh camera xuyên qua tường")]
    public LayerMask collisionMask = ~0;
    public float collisionRadius = 0.2f;

    // ── State ──────────────────────────────
    float _yaw;    // xoay ngang (quanh Y)
    float _pitch;  // xoay dọc  (quanh X)
    float _currentDist;

    Vector3    _smoothPos;
    Quaternion _smoothRot;

    // ── Init ───────────────────────────────
    void Start()
    {
        // Khởi tạo yaw/pitch từ góc camera hiện tại
        _yaw   = transform.eulerAngles.y;
        _pitch = transform.eulerAngles.x;
        _currentDist = distance;

        _smoothPos = transform.position;
        _smoothRot = transform.rotation;

        // Con trỏ luôn hiện để có thể click UI icon
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    // ── Input + logic ──────────────────────
    void LateUpdate()
    {
        if (target == null) return;

        // ── Chỉ xoay camera khi ĐANG giữ RMB ──
        bool rmb = Input.GetMouseButton(1);
        if (rmb)
        {
            float mouseX = Input.GetAxis("Mouse X") * sensitivityX * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * sensitivityY * Time.deltaTime;
            _yaw   += mouseX;
            _pitch -= mouseY;
            _pitch  = Mathf.Clamp(_pitch, minPitch, maxPitch);
        }

        // ── Zoom ──
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        _currentDist -= scroll * zoomSpeed;
        _currentDist  = Mathf.Clamp(_currentDist, minDistance, maxDistance);

        // ── Tính vị trí camera ──
        Quaternion targetRot = Quaternion.Euler(_pitch, _yaw, 0f);
        Vector3    pivot     = target.position + pivotOffset;

        // ── Camera collision ──
        float safeDist = GetSafeDistance(pivot, targetRot, _currentDist);
        Vector3 finalPos = pivot - targetRot * Vector3.forward * safeDist;

        // ── Smooth ──
        _smoothPos = Vector3.Lerp(_smoothPos, finalPos, posSmooth * Time.deltaTime);
        _smoothRot = Quaternion.Slerp(_smoothRot, targetRot, rotSmooth * Time.deltaTime);

        transform.position = _smoothPos;
        transform.rotation = _smoothRot;
    }

    // Tránh camera xuyên tường bằng Spherecast
    float GetSafeDistance(Vector3 pivot, Quaternion rot, float wantedDist)
    {
        Vector3 dir = -(rot * Vector3.forward);
        if (Physics.SphereCast(pivot, collisionRadius, dir, out RaycastHit hit,
                               wantedDist, collisionMask, QueryTriggerInteraction.Ignore))
        {
            return Mathf.Max(hit.distance - collisionRadius, minDistance);
        }
        return wantedDist;
    }



    /// <summary>
    /// Hướng nhìn phẳng của camera (dùng bởi PlayerController để di chuyển theo camera).
    /// </summary>
    public Vector3 CameraForwardFlat =>
        Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;

    public Vector3 CameraRightFlat =>
        Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
}
