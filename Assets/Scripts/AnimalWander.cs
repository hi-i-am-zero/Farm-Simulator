using System.Collections;
using UnityEngine;

/// <summary>
/// Quản lý chuyển động tự do (AI đi lang thang) cho động vật: Gà, Bò, Cừu.
/// - Bước đi từng ô phẳng 1x1 mượt mà (không nhảy).
/// - Tự đổi hướng nhìn (FlipX), lắc thân khi bước và thở nhẹ khi đứng yên.
/// - Kiểm tra va chạm để không đi xuyên hàng rào, tường hay đè lên con khác.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class AnimalWander : MonoBehaviour
{
    public enum AnimalType { Chicken, Cow, Sheep, Pig, Dog }

    // ==========================================
    // 1. CẤU HÌNH TRONG INSPECTOR
    // ==========================================
    [Header("Loại Động Vật & Di Chuyển 1 Ô")]
    [SerializeField] AnimalType animalType = AnimalType.Chicken;
    [SerializeField] float stepDistance = 1.0f;       // Khoảng cách mỗi bước (1 ô lưới = 1 unit)
    [SerializeField] float stepDuration = 0.40f;      // Thời gian trượt qua 1 ô (giây)
    [SerializeField] float minWaitTime = 2.0f;        // Thời gian nghỉ tối thiểu giữa các bước
    [SerializeField] float maxWaitTime = 5.0f;        // Thời gian nghỉ tối đa giữa các bước
    [SerializeField] int maxStepFromHome = 4;        // Bán kính tối đa được đi xa chuồng/tổ

    [Header("Hiệu Ứng Hình Ảnh")]
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] bool enableIdleBreathing = true; // Thở phập phồng nhẹ khi đứng yên
    [SerializeField] bool enableWalkWaddle = true;    // Lắc lư thân mình khi bước chân

    [Header("Kiểm Tra Vật Cản")]
    [SerializeField] LayerMask blockingLayers = ~0;  // Các Layer chặn đường (tường, rào, chuồng)

    [Header("Giới Hạn Chuồng (Tự động khóa cổng)")]
    [SerializeField] bool usePenBounds = false;
    [SerializeField] Vector2 penMinBounds;
    [SerializeField] Vector2 penMaxBounds;

    // ==========================================
    // 2. BIẾN NỘI BỘ QUẢN LÝ TRẠNG THÁI
    // ==========================================
    public AnimalType Kind => animalType;

    Vector2 homePos;          // Vị trí gốc (chuồng/tổ ban đầu)
    Vector2 currentGridPos;   // Tọa độ ô lưới hiện tại
    bool isStepping = false;  // Đang trong quá trình bước đi?
    float waitTimer;          // Bộ đếm thời gian nghỉ
    Vector3 originalScale;    // Kích thước gốc của sprite
    Rigidbody2D rb;

    // ==========================================
    // 3. KHỞI TẠO (AWAKE & START)
    // ==========================================
    void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();

        // Cấu hình vật lý 2D: Kinematic để tự điều khiển chuyển động mượt mà
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    static Vector2 ToCellCenter(Vector2 pos) => new(Mathf.Floor(pos.x) + 0.5f, Mathf.Floor(pos.y) + 0.5f);

    /// <summary>
    /// Giới hạn số bước tối đa tính từ vị trí ban đầu.
    /// </summary>
    public void SetMaxStepFromHome(int steps) => maxStepFromHome = Mathf.Max(1, steps);

    /// <summary>
    /// Giới hạn vùng ô lưới mà con vật được phép di chuyển bên trong chuồng.
    /// </summary>
    public void SetPenBounds(Vector2 minPos, Vector2 maxPos)
    {
        Vector2 minCell = ToCellCenter(minPos);
        Vector2 maxCell = ToCellCenter(maxPos);
        penMinBounds = new Vector2(Mathf.Min(minCell.x, maxCell.x), Mathf.Min(minCell.y, maxCell.y));
        penMaxBounds = new Vector2(Mathf.Max(minCell.x, maxCell.x), Mathf.Max(minCell.y, maxCell.y));
        usePenBounds = true;
    }

    bool IsInsidePenBounds(Vector2 pos)
    {
        const float eps = 0.05f;
        return pos.x >= penMinBounds.x - eps && pos.x <= penMaxBounds.x + eps
            && pos.y >= penMinBounds.y - eps && pos.y <= penMaxBounds.y + eps;
    }

    Vector2 ClampToPenBounds(Vector2 pos)
    {
        return new Vector2(
            Mathf.Clamp(pos.x, penMinBounds.x, penMaxBounds.x),
            Mathf.Clamp(pos.y, penMinBounds.y, penMaxBounds.y)
        );
    }

    void Start()
    {
        // Làm tròn vị trí ban đầu vào đúng tâm ô lưới Tilemap (x + 0.5, y + 0.5)
        homePos = ToCellCenter(transform.position);
        if (usePenBounds)
            homePos = ClampToPenBounds(homePos);

        currentGridPos = homePos;

        if (rb != null) rb.position = currentGridPos;
        transform.position = new Vector3(currentGridPos.x, currentGridPos.y, transform.position.z);
        originalScale = transform.localScale;

        ResetWaitTimer();
    }

    // ==========================================
    // 4. VÒNG LẶP UPDATE CHÍNH
    // ==========================================
    void Update()
    {
        // Khi đang bước đi thì không xử lý chờ hay thở
        if (isStepping) return;

        // Hiệu ứng thở nhẹ nhàng khi đứng yên
        if (enableIdleBreathing)
        {
            ApplyBreathingEffect();
        }

        // Đếm ngược thời gian chờ để bước tiếp
        waitTimer -= Time.deltaTime;
        if (waitTimer <= 0f)
        {
            ResetWaitTimer();
            TryTakeStep();
        }
    }

    /// <summary>
    /// Tạo hiệu ứng thở phập phồng nhẹ theo hàm Sin
    /// </summary>
    void ApplyBreathingEffect()
    {
        float breath = Mathf.Sin(Time.time * 2.5f) * 0.03f;
        transform.localScale = new Vector3(
            originalScale.x * (1f - breath * 0.5f),
            originalScale.y * (1f + breath),
            originalScale.z
        );
    }

    void ResetWaitTimer()
    {
        waitTimer = Random.Range(minWaitTime, maxWaitTime);
    }

    // ==========================================
    // 5. TÌM VÀ BƯỚC SANG Ô TIẾP THEO
    // ==========================================
    void TryTakeStep()
    {
        // 4 hướng có thể đi: Lên, Xuống, Trái, Phải
        Vector2[] directions = new Vector2[]
        {
            Vector2.up * stepDistance,
            Vector2.down * stepDistance,
            Vector2.left * stepDistance,
            Vector2.right * stepDistance
        };

        // Xáo trộn ngẫu nhiên thứ tự 4 hướng để chuyển động tự nhiên
        ShuffleDirections(directions);

        // Thử từng hướng, hướng nào đi được thì thực hiện ngay
        foreach (var dir in directions)
        {
            Vector2 target = currentGridPos + dir;

            // 1. Kiểm tra không đi quá xa chuồng/tổ
            if (Vector2.Distance(target, homePos) > maxStepFromHome * stepDistance)
                continue;

            // 1b. Không bước ra ngoài giới hạn bên trong chuồng (kể cả qua ô cổng mở)
            if (usePenBounds && !IsInsidePenBounds(target))
                continue;

            // 2. Kiểm tra ô đích có bị chặn không (tường, hàng rào, cửa chắn động vật, con vật khác)
            if (IsCellBlocked(target))
                continue;

            // Ô đích an toàn -> bắt đầu bước đi
            StartCoroutine(StepRoutine(target, dir));
            return;
        }
    }

    /// <summary>
    /// Kiểm tra xem ô tọa độ target có vật cản nào không
    /// </summary>
    bool IsCellBlocked(Vector2 target)
    {
        // Quét một hộp kích thước 0.65x0.65 tại tâm ô đích
        Collider2D[] colliders = Physics2D.OverlapBoxAll(target, new Vector2(0.65f, 0.65f), 0f, blockingLayers);

        foreach (var col in colliders)
        {
            // Bỏ qua chính bản thân con vật và các đối tượng con (như icon trứng)
            if (col.gameObject == gameObject || col.transform.IsChildOf(transform))
                continue;

            // Nếu có con vật khác ở ô này -> bị chặn
            if (col.GetComponent<AnimalWander>() != null || col.GetComponentInParent<AnimalWander>() != null)
                return true;

            // Cửa chuồng chỉ chắn động vật (dùng Trigger để Player vẫn ra vào tự do)
            if (col.GetComponent<AnimalGateBlocker>() != null || col.GetComponentInParent<AnimalGateBlocker>() != null)
                return true;

            // Bỏ qua trigger vô hại (như trigger ô đất trồng, trigger tương tác)
            if (col.isTrigger)
                continue;

            // Bất kỳ vật cản đặc nào (Hàng rào, Tường nhà, Máng nước, Cây cối, Player)
            return true;
        }

        return false;
    }

    /// <summary>
    /// Xáo trộn mảng hướng ngẫu nhiên
    /// </summary>
    void ShuffleDirections(Vector2[] array)
    {
        for (int i = 0; i < array.Length; i++)
        {
            int r = Random.Range(i, array.Length);
            var temp = array[i];
            array[i] = array[r];
            array[r] = temp;
        }
    }

    // ==========================================
    // 6. COROUTINE DI CHUYỂN MƯỢT MÀ QUA 1 Ô
    // ==========================================
    IEnumerator StepRoutine(Vector2 target, Vector2 dir)
    {
        isStepping = true;
        Vector2 start = currentGridPos;
        float elapsed = 0f;

        // Lật mặt trái/phải theo hướng di chuyển ngang
        if (spriteRenderer != null && Mathf.Abs(dir.x) > 0.01f)
        {
            spriteRenderer.flipX = dir.x < 0;
        }

        while (elapsed < stepDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / stepDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            // Trượt phẳng êm ái trên mặt đất (hoàn toàn không nhảy)
            Vector2 currentPos = Vector2.Lerp(start, target, smoothT);
            if (rb != null)
                rb.MovePosition(currentPos);
            else
                transform.position = new Vector3(currentPos.x, currentPos.y, transform.position.z);

            // Lắc nhẹ thân mình (waddle) mô phỏng động tác bước chân
            if (enableWalkWaddle)
            {
                float waddleZ = Mathf.Sin(t * Mathf.PI * 2f) * 4f;
                transform.rotation = Quaternion.Euler(0f, 0f, waddleZ);
            }

            yield return null;
        }

        // Đảm bảo đến đúng vị trí đích và reset góc quay
        if (rb != null) rb.MovePosition(target);
        transform.position = new Vector3(target.x, target.y, transform.position.z);
        transform.rotation = Quaternion.identity;
        transform.localScale = originalScale;
        currentGridPos = target;

        isStepping = false;
    }

    // ==========================================
    // 7. VẼ GIZMOS TRONG SCENE VIEW ĐỂ DEBUG
    // ==========================================
    void OnDrawGizmosSelected()
    {
        // Khung xanh lá: Phạm vi tối đa con vật được đi quanh tổ
        Gizmos.color = Color.green;
        Vector3 center = Application.isPlaying ? (Vector3)homePos : transform.position;
        Gizmos.DrawWireCube(center, Vector3.one * (maxStepFromHome * 2 * stepDistance));

        // Khung đỏ: Kích thước vùng kiểm tra vật cản
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, new Vector3(0.65f, 0.65f, 0f));
    }
}
