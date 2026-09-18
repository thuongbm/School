using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// - Nút "Reset" cố định ở góc màn hình: bấm để tải lại scene hiện tại.
/// - Hiện bảng thông báo khi nhân vật rơi xuống vực (gọi ShowDeathMessage() từ PitDeathZone).
/// </summary>
public class GameFlowUI : MonoBehaviour
{
    Text       _deathText;
    GameObject _deathPanel;

    void Awake()
    {
        // Đảm bảo có EventSystem để nút bấm được (InteractionDetector cũng tự tạo,
        // nhưng phòng trường hợp script này chạy trước / độc lập).
        if (FindObjectOfType<EventSystem>() == null)
        {
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<StandaloneInputModule>();
        }

        BuildUI();
    }

    void BuildUI()
    {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // ── Canvas riêng cho Reset + Death UI ─────────────────
        var cGO = new GameObject("GameFlowCanvas");
        var canvas = cGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // luôn nổi trên cùng
        var scaler = cGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight  = 0.5f;
        cGO.AddComponent<GraphicRaycaster>();

        // ── Nút Reset (góc trên phải, luôn hiện) ──────────────
        var btnGO = new GameObject("ResetButton");
        btnGO.transform.SetParent(cGO.transform, false);
        var btnImg = btnGO.AddComponent<Image>();
        btnImg.color = new Color(0.82f, 0.16f, 0.16f, 0.92f);
        var btnRect = btnGO.GetComponent<RectTransform>();
        btnRect.anchorMin       = new Vector2(1f, 1f);
        btnRect.anchorMax       = new Vector2(1f, 1f);
        btnRect.pivot           = new Vector2(1f, 1f);
        btnRect.anchoredPosition = new Vector2(-24f, -24f);
        btnRect.sizeDelta        = new Vector2(130f, 52f);

        var btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        var bc = btn.colors;
        bc.highlightedColor = new Color(1f, 0.35f, 0.35f, 1f);
        bc.pressedColor     = new Color(0.6f, 0.1f, 0.1f, 1f);
        btn.colors = bc;
        btn.onClick.AddListener(OnResetClicked);

        var btnTxtGO = new GameObject("Text");
        btnTxtGO.transform.SetParent(btnGO.transform, false);
        var btnTxt = btnTxtGO.AddComponent<Text>();
        btnTxt.text = "Reset";
        btnTxt.font = font;
        btnTxt.fontSize = 24;
        btnTxt.fontStyle = FontStyle.Bold;
        btnTxt.color = Color.white;
        btnTxt.alignment = TextAnchor.MiddleCenter;
        var btnTxtRect = btnTxtGO.GetComponent<RectTransform>();
        btnTxtRect.anchorMin = Vector2.zero; btnTxtRect.anchorMax = Vector2.one;
        btnTxtRect.sizeDelta = Vector2.zero;

        // ── Bảng thông báo chết (ẩn lúc đầu) ───────────────────
        var panelGO = new GameObject("DeathPanel");
        panelGO.transform.SetParent(cGO.transform, false);
        var panelImg = panelGO.AddComponent<Image>();
        panelImg.color = new Color(0f, 0f, 0f, 0.72f);
        var panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero; panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;

        var txtGO = new GameObject("DeathText");
        txtGO.transform.SetParent(panelGO.transform, false);
        _deathText = txtGO.AddComponent<Text>();
        _deathText.font      = font;
        _deathText.fontSize  = 40;
        _deathText.fontStyle = FontStyle.Bold;
        _deathText.color     = Color.white;
        _deathText.alignment = TextAnchor.MiddleCenter;
        var txtRect = txtGO.GetComponent<RectTransform>();
        txtRect.anchorMin = new Vector2(0.08f, 0.35f);
        txtRect.anchorMax = new Vector2(0.92f, 0.65f);
        txtRect.sizeDelta = Vector2.zero;

        _deathPanel = panelGO;
        _deathPanel.SetActive(false);

        // Panel chết không được che nút Reset -> đưa panel xuống dưới nút trong thứ tự vẽ
        panelGO.transform.SetSiblingIndex(0);
    }

    /// <summary>Gọi từ PitDeathZone khi Player rơi xuống vực.</summary>
    public void ShowDeathMessage()
    {
        if (_deathText != null)
            _deathText.text = "☠ Char9 đã rơi xuống vực!\nBấm \"Reset\" để chơi lại.";
        if (_deathPanel != null)
            _deathPanel.SetActive(true);
    }

    void OnResetClicked()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
