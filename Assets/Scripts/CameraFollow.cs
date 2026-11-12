using UnityEngine;
using UnityEngine.Tilemaps;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] float smooth = 8f;
    [SerializeField] bool clampToMap = true;
    [SerializeField] Vector2 minBounds = new(-28f, -34f);
    [SerializeField] Vector2 maxBounds = new(20f, 22f);

    Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
        var groundGo = GameObject.Find("Ground");
        if (groundGo != null && groundGo.TryGetComponent(out Tilemap tm))
        {
            tm.CompressBounds();
            var b = tm.localBounds;
            if (b.size.x > 1f && b.size.y > 1f)
            {
                minBounds = tm.transform.TransformPoint(b.min);
                maxBounds = tm.transform.TransformPoint(b.max);
            }
        }
    }

    void LateUpdate()
    {
        if (target == null) return;
        Vector3 goal = ClampPosition(new Vector3(target.position.x, target.position.y, transform.position.z));
        Vector3 next = Vector3.Lerp(transform.position, goal, 1f - Mathf.Exp(-smooth * Time.deltaTime));
        transform.position = ClampPosition(next);
    }

    Vector3 ClampPosition(Vector3 pos)
    {
        if (!clampToMap || cam == null || !cam.orthographic) return pos;

        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;

        float minX = minBounds.x + halfW;
        float maxX = maxBounds.x - halfW;
        float minY = minBounds.y + halfH;
        float maxY = maxBounds.y - halfH;

        pos.x = minX <= maxX ? Mathf.Clamp(pos.x, minX, maxX) : (minBounds.x + maxBounds.x) * 0.5f;
        pos.y = minY <= maxY ? Mathf.Clamp(pos.y, minY, maxY) : (minBounds.y + maxBounds.y) * 0.5f;
        return pos;
    }
}
