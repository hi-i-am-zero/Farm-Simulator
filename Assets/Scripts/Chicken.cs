using System;
using UnityEngine;

/// <summary>
/// Quản lý vòng đời của Con Gà:
/// 1. Chưa cho ăn: Màu lông hơi xám, bấm tương tác để cho ăn (tốn tiền thức ăn).
/// 2. Đã cho ăn: Đếm ngược thời gian lớn dần để đẻ trứng.
/// 3. Sẵn sàng thu hoạch: Hiện icon quả trứng trên đầu, bấm vào để thu hoạch trứng vào túi đồ.
/// </summary>
public class Chicken : MonoBehaviour, IInteractable, IGrowable
{
    // ==========================================
    // 1. CẤU HÌNH TRONG INSPECTOR
    // ==========================================
    [Header("Cấu Hình Thu Hoạch & Thức Ăn")]
    [SerializeField] CropData eggItem;         // Dữ liệu quả trứng (Assets/Data/Crops/Egg.asset)
    [SerializeField] int feedCost = 5;          // Chi phí tiền để cho gà ăn (5$)
    [SerializeField] float eggTime = 30f;       // Thời gian đẻ trứng sau khi cho ăn (30 giây)

    [Header("Hiển Thị Hình Ảnh")]
    [SerializeField] SpriteRenderer body;      // Sprite thân gà
    [SerializeField] GameObject eggIcon;       // Icon quả trứng hiện trên đầu gà khi đã đẻ

    // ==========================================
    // 2. TRẠNG THÁI NỘI BỘ
    // ==========================================
    public long FedTicks { get; private set; } // Thời điểm cho ăn (0 = chưa cho ăn)
    public bool IsFed => FedTicks != 0;

    // Tính số giây đã trôi qua kể từ khi cho ăn
    double ElapsedSeconds =>
        (DateTime.UtcNow - new DateTime(FedTicks, DateTimeKind.Utc)).TotalSeconds;

    // Gà đã đẻ trứng xong chưa?
    public bool IsReady => IsFed && ElapsedSeconds >= eggTime;

    // ==========================================
    // 3. ĐĂNG KÝ VÀO DANH SÁCH GROWABLES
    // ==========================================
    void OnEnable() => Growables.All.Add(this);
    void OnDisable() => Growables.All.Remove(this);

    // ==========================================
    // 4. TƯƠNG TÁC (KHI NGƯỜI CHƠI BẤM E / SPACE)
    // ==========================================
    public void Interact()
    {
        if (!IsFed)
        {
            Feed();
        }
        else if (IsReady)
        {
            Collect();
        }
        else
        {
            // Đang chờ đẻ: Hiển thị thông báo thời gian còn lại
            float secondsLeft = Mathf.Max(0f, eggTime - (float)ElapsedSeconds);
            Toast.Show($"Gà sẽ đẻ trứng sau {Mathf.Ceil(secondsLeft)} giây");
        }
    }

    /// <summary>
    /// Cho gà ăn (trừ tiền trong GameManager)
    /// </summary>
    void Feed()
    {
        if (!GameManager.Instance.TrySpend(feedCost)) return;

        SetFed(DateTime.UtcNow.Ticks);
        AudioManager.Instance?.Play("plant");
    }

    /// <summary>
    /// Thu hoạch trứng (cộng trứng vào kho và hiện hiệu ứng)
    /// </summary>
    void Collect()
    {
        if (eggItem == null)
        {
            Debug.LogWarning("Chicken: Chưa gán eggItem vào Inspector!", this);
            return;
        }

        GameManager.Instance.AddHarvest(eggItem);
        AudioManager.Instance?.Play("harvest");
        PopEffect.Spawn(eggItem.icon, transform.position);

        // Reset lại trạng thái chưa cho ăn
        SetFed(0);
    }

    /// <summary>
    /// Đặt thời điểm cho ăn (hỗ trợ Save/Load game)
    /// </summary>
    public void SetFed(long ticks)
    {
        FedTicks = ticks;
        UpdateVisual();
    }

    /// <summary>
    /// Tăng tốc thời gian sinh trưởng (khi ngủ qua đêm)
    /// </summary>
    public void AddGrowth(float seconds)
    {
        if (!IsFed) return;
        FedTicks -= (long)((double)seconds * TimeSpan.TicksPerSecond);
    }

    // ==========================================
    // 5. CẬP NHẬT GIAO DIỆN HÌNH ẢNH
    // ==========================================
    void Update() => UpdateVisual();

    void UpdateVisual()
    {
        // Hiện quả trứng trên đầu khi đã đẻ
        if (eggIcon != null)
            eggIcon.SetActive(IsReady);

        // Màu lông gà: Đã ăn = Trắng sáng bình thường, Chưa ăn = Hơi xám tối
        if (body != null)
            body.color = IsFed ? Color.white : new Color(0.75f, 0.75f, 0.75f, 1f);
    }
}
