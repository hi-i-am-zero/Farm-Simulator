using UnityEngine;

public class PlayerVisual : MonoBehaviour
{
    [SerializeField] Rigidbody2D body;
    [SerializeField] SpriteRenderer sprite;
    [SerializeField] float bobSpeed = 14f;
    [SerializeField] float bobAmount = 0.05f;

    [Header("Sprite Animations (Tuỳ chọn)")]
    [SerializeField] Sprite[] idleSprites;
    [SerializeField] Sprite[] runSprites;
    [SerializeField] float animFps = 10f;

    Vector3 startPos;
    Vector2 lastPos;
    float animTimer;
    int currentFrame;

    void Start()
    {
        if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>();
        if (body == null) body = GetComponent<Rigidbody2D>();
        if (sprite != null) startPos = sprite.transform.localPosition;
        if (body != null) lastPos = body.position;
    }

    void Update()
    {
        if (body == null || sprite == null) return;

        Vector2 delta = body.position - lastPos;
        lastPos = body.position;
        bool moving = delta.sqrMagnitude > 0.00001f;

        if (Mathf.Abs(delta.x) > 0.0001f) sprite.flipX = delta.x < 0;

        // Frame animation if provided
        Sprite[] activeClip = moving ? runSprites : idleSprites;
        if (activeClip != null && activeClip.Length > 0)
        {
            animTimer += Time.deltaTime * animFps;
            currentFrame = (int)animTimer % activeClip.Length;
            sprite.sprite = activeClip[currentFrame];
        }
        else
        {
            // Fallback bobbing animation as in PDF
            float bob = moving ? Mathf.Abs(Mathf.Sin(Time.time * bobSpeed)) * bobAmount : 0f;
            sprite.transform.localPosition = startPos + Vector3.up * bob;
        }
    }
}
