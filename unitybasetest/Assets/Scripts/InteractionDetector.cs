using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gắn vào Player. Tự detect IInteractable gần nhất bằng OverlapSphere,
/// hiển thị world-space UI icon + label "[E] Tên vật thể" nổi trên đầu object,
/// và gọi Interact() + animation khi nhấn E.
/// </summary>
public class InteractionDetector : MonoBehaviour
{
    [Header("Detection")]
    public float detectionRadius = 2.5f;
    public LayerMask interactLayer = ~0;

    [Header("UI References (tự tạo nếu để trống)")]
    public Canvas       promptCanvas;   // World-space canvas
    public Image        iconImage;      // Icon [E]
    public Text         labelText;      // Tên vật thể

    [Header("Prompt Offset")]
    public Vector3 promptOffset = new Vector3(0f, 2.2f, 0f);

    [Header("Icon Colors")]
    public Color readyColor    = new Color(1f, 0.92f, 0.016f, 1f); // Vàng
    public Color disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);

    // ── State ──────────────────────────────────────
    IInteractable       _current;
    Transform           _currentTransform;
    CharacterAnimator   _anim;
    Camera              _mainCam;
    RectTransform       _canvasRect;

    // ═══════════════════════════════════════════════
    void Awake()
    {
        _anim    = GetComponent<CharacterAnimator>();
        _mainCam = Camera.main;

        if (promptCanvas == null) BuildPromptUI();
        promptCanvas.gameObject.SetActive(false);
    }

    // ── Build UI nếu chưa có ───────────────────────
    void BuildPromptUI()
    {
        // World-space canvas bám theo object, nhìn về camera
        var canvasGO = new GameObject("InteractPrompt");
        canvasGO.transform.SetParent(null); // root, sẽ move mỗi frame
        promptCanvas = canvasGO.AddComponent<Canvas>();
        promptCanvas.renderMode  = RenderMode.WorldSpace;
        promptCanvas.sortingOrder = 20;
        _canvasRect = canvasGO.GetComponent<RectTransform>();
        _canvasRect.sizeDelta = new Vector2(220f, 70f);
        _canvasRect.localScale = Vector3.one * 0.008f; // scale nhỏ cho world space

        // Nền mờ
        var bgGO  = new GameObject("BG");
        bgGO.transform.SetParent(canvasGO.transform, false);
        var bg    = bgGO.AddComponent<Image>();
        bg.color  = new Color(0f, 0f, 0f, 0.65f);
        var bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero; bgRect.offsetMax = Vector2.zero;

        // Icon [E]
        var keyGO  = new GameObject("KeyIcon");
        keyGO.transform.SetParent(canvasGO.transform, false);
        iconImage  = keyGO.AddComponent<Image>();
        iconImage.color = readyColor;
        var keyRect = keyGO.GetComponent<RectTransform>();
        keyRect.anchorMin = new Vector2(0f, 0f);
        keyRect.anchorMax = new Vector2(0f, 1f);
        keyRect.pivot     = new Vector2(0f, 0.5f);
        keyRect.offsetMin = new Vector2(8f,  8f);
        keyRect.offsetMax = new Vector2(68f, -8f);

        // Text "[E]" trên icon
        var keyTxtGO = new GameObject("KeyText");
        keyTxtGO.transform.SetParent(keyGO.transform, false);
        var keyTxt      = keyTxtGO.AddComponent<Text>();
        keyTxt.text     = "E";
        keyTxt.fontSize  = 32;
        keyTxt.fontStyle = FontStyle.Bold;
        keyTxt.color     = Color.black;
        keyTxt.alignment = TextAnchor.MiddleCenter;
        keyTxt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var ktRect = keyTxtGO.GetComponent<RectTransform>();
        ktRect.anchorMin = Vector2.zero; ktRect.anchorMax = Vector2.one;
        ktRect.offsetMin = Vector2.zero; ktRect.offsetMax = Vector2.zero;

        // Label tên vật thể
        var lblGO  = new GameObject("Label");
        lblGO.transform.SetParent(canvasGO.transform, false);
        labelText  = lblGO.AddComponent<Text>();
        labelText.text      = "";
        labelText.fontSize   = 20;
        labelText.color      = Color.white;
        labelText.alignment  = TextAnchor.MiddleLeft;
        labelText.font       = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var lblRect = lblGO.GetComponent<RectTransform>();
        lblRect.anchorMin = new Vector2(0f, 0f);
        lblRect.anchorMax = new Vector2(1f, 1f);
        lblRect.offsetMin = new Vector2(76f, 6f);
        lblRect.offsetMax = new Vector2(-8f, -6f);
    }

    // ═══════════════════════════════════════════════
    void Update()
    {
        DetectInteractable();
        UpdatePromptUI();
        HandleInput();
    }

    // ── Detect nearest IInteractable ───────────────
    void DetectInteractable()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, interactLayer);

        IInteractable   best     = null;
        Transform       bestTr   = null;
        float           bestDist = float.MaxValue;

        foreach (var col in hits)
        {
            var interactable = col.GetComponent<IInteractable>();
            if (interactable == null)
                interactable = col.GetComponentInParent<IInteractable>();
            if (interactable == null) continue;

            float dist = Vector3.Distance(transform.position, col.transform.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                best     = interactable;
                bestTr   = col.transform;
            }
        }

        _current          = best;
        _currentTransform = bestTr;
    }

    // ── Update UI vị trí + nội dung ───────────────
    void UpdatePromptUI()
    {
        if (_current == null)
        {
            promptCanvas.gameObject.SetActive(false);
            return;
        }

        promptCanvas.gameObject.SetActive(true);

        // Vị trí: nổi trên đầu vật thể, billboard quay về camera
        Vector3 worldPos = _currentTransform.position + promptOffset;
        promptCanvas.transform.position = worldPos;

        // Billboard: luôn nhìn về camera
        if (_mainCam != null)
            promptCanvas.transform.LookAt(
                worldPos + _mainCam.transform.rotation * Vector3.forward,
                _mainCam.transform.rotation * Vector3.up);

        // Nội dung
        labelText.text  = _current.InteractLabel;
        iconImage.color = _current.CanInteract ? readyColor : disabledColor;

        // Pulse nhẹ khi available
        float pulse = 1f + Mathf.Sin(Time.time * 4f) * 0.06f;
        promptCanvas.transform.localScale = Vector3.one * 0.008f * pulse;
    }

    // ── Input ──────────────────────────────────────
    void HandleInput()
    {
        if (_current == null) return;
        if (!Input.GetKeyDown(KeyCode.E)) return;
        if (!_current.CanInteract) return;

        _current.Interact();

        // Trigger animation Interact
        _anim?.TriggerInteract();
    }

    // Visualize detection radius trong Editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
        Gizmos.DrawSphere(transform.position, detectionRadius);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
