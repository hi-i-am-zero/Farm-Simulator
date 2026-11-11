using UnityEngine;

/// <summary>
/// Đặt tại ô cửa chuồng ở dạng Trigger để Player ra vào bình thường,
/// nhưng <see cref="AnimalWander"/> vẫn coi ô này là vật cản và không bước ra ngoài.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class AnimalGateBlocker : MonoBehaviour
{
    void Reset()
    {
        EnsureTriggerCollider(resetSize: true);
    }

    void Awake()
    {
        EnsureTriggerCollider(resetSize: false);
    }

    void EnsureTriggerCollider(bool resetSize)
    {
        var box = GetComponent<BoxCollider2D>();
        if (box == null) return;

        box.isTrigger = true;
        if (resetSize)
            box.size = new Vector2(0.9f, 0.9f);
    }

    void OnDrawGizmos()
    {
        var box = GetComponent<BoxCollider2D>();
        Vector3 size = box != null ? new Vector3(box.size.x, box.size.y, 0.1f) : new Vector3(0.9f, 0.9f, 0.1f);
        Vector3 offset = box != null ? new Vector3(box.offset.x, box.offset.y, 0f) : Vector3.zero;
        Vector3 center = transform.position + offset;

        Gizmos.color = new Color(0.15f, 0.85f, 0.35f, 0.28f);
        Gizmos.DrawCube(center, size);
        Gizmos.color = new Color(0.15f, 0.85f, 0.35f, 0.9f);
        Gizmos.DrawWireCube(center, size);
    }
}
