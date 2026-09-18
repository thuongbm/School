using UnityEngine;
using System.Collections;

/// <summary>
/// Lớp cơ sở dùng chung cho các vật thể tương tác (cửa, nút bấm, platform...).
/// Gộp phần khung sườn lặp lại của IInteractable: cờ "đang chạy animation",
/// CanInteract mặc định, và 1 coroutine nội suy (lerp) dùng chung cho hiệu ứng
/// mở/đóng/di chuyển — để các lớp con không phải viết lại vòng lặp thủ công.
/// </summary>
public abstract class InteractableBase : MonoBehaviour, IInteractable
{
    /// <summary>true trong lúc đang chạy animation (mở/đóng/di chuyển) — set/clear tự động bởi AnimateLerp.</summary>
    protected bool IsAnimating { get; private set; }

    public abstract string InteractLabel { get; }
    public virtual  bool   CanInteract => !IsAnimating;
    public abstract void   Interact();

    /// <summary>
    /// Coroutine nội suy dùng chung theo thời gian: chạy trong "duration" giây,
    /// gọi onStep(t) mỗi frame với t đi từ 0→1 (có thể easing bằng SmoothStep),
    /// rồi gọi onComplete khi xong. Tự set/clear IsAnimating.
    /// </summary>
    protected IEnumerator AnimateLerp(float duration, System.Action<float> onStep,
                                       System.Action onComplete = null, bool smoothStep = false)
    {
        IsAnimating = true;
        float t = 0f;
        duration = Mathf.Max(duration, 0.0001f);
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float p = Mathf.Clamp01(t);
            onStep(smoothStep ? Mathf.SmoothStep(0f, 1f, p) : p);
            yield return null;
        }
        onStep(1f);
        onComplete?.Invoke();
        IsAnimating = false;
    }
}
