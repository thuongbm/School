using UnityEngine;

/// <summary>
/// Camera bám theo nhân vật từ phía sau.
/// Gắn vào Main Camera và kéo Player vào trường Target.
/// </summary>
public class FollowCamera : MonoBehaviour
{
    [Tooltip("Transform của nhân vật cần theo dõi")]
    public Transform target;

    [Tooltip("Khoảng cách theo trục Y (cao)")]
    public float heightOffset = 4f;

    [Tooltip("Khoảng cách theo trục Z (lùi sau)")]
    public float distanceBack = 7f;

    [Tooltip("Độ mượt khi di chuyển camera")]
    public float smoothSpeed = 8f;

    private Vector3 _desiredPos;

    void LateUpdate()
    {
        if (target == null) return;

        // Vị trí mong muốn: sau lưng nhân vật theo hướng forward của camera
        _desiredPos = target.position
                    - target.forward * distanceBack
                    + Vector3.up * heightOffset;

        transform.position = Vector3.Lerp(transform.position, _desiredPos, smoothSpeed * Time.deltaTime);

        // Nhìn vào đầu nhân vật
        transform.LookAt(target.position + Vector3.up * 1f);
    }
}

