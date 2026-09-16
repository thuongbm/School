using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý nhiệm vụ: xuất phát từ tủ đồ Room1, đến cuối ống thông gió Room2.
/// </summary>
public class MissionManager : MonoBehaviour
{
    [Header("UI")]
    public Text missionText;
    public Text statusText;

    private bool _missionComplete = false;

    void Start()
    {
        UpdateUI("🎯 Nhiệm vụ: Đến cuối ống thông gió phòng 2!", "Đang thực hiện...");
    }

    /// <summary>Gọi khi nhân vật chạm goal trigger.</summary>
    public void OnMissionComplete()
    {
        if (_missionComplete) return;
        _missionComplete = true;
        UpdateUI("✅ Nhiệm vụ hoàn thành!", "Bạn đã đến cuối ống thông gió!");
        Debug.Log("[Mission] COMPLETE!");
    }

    void UpdateUI(string mission, string status)
    {
        if (missionText != null) missionText.text = mission;
        if (statusText  != null) statusText.text  = status;
    }
}

