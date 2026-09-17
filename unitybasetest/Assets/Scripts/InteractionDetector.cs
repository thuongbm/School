using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Phát hiện IInteractable gần nhất bằng OverlapSphere.
/// Hiển thị UI icon nổi đúng vị trí màn hình của vật thể.
/// Bấm LMB (chuột trái) vào icon hoặc nhấn E để tương tác.
/// </summary>
public class InteractionDetector : MonoBehaviour
{
    [Header("Detection")]
    public float     detectionRadius = 2.5f;
    public LayerMask interactLayer   = ~0;

    [Header("Icon offset (Canvas pixels)")]
    public Vector2 promptOffset = new Vector2(0f, 45f);

    [Header("Colors")]
    public Color colorReady    = new Color(1f, 0.85f, 0.05f, 1f);
    public Color colorDisabled = new Color(0.4f, 0.4f, 0.4f, 0.6f);
    public Color bgNormal      = new Color(0.07f, 0.07f, 0.10f, 0.88f);

    // ── Private runtime ──────────────────────────────────────
    Canvas        _canvas;
    RectTransform _canvasRect;
    RectTransform _promptRect;
    Image         _promptBg;
    Button        _promptBtn;
    Image         _keyBadgeBg;
    Text          _labelText;

    IInteractable   _current;
    Collider        _currentCol;
    Transform       _currentTr;
    CharacterAnimator _anim;

    // ── Awake: gọi 1 lần khi Play bắt đầu ──────────────────
    void Awake()
    {
        _anim = GetComponent<CharacterAnimator>();

        // Dọn sạch MỌI canvas cũ từ lần trước (Editor mode)
        foreach (var old in FindObjectsOfType<Canvas>())
            if (old.name == "InteractCanvas")
                Destroy(old.gameObject);

        // Dọn EventSystem cũ
        foreach (var es in FindObjectsOfType<EventSystem>())
            Destroy(es.gameObject);

        // Tạo EventSystem mới sạch
        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<EventSystem>();
        esGO.AddComponent<StandaloneInputModule>();

        // Xây dựng UI mới hoàn toàn
        BuildUI();

        _promptRect.gameObject.SetActive(false);
    }

    // ── Xây UI ──────────────────────────────────────────────
    void BuildUI()
    {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Canvas – ScreenSpaceOverlay
        var cGO = new GameObject("InteractCanvas");
        _canvas = cGO.AddComponent<Canvas>();
        _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 60;
        var scaler = cGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight  = 0.5f;
        cGO.AddComponent<GraphicRaycaster>();   // BẮT BUỘC để nhận click
        _canvasRect = cGO.GetComponent<RectTransform>();

        // Prompt container
        var pGO = new GameObject("InteractPrompt");
        pGO.transform.SetParent(cGO.transform, false);
        _promptRect = pGO.AddComponent<RectTransform>();   // AddComponent, không GetComponent
        _promptRect.sizeDelta = new Vector2(240f, 60f);
        _promptRect.anchorMin = _promptRect.anchorMax = new Vector2(0.5f, 0.5f);
        _promptRect.pivot     = new Vector2(0.5f, 0.5f);

        _promptBg = pGO.AddComponent<Image>();
        _promptBg.color = bgNormal;

        _promptBtn = pGO.AddComponent<Button>();
        var bc = _promptBtn.colors;
        bc.normalColor      = Color.white;
        bc.highlightedColor = new Color(1.15f, 1.15f, 0.8f, 1f);
        bc.pressedColor     = new Color(0.75f, 0.75f, 0.5f, 1f);
        _promptBtn.colors        = bc;
        _promptBtn.targetGraphic = _promptBg;
        _promptBtn.onClick.AddListener(OnButtonClicked);   // runtime listener

        // Badge [E]
        var kbGO = new GameObject("KeyBadge");
        kbGO.transform.SetParent(pGO.transform, false);
        _keyBadgeBg = kbGO.AddComponent<Image>();
        _keyBadgeBg.color = colorReady;
        var kbR = kbGO.GetComponent<RectTransform>();
        kbR.anchorMin       = new Vector2(0f, 0.5f);
        kbR.anchorMax       = new Vector2(0f, 0.5f);
        kbR.pivot           = new Vector2(0f, 0.5f);
        kbR.anchoredPosition = new Vector2(8f, 0f);
        kbR.sizeDelta        = new Vector2(44f, 44f);

        var ktGO = new GameObject("KeyText");
        ktGO.transform.SetParent(kbGO.transform, false);
        var kt = ktGO.AddComponent<Text>();
        kt.text = "E"; kt.font = font; kt.fontSize = 26;
        kt.fontStyle = FontStyle.Bold; kt.color = Color.black;
        kt.alignment = TextAnchor.MiddleCenter;
        var ktR = ktGO.GetComponent<RectTransform>();
        ktR.anchorMin = Vector2.zero; ktR.anchorMax = Vector2.one; ktR.sizeDelta = Vector2.zero;

        // Label
        var lblGO = new GameObject("Label");
        lblGO.transform.SetParent(pGO.transform, false);
        _labelText = lblGO.AddComponent<Text>();
        _labelText.font = font; _labelText.fontSize = 18;
        _labelText.fontStyle = FontStyle.Bold;
        _labelText.color = Color.white;
        _labelText.alignment = TextAnchor.MiddleLeft;
        var lblR = lblGO.GetComponent<RectTransform>();
        lblR.anchorMin = Vector2.zero; lblR.anchorMax = Vector2.one;
        lblR.offsetMin = new Vector2(58f, 4f); lblR.offsetMax = new Vector2(-8f, -4f);
    }

    // ── Update ──────────────────────────────────────────────
    void Update()
    {
        if (_promptRect == null) BuildUI();

        DetectNearest();
        PositionPrompt();
        HandleKey();
    }

    // ── Phát hiện vật thể gần nhất ─────────────────────────
    void DetectNearest()
    {
        var hits = Physics.OverlapSphere(transform.position, detectionRadius, interactLayer);

        IInteractable best    = null;
        Collider      bestCol = null;
        Transform     bestTr  = null;
        float         minDist = float.MaxValue;

        foreach (var col in hits)
        {
            if (col.transform.root == transform.root) continue;   // bỏ qua Player

            var ia = col.GetComponent<IInteractable>()
                  ?? col.GetComponentInParent<IInteractable>();
            if (ia == null) continue;

            float d = (col.ClosestPoint(transform.position) - transform.position).magnitude;
            if (d < minDist) { minDist = d; best = ia; bestCol = col; bestTr = ((MonoBehaviour)ia).transform; }
        }

        _current    = best;
        _currentCol = bestCol;
        _currentTr  = bestTr;

        bool show = _current != null;
        if (_promptRect != null && _promptRect.gameObject.activeSelf != show)
            _promptRect.gameObject.SetActive(show);

        if (show)
        {
            if (_labelText != null) _labelText.text = _current.InteractLabel;
            if (_promptBtn != null) _promptBtn.interactable = _current.CanInteract;
            if (_keyBadgeBg != null) _keyBadgeBg.color = _current.CanInteract ? colorReady : colorDisabled;
        }
    }

    // ── Đặt vị trí prompt theo vật thể ─────────────────────
    void PositionPrompt()
    {
        if (_current == null || _promptRect == null) return;

        var cam = Camera.main;
        if (cam == null || _canvasRect == null) return;

        Vector3 anchor;
        if (_currentCol != null)
        {
            var b = _currentCol.bounds;
            anchor = new Vector3(b.center.x, b.max.y + 0.3f, b.center.z);
        }
        else anchor = _currentTr.position + Vector3.up * 1.5f;

        var screenPt = cam.WorldToScreenPoint(anchor);
        if (screenPt.z <= 0f) { _promptRect.gameObject.SetActive(false); return; }

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRect, screenPt, null, out Vector2 local))
        {
            float bob = Mathf.Sin(Time.time * 3.5f) * 3f;
            var pos = local + promptOffset + new Vector2(0f, bob);

            // Kẹp trong biên màn hình
            var half  = _canvasRect.sizeDelta * 0.5f;
            var pHalf = _promptRect.sizeDelta  * 0.5f;
            pos.x = Mathf.Clamp(pos.x, -half.x + pHalf.x + 8f, half.x - pHalf.x - 8f);
            pos.y = Mathf.Clamp(pos.y, -half.y + pHalf.y + 8f, half.y - pHalf.y - 8f);
            _promptRect.anchoredPosition = pos;
        }

        // Pulse nhẹ
        float s = 1f + Mathf.Sin(Time.time * 4f) * 0.025f;
        _promptRect.localScale = new Vector3(s, s, 1f);
    }

    // ── Phím E ──────────────────────────────────────────────
    void HandleKey()
    {
        if (_current != null && _current.CanInteract && Input.GetKeyDown(KeyCode.E))
            DoInteract();
    }

    // ── Click LMB vào Button ────────────────────────────────
    void OnButtonClicked()
    {
        if (_current != null && _current.CanInteract)
            DoInteract();
    }

    // ── Thực hiện tương tác ─────────────────────────────────
    void DoInteract()
    {
        if (_current == null || !_current.CanInteract) return;
        _current.Interact();
        _anim?.TriggerInteract();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0.7f, 0.15f);
        Gizmos.DrawSphere(transform.position, detectionRadius);
        Gizmos.color = new Color(0f, 1f, 0.7f, 0.85f);
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}