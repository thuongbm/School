using UnityEngine;
using System.Collections;

/// <summary>
/// Cửa trượt (sliding door): khi tương tác, cửa trượt ngang (theo trục chỉ định trong local space)
/// một khoảng đúng bằng bề rộng của cửa/ống thông gió, để mở hoàn toàn lối đi
/// (không như cửa bản lề có thể vẫn choán chỗ trong không gian hẹp).
/// </summary>
public class SlidingDoorInteractable : MonoBehaviour, IInteractable
{
    [Header("Cửa")]
    public string doorName = "Cửa thông gió";

    [Header("Trượt")]
    [Tooltip("Hướng trượt, tính theo local space của chính object này (vd: Vector3.left = sang trái)")]
    public Vector3 slideDirectionLocal = Vector3.left;

    [Tooltip("Khoảng cách trượt (nên bằng đúng bề rộng cửa / ống thông gió)")]
    public float slideDistance = 2.66f;

    [Tooltip("Tốc độ trượt (1 / giây, giá trị càng lớn trượt càng nhanh)")]
    public float animSpeed = 2.5f;

    // ── IInteractable ──────────────────────────────
    public string InteractLabel => _isOpen ? $"[Đóng] {doorName}" : $"[Mở] {doorName}";
    public bool   CanInteract   => !_isAnimating;

    // ── State ──────────────────────────────────────
    bool    _isOpen      = false;
    bool    _isAnimating = false;
    Vector3 _closedLocalPos;
    Vector3 _openLocalPos;

    void Awake()
    {
        _closedLocalPos = transform.localPosition;
        _openLocalPos   = _closedLocalPos + slideDirectionLocal.normalized * slideDistance;
    }

    public void Interact()
    {
        if (_isAnimating) return;
        StopAllCoroutines();
        StartCoroutine(SlideRoutine(!_isOpen));
    }

    IEnumerator SlideRoutine(bool opening)
    {
        _isAnimating = true;
        Vector3 start  = transform.localPosition;
        Vector3 target = opening ? _openLocalPos : _closedLocalPos;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * animSpeed;
            transform.localPosition = Vector3.Lerp(start, target, Mathf.Clamp01(t));
            yield return null;
        }
        transform.localPosition = target;
        _isOpen = opening;
        _isAnimating = false;
    }
}
