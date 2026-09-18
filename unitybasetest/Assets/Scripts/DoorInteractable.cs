using UnityEngine;

/// <summary>
/// Cửa có thể mở/đóng khi tương tác (xoay quanh bản lề).
/// Gắn vào GameObject cánh cửa. Có thể dùng cho bất kỳ cửa nào trong map.
/// </summary>
public class DoorInteractable : InteractableBase
{
    [Header("Settings")]
    public string doorName  = "Cửa";
    public float  openAngle = 90f;   // Góc mở (degrees)
    public float  animSpeed = 2.5f;  // Tốc độ mở/đóng

    [Header("Hinge Axis")]
    [Tooltip("Trục xoay của bản lề cửa (local space)")]
    public Vector3 hingeAxis = Vector3.up;

    public override string InteractLabel => _isOpen ? $"Đóng {doorName}" : $"Mở {doorName}";

    bool       _isOpen;
    float      _currentAngle;
    Quaternion _initialLocalRot;

    void Awake()
    {
        _initialLocalRot = transform.localRotation;
    }

    public override void Interact()
    {
        if (!CanInteract) return;

        float targetAngle = _isOpen ? 0f : openAngle;
        float startAngle  = _currentAngle;
        float duration    = Mathf.Abs(targetAngle - startAngle) / Mathf.Max(openAngle * animSpeed, 0.01f);
        _isOpen = !_isOpen;

        StartCoroutine(AnimateLerp(duration, p =>
        {
            _currentAngle = Mathf.Lerp(startAngle, targetAngle, p);
            transform.localRotation = _initialLocalRot * Quaternion.AngleAxis(_currentAngle, hingeAxis);
        }, smoothStep: true));
    }
}
