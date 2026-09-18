using UnityEngine;

/// <summary>
/// Nút bấm mở lối đi: các platform chỉ định sẽ thu gọn (scale 0, vô hình + không va chạm)
/// lúc bắt đầu game, và "mở ra" (phóng to dần về kích thước gốc) khi người chơi tương tác
/// với nút này, cho phép đi qua.
/// </summary>
public class PlatformButton : InteractableBase
{
    [Header("Nút bấm")]
    public string buttonName    = "Mở lối đi";
    public float  pressDownAmt  = 0.05f;

    [Header("Platform điều khiển")]
    [Tooltip("Các platform sẽ thu gọn lúc đầu, mở ra khi bấm nút này")]
    public Transform[] platforms;
    public float extendDuration = 0.6f;

    public override string InteractLabel => _isActivated ? "Đã mở" : $"[Mở] {buttonName}";
    public override bool   CanInteract   => !_isActivated;

    bool      _isActivated;
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

    public override void Interact()
    {
        if (_isActivated) return;
        _isActivated = true;

        // Hiệu ứng nút hụp xuống khi bấm
        transform.localPosition = _restPos - Vector3.up * pressDownAmt;

        StartCoroutine(AnimateLerp(extendDuration, p =>
        {
            for (int i = 0; i < platforms.Length; i++)
                if (platforms[i] != null)
                    platforms[i].localScale = Vector3.Lerp(Vector3.zero, _extendedScales[i], p);
        }));
    }
}
