using UnityEngine;
using System.Collections;

/// <summary>
/// Nút bấm mở lối đi: các platform chỉ định sẽ thu gọn (scale 0, vô hình + không va chạm)
/// lúc bắt đầu game, và "mở ra" (phóng to dần về kích thước gốc) khi người chơi tương tác
/// với nút này, cho phép đi qua.
/// </summary>
public class PlatformButton : MonoBehaviour, IInteractable
{
    [Header("Nút bấm")]
    public string buttonName    = "Mở lối đi";
    public float  pressDownAmt  = 0.05f;

    [Header("Platform điều khiển")]
    [Tooltip("Các platform sẽ thu gọn lúc đầu, mở ra khi bấm nút này")]
    public Transform[] platforms;
    public float extendDuration = 0.6f;

    // ── IInteractable ──────────────────────────────
    public string InteractLabel => _isActivated ? "Đã mở" : $"[Mở] {buttonName}";
    public bool   CanInteract   => !_isActivated;

    // ── State ──────────────────────────────────────
    bool      _isActivated = false;
    Vector3   _restPos;
    Vector3[] _extendedScales;

    void Awake()
    {
        _restPos = transform.localPosition;

        _extendedScales = new Vector3[platforms.Length];
        for (int i = 0; i < platforms.Length; i++)
        {
            if (platforms[i] == null) continue;
            _extendedScales[i] = platforms[i].localScale;
            platforms[i].localScale = Vector3.zero; // thu gọn lúc bắt đầu (ẩn + tắt va chạm)
        }
    }

    public void Interact()
    {
        if (_isActivated) return;
        _isActivated = true;

        // Hiệu ứng nút hụp xuống khi bấm
        transform.localPosition = _restPos - Vector3.up * pressDownAmt;

        StartCoroutine(ExtendRoutine());
    }

    IEnumerator ExtendRoutine()
    {
        float t = 0f;
        while (t < extendDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / extendDuration);
            for (int i = 0; i < platforms.Length; i++)
                if (platforms[i] != null)
                    platforms[i].localScale = Vector3.Lerp(Vector3.zero, _extendedScales[i], p);
            yield return null;
        }

        for (int i = 0; i < platforms.Length; i++)
            if (platforms[i] != null)
                platforms[i].localScale = _extendedScales[i];
    }
}
