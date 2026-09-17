using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Detect IInteractable gần nhất, hiển thị icon ở vị trí màn hình của vật thể.
/// Click chuột TRÁI vào icon để tương tác.
/// Phím E vẫn hoạt động như shortcut.
/// </summary>
public class InteractionDetector : MonoBehaviour
{
    [Header("Detection")]
    public float detectionRadius = 2.5f;
    public LayerMask interactLayer = ~0;

    [Header("UI (tự tạo nếu để trống)")]
    public RectTransform promptRect;  // Container icon trên Screen Space canvas
    public Image         iconBg;
    public Text          labelText;
    public Button        interactBtn;

    [Header("Prompt Offset (screen pixels)")]
    public Vector2 screenOffset = new Vector2(0f, 80f); // offset từ world pos lên trên

    [Header("Colors")]
    public Color readyColor    = new Color(1f, 0.85f, 0f, 1f);
    public Color hoverColor    = new Color(1f, 1f, 0.4f, 1f);
    public Color disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.7f);

    // ── Internal ──
    IInteractable     _current;
    Transform         _currentTransform;
    CharacterAnimator _anim;
    Camera            _mainCam;
    Canvas            _canvas;
    bool              _isHovering;

    // ═══════════════════════════════════════════════
    void Awake()
    {
        _anim    = GetComponent<CharacterAnimator>();
        _mainCam = Camera.main;

        if (promptRect == null) BuildScreenSpaceUI();

        SetPromptVisible(false);
        EnsureEventSystem();
    }

    // ── Đảm bảo có EventSystem để Button hoạt động ──
    void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<StandaloneInputModule>();
        }
    }

    // ── Build Screen Space UI ──────────────────────
    void BuildScreenSpaceUI()
    {
        // Tìm canvas Screen Space Overlay đã có, hoặc tạo mới
        Canvas existingCanvas = null;
        foreach (var c in FindObjectsOfType<Canvas>())
        {
            if (c.renderMode == RenderMode.ScreenSpaceOverlay && c.name != "MissionCanvas")
            { existingCanvas = c; break; }
        }

        if (existingCanvas == null)
        {
            var cGO = new GameObject("InteractCanvas");
            existingCanvas = cGO.AddComponent<Canvas>();
            existingCanvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            existingCanvas.sortingOrder = 50;
            var scaler = cGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode          = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution  = new Vector2(1920f, 1080f);
            cGO.AddComponent<GraphicRaycaster>();
        }
        _canvas = existingCanvas;

        // ── Container (prompt) ──
        var promptGO = new GameObject("InteractPrompt");
        promptGO.transform.SetParent(_canvas.transform, false);
        promptRect = promptGO.AddComponent<RectTransform>();
        promptRect.sizeDelta = new Vector2(200f, 58f);
        promptRect.anchorMin = promptRect.anchorMax = new Vector2(0.5f, 0f);
        promptRect.pivot     = new Vector2(0.5f, 0f);

        // Nền bo tròn với màu đậm
        iconBg = promptGO.AddComponent<Image>();
        iconBg.color = new Color(0f, 0f, 0f, 0.78f);

        // Button component để nhận click
        interactBtn = promptGO.AddComponent<Button>();
        var colors = interactBtn.colors;
        colors.normalColor      = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 0.7f, 1f);
        colors.pressedColor     = new Color(0.7f, 0.7f, 0.4f, 1f);
        interactBtn.colors      = colors;
        interactBtn.targetGraphic = iconBg;
        interactBtn.onClick.AddListener(OnIconClicked);

        // ── Key badge "[E]" ──
        var keyGO  = new GameObject("KeyBadge");
        keyGO.transform.SetParent(promptGO.transform, false);
        var keyImg = keyGO.AddComponent<Image>();
        keyImg.color = readyColor;
        var keyRect  = keyGO.GetComponent<RectTransform>();
        keyRect.anchorMin = new Vector2(0f, 0f);
        keyRect.anchorMax = new Vector2(0f, 1f);
        keyRect.pivot     = new Vector2(0f, 0.5f);
        keyRect.offsetMin = new Vector2(6f,  6f);
        keyRect.offsetMax = new Vector2(52f, -6f);

        var keyTxtGO = new GameObject("KeyText");
        keyTxtGO.transform.SetParent(keyGO.transform, false);
        var keyTxt  = keyTxtGO.AddComponent<Text>();
        keyTxt.text      = "E";
        keyTxt.fontSize  = 26;
        keyTxt.fontStyle = FontStyle.Bold;
        keyTxt.color     = Color.black;
        keyTxt.alignment = TextAnchor.MiddleCenter;
        keyTxt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var ktRect = keyTxtGO.GetComponent<RectTransform>();
        ktRect.anchorMin = Vector2.zero;
        ktRect.anchorMax = Vector2.one;
        ktRect.offsetMin = Vector2.zero;
        ktRect.offsetMax = Vector2.zero;

        // ── Label ──
        var lblGO  = new GameObject("Label");
        lblGO.transform.SetParent(promptGO.transform, false);
        labelText  = lblGO.AddComponent<Text>();
        labelText.fontSize  = 18;
        labelText.color     = Color.white;
        labelText.alignment = TextAnchor.MiddleLeft;
        labelText.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var lblRect = lblGO.GetComponent<RectTransform>();
        lblRect.anchorMin = new Vector2(0f, 0f);
        lblRect.anchorMax = new Vector2(1f, 1f);
        lblRect.offsetMin = new Vector2(58f, 4f);
        lblRect.offsetMax = new Vector2(-8f, -4f);
    }

    // ═══════════════════════════════════════════════
    void Update()
    {
        DetectNearest();
        UpdatePromptPosition();
        HandleKeyboard();
        AnimatePulse();
    }

    // ── Tìm IInteractable gần nhất ────────────────
    void DetectNearest()
    {
        var hits = Physics.OverlapSphere(transform.position, detectionRadius, interactLayer);

        IInteractable best    = null;
        Transform     bestTr  = null;
        float         bestD   = float.MaxValue;

        foreach (var col in hits)
        {
            var ia = col.GetComponent<IInteractable>()
                  ?? col.GetComponentInParent<IInteractable>();
            if (ia == null) continue;

            float d = Vector3.Distance(transform.position, col.transform.position);
            if (d < bestD) { bestD = d; best = ia; bestTr = col.transform; }
        }

        _current          = best;
        _currentTransform = bestTr;

        SetPromptVisible(_current != null);
        if (_current != null && labelText != null)
            labelText.text = _current.InteractLabel;
    }

    // ── Đặt prompt đúng vị trí màn hình ──────────
    void UpdatePromptPosition()
    {
        if (_current == null || promptRect == null || _mainCam == null) return;

        // World → screen
        Vector3 worldPos  = _currentTransform.position + Vector3.up * 1.5f;
        Vector3 screenPos = _mainCam.WorldToScreenPoint(worldPos);

        // Nếu sau lưng camera thì ẩn
        if (screenPos.z < 0f) { SetPromptVisible(false); return; }

        // Chuyển sang canvas local space
        promptRect.anchoredPosition = new Vector2(
            screenPos.x - Screen.width  * 0.5f,
            screenPos.y - Screen.height * 0f + screenOffset.y);

        // Màu theo trạng thái
        bool canDo = _current.CanInteract;
        if (iconBg != null)
            iconBg.color = canDo
                ? (_isHovering ? new Color(0.15f, 0.15f, 0f, 0.85f) : new Color(0f, 0f, 0f, 0.78f))
                : new Color(0.1f, 0.1f, 0.1f, 0.6f);

        if (interactBtn != null)
            interactBtn.interactable = canDo;
    }

    // ── Pulse animation ───────────────────────────
    void AnimatePulse()
    {
        if (promptRect == null || _current == null) return;
        float s = 1f + Mathf.Sin(Time.time * 3.5f) * 0.04f;
        promptRect.localScale = Vector3.one * s;
    }

    // ── Phím E shortcut ───────────────────────────
    void HandleKeyboard()
    {
        if (_current == null) return;
        if (Input.GetKeyDown(KeyCode.E)) DoInteract();
    }

    // ── Click button ──────────────────────────────
    void OnIconClicked()
    {
        if (_current == null || !_current.CanInteract) return;
        DoInteract();
    }

    void DoInteract()
    {
        if (_current == null || !_current.CanInteract) return;
        _current.Interact();
        _anim?.TriggerInteract();
    }

    void SetPromptVisible(bool show)
    {
        if (promptRect != null && promptRect.gameObject.activeSelf != show)
            promptRect.gameObject.SetActive(show);
    }

    // ── Gizmo ─────────────────────────────────────
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.2f);
        Gizmos.DrawSphere(transform.position, detectionRadius);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}