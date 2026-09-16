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
    private Vector3 _velocity;

    void Awake()
    {
        _controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        // --- Input WASD ---
        float horizontal = Input.GetAxisRaw("Horizontal"); // A / D
        float vertical   = Input.GetAxisRaw("Vertical");   // W / S

        Vector3 inputDir = new Vector3(horizontal, 0f, vertical).normalized;

        // --- Di chuyển ---
        if (inputDir.magnitude >= 0.01f)
        {
            // Xoay nhân vật mượt theo hướng di chuyển
            float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg;
            float angle = Mathf.MoveTowardsAngle(
                transform.eulerAngles.y,
                targetAngle,
                rotationSpeed * Time.deltaTime
            );
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            _controller.Move(inputDir * moveSpeed * Time.deltaTime);
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
}

