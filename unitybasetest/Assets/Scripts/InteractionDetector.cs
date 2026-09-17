using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Phát hiện các vật thể IInteractable (Cửa, Nút bấm, Ống thông gió...) khi nhân vật đứng gần.
/// Hiển thị UI Icon nổi trên đầu vật thể theo vị trí màn hình.
/// Người chơi có thể:
/// 1. Nhấn phím E
/// 2. Click chuột trái (LMB) vào UI Icon
/// 3. Click chuột trái (LMB) trực tiếp vào vật thể trong thế giới 3D
/// </summary>
public class InteractionDetector : MonoBehaviour
{
    [Header("Detection Settings")]
    [Tooltip("Khoảng cách phát hiện vật thể tương tác")]
    public float detectionRadius = 3.5f;

    [Tooltip("LayerMask cho vật thể tương tác")]
    public LayerMask interactLayer = ~0;

    [Header("UI References")]
    public Canvas        canvas;
    public RectTransform promptRect;
    public Image         promptBg;
    public Image         keyBadgeBg;
    public Text          keyText;
    public Text          labelText;
    public Button        interactButton;

    [Header("Screen Offset (Canvas Local Space)")]
    public Vector2 screenOffset = new Vector2(0f, 40f);

    [Header("UI Styling")]
    public Color readyColor       = new Color(1f, 0.85f, 0.05f, 1f);   // Vàng tươi
    public Color readyBgColor     = new Color(0.08f, 0.08f, 0.12f, 0.85f); // Đen ánh xanh
    public Color disabledBgColor  = new Color(0.15f, 0.15f, 0.15f, 0.6f);
    public Color hoverBgColor     = new Color(0.25f, 0.22f, 0.05f, 0.95f);

    // ── Internal State ──
    private IInteractable     _current;
    private Collider          _currentCollider;
    private Transform         _currentTransform;
    private CharacterAnimator _anim;
    private Camera            _mainCam;
    private RectTransform     _canvasRect;

    void Awake()
    {
        _anim = GetComponent<CharacterAnimator>();
        _mainCam = Camera.main;

        EnsureUI();
        EnsureEventSystem();
        SetPromptVisible(false);
    }

    void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<StandaloneInputModule>();
        }
    }

    void EnsureUI()
    {
        if (canvas == null)
        {
            var cGO = GameObject.Find("InteractCanvas");
            if (cGO == null)
            {
                cGO = new GameObject("InteractCanvas");
                canvas = cGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 50;

                var scaler = cGO.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;

                cGO.AddComponent<GraphicRaycaster>();
            }
            else
            {
                canvas = cGO.GetComponent<Canvas>();
            }
        }

        _canvasRect = canvas.GetComponent<RectTransform>();

        if (promptRect == null)
        {
            var pTr = canvas.transform.Find("InteractPrompt");
            if (pTr != null)
            {
                promptRect = pTr.GetComponent<RectTransform>();
            }
            else
            {
                BuildPromptHierarchy();
            }
        }

        // Lấy component references
        if (promptRect != null)
        {
            promptBg = promptRect.GetComponent<Image>();
            interactButton = promptRect.GetComponent<Button>();

            var keyBadge = promptRect.Find("KeyBadge");
            if (keyBadge != null)
            {
                keyBadgeBg = keyBadge.GetComponent<Image>();
                var kt = keyBadge.Find("KeyText");
                if (kt != null) keyText = kt.GetComponent<Text>();
            }

            var lbl = promptRect.Find("Label");
            if (lbl != null) labelText = lbl.GetComponent<Text>();

            if (interactButton != null)
            {
                interactButton.onClick.RemoveAllListeners();
                interactButton.onClick.AddListener(OnButtonClicked);
            }
        }
    }

    void BuildPromptHierarchy()
    {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Container
        var promptGO = new GameObject("InteractPrompt");
        promptGO.transform.SetParent(canvas.transform, false);
        promptRect = promptGO.AddComponent<RectTransform>();
        promptRect.sizeDelta = new Vector2(240f, 64f);
        promptRect.anchorMin = new Vector2(0.5f, 0.5f);
        promptRect.anchorMax = new Vector2(0.5f, 0.5f);
        promptRect.pivot     = new Vector2(0.5f, 0.5f);

        promptBg = promptGO.AddComponent<Image>();
        promptBg.color = readyBgColor;

        interactButton = promptGO.AddComponent<Button>();
        var btnColors = interactButton.colors;
        btnColors.normalColor      = Color.white;
        btnColors.highlightedColor = new Color(1.2f, 1.2f, 0.9f, 1f);
        btnColors.pressedColor     = new Color(0.8f, 0.8f, 0.6f, 1f);
        interactButton.colors = btnColors;
        interactButton.targetGraphic = promptBg;
        interactButton.onClick.AddListener(OnButtonClicked);

        // KeyBadge [E]
        var keyBadgeGO = new GameObject("KeyBadge");
        keyBadgeGO.transform.SetParent(promptGO.transform, false);
        keyBadgeBg = keyBadgeGO.AddComponent<Image>();
        keyBadgeBg.color = readyColor;
        var kbRect = keyBadgeGO.GetComponent<RectTransform>();
        kbRect.anchorMin = new Vector2(0f, 0.5f);
        kbRect.anchorMax = new Vector2(0f, 0.5f);
        kbRect.pivot     = new Vector2(0f, 0.5f);
        kbRect.anchoredPosition = new Vector2(10f, 0f);
        kbRect.sizeDelta = new Vector2(46f, 46f);

        var keyTextGO = new GameObject("KeyText");
        keyTextGO.transform.SetParent(keyBadgeGO.transform, false);
        keyText = keyTextGO.AddComponent<Text>();
        keyText.text = "E";
        keyText.font = font;
        keyText.fontSize = 28;
        keyText.fontStyle = FontStyle.Bold;
        keyText.alignment = TextAnchor.MiddleCenter;
        keyText.color = Color.black;
        var ktRect = keyTextGO.GetComponent<RectTransform>();
        ktRect.anchorMin = Vector2.zero;
        ktRect.anchorMax = Vector2.one;
        ktRect.sizeDelta = Vector2.zero;

        // Label
        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(promptGO.transform, false);
        labelText = labelGO.AddComponent<Text>();
        labelText.text = "Tương tác";
        labelText.font = font;
        labelText.fontSize = 20;
        labelText.fontStyle = FontStyle.Bold;
        labelText.alignment = TextAnchor.MiddleLeft;
        labelText.color = Color.white;
        var lblRect = labelGO.GetComponent<RectTransform>();
        lblRect.anchorMin = new Vector2(0f, 0f);
        lblRect.anchorMax = new Vector2(1f, 1f);
        lblRect.offsetMin = new Vector2(64f, 6f);
        lblRect.offsetMax = new Vector2(-10f, -6f);
    }

    void Update()
    {
        if (_mainCam == null) _mainCam = Camera.main;

        DetectNearestInteractable();
        UpdatePromptPositionAndVisuals();
        HandleDirectInteractionInput();
    }

    /// <summary>
    /// Tìm vật thể IInteractable gần nhất, tính theo ClosestPoint trên Collider
    /// </summary>
    void DetectNearestInteractable()
    {
        var hits = Physics.OverlapSphere(transform.position, detectionRadius, interactLayer);

        IInteractable best       = null;
        Collider      bestCol    = null;
        Transform     bestTr     = null;
        float         minDistSq  = float.MaxValue;
        Vector3       playerPos  = transform.position;

        foreach (var col in hits)
        {
            // Bỏ qua collider của chính Player
            if (col.transform.root == transform.root) continue;

            var ia = col.GetComponent<IInteractable>() ?? col.GetComponentInParent<IInteractable>();
            if (ia == null) continue;

            // Tính khoảng cách tới điểm gần nhất trên collider
            Vector3 closestPt = col.ClosestPoint(playerPos);
            float distSq = (closestPt - playerPos).sqrMagnitude;

            if (distSq < minDistSq)
            {
                minDistSq = distSq;
                best      = ia;
                bestCol   = col;
                bestTr    = ((MonoBehaviour)ia).transform;
            }
        }

        _current          = best;
        _currentCollider  = bestCol;
        _currentTransform = bestTr;

        bool hasTarget = _current != null;
        SetPromptVisible(hasTarget);

        if (hasTarget && labelText != null)
        {
            labelText.text = _current.InteractLabel;
        }
    }

    /// <summary>
    /// Đặt UI icon đúng vị trí 3D của vật thể trên màn hình
    /// </summary>
    void UpdatePromptPositionAndVisuals()
    {
        if (_mainCam == null) _mainCam = Camera.main;
        if (_canvasRect == null && canvas != null) _canvasRect = canvas.GetComponent<RectTransform>();
        if (_current == null || promptRect == null || _mainCam == null || _canvasRect == null)
            return;

        // Vị trí mốc 3D (trên đầu vật thể)
        Vector3 worldAnchor;
        if (_currentCollider != null)
        {
            var bounds = _currentCollider.bounds;
            worldAnchor = new Vector3(bounds.center.x, bounds.max.y + 0.35f, bounds.center.z);
        }
        else
        {
            worldAnchor = _currentTransform.position + Vector3.up * 1.5f;
        }

        // Chiếu sang screen point
        Vector3 screenPos = _mainCam.WorldToScreenPoint(worldAnchor);

        // Nếu vật thể ở sau lưng camera thì ẩn
        if (screenPos.z <= 0f)
        {
            SetPromptVisible(false);
            return;
        }

        SetPromptVisible(true);

        // Chuyển sang canvas local space chuẩn qua RectTransformUtility
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRect, screenPos, null, out Vector2 localPoint))
        {
            // Thêm offset + hiệu ứng nhấp nhô nhẹ (bobbing)
            float bob = Mathf.Sin(Time.time * 3.5f) * 4f;
            Vector2 targetPos = localPoint + screenOffset + new Vector2(0f, bob);

            // Giới hạn để không bị lọt ra ngoài mép màn hình
            Vector2 halfSize = _canvasRect.sizeDelta * 0.5f;
            Vector2 pSize    = promptRect.sizeDelta * 0.5f;
            targetPos.x = Mathf.Clamp(targetPos.x, -halfSize.x + pSize.x + 10f, halfSize.x - pSize.x - 10f);
            targetPos.y = Mathf.Clamp(targetPos.y, -halfSize.y + pSize.y + 10f, halfSize.y - pSize.y - 10f);

            promptRect.anchoredPosition = targetPos;
        }

        // Pulse scale nhẹ
        float scale = 1f + Mathf.Sin(Time.time * 4f) * 0.03f;
        promptRect.localScale = new Vector3(scale, scale, 1f);

        // Màu sắc theo trạng thái CanInteract
        bool canInteract = _current.CanInteract;
        if (interactButton != null) interactButton.interactable = canInteract;
        if (promptBg != null) promptBg.color = canInteract ? readyBgColor : disabledBgColor;
        if (keyBadgeBg != null) keyBadgeBg.color = canInteract ? readyColor : Color.gray;
    }

    /// <summary>
    /// Xử lý nhấn phím E hoặc click trực tiếp vào vật thể 3D
    /// </summary>
    void HandleDirectInteractionInput()
    {
        if (_current == null || !_current.CanInteract) return;

        // 1. Phím E
        if (Input.GetKeyDown(KeyCode.E))
        {
            ExecuteInteraction();
            return;
        }

        // 2. Click chuột trái (LMB) trong thế giới 3D vào vật thể
        if (Input.GetMouseButtonDown(0))
        {
            // Nếu con trỏ đang click trên UI, Button component sẽ tự nhận onClick
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            if (_mainCam != null)
            {
                Ray ray = _mainCam.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 20f, interactLayer))
                {
                    var hitIa = hit.collider.GetComponent<IInteractable>() 
                             ?? hit.collider.GetComponentInParent<IInteractable>();
                    if (hitIa == _current)
                    {
                        ExecuteInteraction();
                    }
                }
            }
        }
    }

    /// <summary>
    /// Gọi khi click vào UI button
    /// </summary>
    public void OnButtonClicked()
    {
        if (_current != null && _current.CanInteract)
        {
            ExecuteInteraction();
        }
    }

    void ExecuteInteraction()
    {
        if (_current == null) return;

        _current.Interact();

        // Kích hoạt animation vươn tay tương tác
        if (_anim != null)
        {
            _anim.TriggerInteract();
        }
    }

    void SetPromptVisible(bool visible)
    {
        if (promptRect != null && promptRect.gameObject.activeSelf != visible)
        {
            promptRect.gameObject.SetActive(visible);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0.8f, 0.2f);
        Gizmos.DrawSphere(transform.position, detectionRadius);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
