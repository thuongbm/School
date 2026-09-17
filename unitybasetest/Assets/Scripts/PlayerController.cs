using UnityEngine;

/// <summary>
/// Controller di chuyển nhân vật bằng phím WASD.
/// Gắn vào GameObject chứa CharacterController.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Tốc độ di chuyển (units/giây)")]
    public float moveSpeed = 5f;

    [Tooltip("Tốc độ xoay nhân vật theo hướng di chuyển")]
    public float rotationSpeed = 720f;

    [Header("Jump")]
    [Tooltip("Chiều cao nhảy (units)")]
    public float jumpHeight = 1.5f;

    [Header("Gravity")]
    [Tooltip("Lực trọng trọng (âm)")]
    public float gravity = -20f;

    private CharacterController _controller;
    private FollowCamera        _cam;
    private Vector3 _velocity;
    private bool _isDead = false;

    /// <summary>Để CharacterAnimator kiểm tra trạng thái chết.</summary>
    public bool IsDead => _isDead;

    void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _cam = Camera.main?.GetComponent<FollowCamera>();
    }

    void Update()
    {
        if (_isDead) return;

        // --- Input WASD ---
        float horizontal = Input.GetAxisRaw("Horizontal"); // A / D
        float vertical   = Input.GetAxisRaw("Vertical");   // W / S

        // --- Hướng di chuyển theo camera ---
        Vector3 camFwd   = _cam != null ? _cam.CameraForwardFlat : Vector3.forward;
        Vector3 camRight = _cam != null ? _cam.CameraRightFlat   : Vector3.right;

        Vector3 moveDir = (camFwd * vertical + camRight * horizontal).normalized;

        // --- Di chuyển + xoay mặt ---
        if (moveDir.magnitude >= 0.01f)
        {
            // Nhân vật xoay mặt mượt theo hướng đang đi
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, targetRot, rotationSpeed * Time.deltaTime);

            _controller.Move(moveDir * moveSpeed * Time.deltaTime);
        }

        // --- Nhảy (Space) ---
        if (_controller.isGrounded && Input.GetButtonDown("Jump"))
        {
            // v = sqrt(2 * |gravity| * jumpHeight)
            _velocity.y = Mathf.Sqrt(2f * Mathf.Abs(gravity) * jumpHeight);
        }

        // --- Trọng lực ---
        if (_controller.isGrounded && _velocity.y < 0f)
            _velocity.y = -2f;

        _velocity.y += gravity * Time.deltaTime;
        _controller.Move(_velocity * Time.deltaTime);
    }

    /// <summary>Khoá mọi input và kích hoạt animation chết.</summary>
    public void Die()
    {
        if (_isDead) return;
        _isDead = true;
        _velocity = Vector3.zero;
        GetComponent<CharacterAnimator>()?.TriggerDeath();
    }
}

