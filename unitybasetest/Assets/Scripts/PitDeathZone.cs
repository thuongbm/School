using UnityEngine;

/// <summary>
/// Gắn vào vùng "vực" (pit/chasm) trong Room2. Khi Player rơi vào đây,
/// nhân vật sẽ "chết" (khoá điều khiển + animation ngã) và UI hiện thông báo.
/// </summary>
public class PitDeathZone : MonoBehaviour
{
    public string playerTag = "Player";
    bool _triggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (_triggered) return;
        if (!other.CompareTag(playerTag)) return;
        _triggered = true;

        Debug.Log("[PitDeathZone] Player rơi xuống vực!");

        var pc = other.GetComponentInParent<PlayerController>();
        if (pc != null) pc.Die();

        var ui = FindObjectOfType<GameFlowUI>();
        if (ui != null) ui.ShowDeathMessage();
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.35f);
        var col = GetComponent<BoxCollider>();
        if (col != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(col.center, col.size);
        }
    }
}
