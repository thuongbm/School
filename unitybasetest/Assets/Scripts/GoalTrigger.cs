using UnityEngine;

/// <summary>
/// Trigger zone: khi Player vào vùng này thì hoàn thành nhiệm vụ.
/// Gắn vào GoalZone GameObject (có Collider isTrigger=true).
/// </summary>
public class GoalTrigger : MonoBehaviour
{
    [Tooltip("Tag của Player")]
    public string playerTag = "Player";

    private MissionManager _mission;
    private bool _triggered = false;

    void Start()
    {
        _mission = FindObjectOfType<MissionManager>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (_triggered) return;
        if (!other.CompareTag(playerTag)) return;

        _triggered = true;
        Debug.Log("[GoalTrigger] Player reached goal!");

        if (_mission != null)
            _mission.OnMissionComplete();
    }

    // Vẽ Gizmo để dễ thấy trong Editor
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.35f);
        var col = GetComponent<BoxCollider>();
        if (col != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(col.center, col.size);
        }
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}

