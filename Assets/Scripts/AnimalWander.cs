using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class AnimalWander : MonoBehaviour
{
    public enum AnimalType { Chicken, Cow, Sheep, Pig, Dog }

    [Header("Loại Động Vật & Di Chuyển 1 Ô")]
    [SerializeField] AnimalType animalType = AnimalType.Chicken;
    [SerializeField] float stepDistance = 1.0f;  // Đi đúng 1 ô (1 tile)
    [SerializeField] float stepDuration = 0.40f; // Thời gian bước qua 1 ô (mượt mà)
    [SerializeField] float minWaitTime = 2.0f;
    [SerializeField] float maxWaitTime = 5.0f;
    [SerializeField] int maxStepFromHome = 4;   // Bán kính ô tối đa cách tổ

    [Header("Hiệu Ứng Thở & Lắc Chân Nhẹ (KHÔNG NHẢY)")]
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] bool enableIdleBreathing = true;
    [SerializeField] bool enableWalkWaddle = true; // Lắc nhẹ thân mình khi bước đi

    [Header("Chống Xuyên Tường / Hàng Rào")]
    [SerializeField] LayerMask blockingLayers = ~0; // Mặc định kiểm tra mọi layer

    Vector2 homePos;
    Vector2 currentGridPos;
    bool isStepping = false;
    float waitTimer;
    Vector3 originalScale;

    void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
    }

    void Start()
    {
        homePos = new Vector2(Mathf.Round(transform.position.x), Mathf.Round(transform.position.y));
        currentGridPos = homePos;
        transform.position = new Vector3(currentGridPos.x, currentGridPos.y, transform.position.z);
        originalScale = transform.localScale;
        waitTimer = Random.Range(minWaitTime, maxWaitTime);
    }

    void Update()
    {
        if (isStepping) return;

        // Khi đứng yên: Thở nhẹ nhàng
        if (enableIdleBreathing)
        {
            float breath = Mathf.Sin(Time.time * 2.5f) * 0.03f;
            transform.localScale = new Vector3(originalScale.x * (1f - breath * 0.5f),
                                               originalScale.y * (1f + breath),
                                               originalScale.z);
        }

        waitTimer -= Time.deltaTime;
        if (waitTimer <= 0f)
        {
            waitTimer = Random.Range(minWaitTime, maxWaitTime);
            TryTakeStep();
        }
    }

    void TryTakeStep()
    {
        // 4 hướng: Lên, Xuống, Trái, Phải
        Vector2[] directions = new Vector2[]
        {
            Vector2.up * stepDistance,
            Vector2.down * stepDistance,
            Vector2.left * stepDistance,
            Vector2.right * stepDistance
        };

        // Trộn ngẫu nhiên các hướng
        for (int i = 0; i < directions.Length; i++)
        {
            int r = Random.Range(i, directions.Length);
            var temp = directions[i];
            directions[i] = directions[r];
            directions[r] = temp;
        }

        foreach (var dir in directions)
        {
            Vector2 target = currentGridPos + dir;

            // 1. Giới hạn trong chuồng / quanh tổ
            if (Vector2.Distance(target, homePos) > maxStepFromHome * stepDistance)
                continue;

            // 2. KIỂM TRA VẬT CẢN CHÍNH XÁC (Quét ô 0.65x0.65 tại đích đến)
            Collider2D[] collidersAtTarget = Physics2D.OverlapBoxAll(target, new Vector2(0.65f, 0.65f), 0f, blockingLayers);
            bool isBlocked = false;

            foreach (var col in collidersAtTarget)
            {
                // Bỏ qua chính bản thân và các con (như EggIcon)
                if (col.gameObject == gameObject || col.transform.IsChildOf(transform))
                    continue;

                // Nếu có con vật khác đang đứng ở ô này -> bị chặn
                if (col.GetComponent<AnimalWander>() != null || col.GetComponentInParent<AnimalWander>() != null)
                {
                    isBlocked = true;
                    break;
                }

                // Bỏ qua trigger vô hại (như trigger ô đất, trigger tương tác)
                if (col.isTrigger)
                    continue;

                // Bất kỳ vật thể cản đặc nào (Hàng rào, Nhà, Cây, Tường, Tilemap Collider, Người chơi)
                isBlocked = true;
                break;
            }

            // Nếu ô này bị vướng -> thử hướng khác
            if (isBlocked) continue;

            // Ô trống an toàn -> bước đi
            StartCoroutine(StepRoutine(target, dir));
            return;
        }
    }

    IEnumerator StepRoutine(Vector2 target, Vector2 dir)
    {
        isStepping = true;
        Vector2 start = currentGridPos;
        float elapsed = 0f;

        // Lật mặt trái/phải theo hướng đi
        if (spriteRenderer != null && Mathf.Abs(dir.x) > 0.01f)
        {
            spriteRenderer.flipX = dir.x < 0;
        }

        while (elapsed < stepDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / stepDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            // BƯỚC ĐI TRƯỢT PHẲNG MẶT ĐẤT (KHÔNG NHẢY)
            Vector2 currentPos = Vector2.Lerp(start, target, smoothT);
            transform.position = new Vector3(currentPos.x, currentPos.y, transform.position.z);

            // Lắc nhẹ thân mình mô phỏng bước đi
            if (enableWalkWaddle)
            {
                float waddleZ = Mathf.Sin(t * Mathf.PI * 2f) * 4f;
                transform.rotation = Quaternion.Euler(0f, 0f, waddleZ);
            }

            yield return null;
        }

        transform.position = new Vector3(target.x, target.y, transform.position.z);
        transform.rotation = Quaternion.identity;
        transform.localScale = originalScale;
        currentGridPos = target;

        isStepping = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector3 center = Application.isPlaying ? (Vector3)homePos : transform.position;
        Gizmos.DrawWireCube(center, Vector3.one * (maxStepFromHome * 2 * stepDistance));

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, new Vector3(0.65f, 0.65f, 0f));
    }
}
