using UnityEngine;
using System.Collections;

/// <summary>
/// Cửa có thể mở/đóng khi tương tác.
/// Gắn vào GameObject cánh cửa. Có thể dùng cho bất kỳ cửa nào trong map.
/// </summary>
public class DoorInteractable : MonoBehaviour, IInteractable
{
    [Header("Settings")]
    public string doorName    = "Cửa";
    public float  openAngle   = 90f;   // Góc mở (degrees)
    public float  animSpeed   = 2.5f;  // Tốc độ mở/đóng

    [Header("Hinge Axis")]
    [Tooltip("Trục xoay của bản lề cửa (local space)")]
    public Vector3 hingeAxis  = Vector3.up;

    // ── IInteractable ──────────────────────────────
    public string InteractLabel => _isOpen ? $"Đóng {doorName}" : $"Mở {doorName}";
    public bool   CanInteract   => !_isAnimating;

    // ── State ──────────────────────────────────────
    bool       _isOpen      = false;
    bool       _isAnimating = false;
    float      _currentAngle = 0f;
    Quaternion _initialLocalRot;

    void Awake()
    {
        _initialLocalRot = transform.localRotation;
    }

    public void Interact()
    {
        if (_isAnimating) return;
        StartCoroutine(AnimateDoor(_isOpen ? 0f : openAngle));
        _isOpen = !_isOpen;
    }

    IEnumerator AnimateDoor(float targetAngle)
    {
        _isAnimating = true;
        float startAngle = _currentAngle;
        float t = 0f;
        float duration = Mathf.Abs(targetAngle - startAngle) / Mathf.Max(openAngle * animSpeed, 0.01f);

        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(duration, 0.01f);
            _currentAngle = Mathf.Lerp(startAngle, targetAngle, Mathf.SmoothStep(0f, 1f, t));
            transform.localRotation = _initialLocalRot * Quaternion.AngleAxis(_currentAngle, hingeAxis);
            yield return null;
        }

        _currentAngle = targetAngle;
        transform.localRotation = _initialLocalRot * Quaternion.AngleAxis(_currentAngle, hingeAxis);
        _isAnimating = false;
    }
}

