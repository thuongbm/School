using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Nút bấm generic – nhấn E để kích hoạt.
/// Hỗ trợ: nhấn 1 lần (toggle) hoặc nhấn giữ, gắn UnityEvent để kết nối bất kỳ logic nào.
/// </summary>
public class ButtonInteractable : MonoBehaviour, IInteractable
{
    [Header("Settings")]
    public string buttonName   = "Nút bấm";
    public bool   isToggle     = true;   // true = bật/tắt, false = chỉ trigger 1 lần
    public float  pressDownAmt = 0.05f;  // Nút hụp xuống (units) khi bấm

    [Header("Events")]
    [Tooltip("Gọi khi nút được bấm / bật")]
    public UnityEvent onPressed = new UnityEvent();
    [Tooltip("Gọi khi nút được thả / tắt (chỉ với isToggle=true)")]
    public UnityEvent onReleased = new UnityEvent();

    // ── IInteractable ──────────────────────────────
    public string InteractLabel => _isActive ? $"[Tắt] {buttonName}" : $"[Bật] {buttonName}";
    public bool   CanInteract   => true;

    // ── State ──────────────────────────────────────
    bool    _isActive = false;
    Vector3 _restPos;

    void Awake() => _restPos = transform.localPosition;

    public void Interact()
    {
        if (isToggle)
        {
            _isActive = !_isActive;
            if (_isActive) { Press(); onPressed?.Invoke(); }
            else            { Release(); onReleased?.Invoke(); }
        }
        else
        {
            // Chỉ kích hoạt 1 lần
            Press();
            onPressed?.Invoke();
            Invoke(nameof(Release), 0.25f);
        }
    }

    void Press()   => transform.localPosition = _restPos - Vector3.up * pressDownAmt;
    void Release() => transform.localPosition = _restPos;
}

