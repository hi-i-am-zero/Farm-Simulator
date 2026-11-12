using System;
using UnityEngine;

public class Plot : MonoBehaviour, IInteractable, IGrowable
{
    static readonly Color UntilledTint = new(0.72f, 0.68f, 0.58f, 1f);
    static readonly Color TilledTint = Color.white;
    static readonly Color WateredTint = new(0.55f, 0.72f, 0.95f, 1f);

    [SerializeField] SpriteRenderer cropRenderer;
    [SerializeField] SpriteRenderer soilRenderer;

    [Header("Sprites Theo Từng Trạng Thái Đất")]
    [Tooltip("Sprite 1: Đất trống chưa cuốc")]
    [SerializeField] Sprite untilledSprite;
    [Tooltip("Sprite 2: Đất đã cuốc")]
    [SerializeField] Sprite tilledSprite;
    [Tooltip("Sprite 3: Đất đã tưới nước")]
    [SerializeField] Sprite wateredSprite;

    public CropData Crop { get; private set; }
    public long PlantedTicks { get; private set; }
    public bool IsTilled { get; private set; }
    public bool IsWatered { get; private set; }

    double ElapsedSeconds =>
        (DateTime.UtcNow - new DateTime(PlantedTicks, DateTimeKind.Utc)).TotalSeconds;

    float Progress =>
        Crop == null ? 0f : Mathf.Clamp01((float)(ElapsedSeconds / Crop.growTime));

    public bool IsReady => Crop != null && Progress >= 1f;

    void Awake()
    {
        if (soilRenderer == null)
            soilRenderer = GetComponent<SpriteRenderer>();
        if (untilledSprite == null && soilRenderer != null && soilRenderer.sprite != null)
            untilledSprite = soilRenderer.sprite;
        UpdateSoilVisual();
    }

    void OnEnable() => Growables.All.Add(this);
    void OnDisable() => Growables.All.Remove(this);

    public void Interact()
    {
        // Cay chin: thu hoạch với mọi tool (tránh kẹt khi đang cầm cuốc/hạt)
        if (IsReady)
        {
            Harvest();
            return;
        }

        var gm = GameManager.Instance;
        var activeItem = gm != null ? gm.GetActiveHotbarItem() : null;

        if (activeItem == null || activeItem.IsEmpty)
        {
            UseHand();
            return;
        }

        if (activeItem.kind == ItemKind.Tool && activeItem.tool != null)
        {
            switch (activeItem.tool.type)
            {
                case ToolType.Hoe:
                    Till();
                    break;
                case ToolType.WateringCan:
                    Water();
                    break;
                default:
                    UseHand();
                    break;
            }
        }
        else if (activeItem.kind == ItemKind.Seed && activeItem.crop != null)
        {
            Plant(activeItem.crop);
        }
        else
        {
            UseHand();
        }
    }

    void Till()
    {
        if (Crop != null)
        {
            Toast.Show("Ô đang có cây");
            return;
        }

        if (IsTilled)
        {
            Toast.Show("Đã cuốc rồi");
            return;
        }

        IsTilled = true;
        IsWatered = false;
        UpdateSoilVisual();
        Toast.Show("Đã cuốc đất");
        AudioManager.Instance?.Play("plant");
    }

    void Water()
    {
        if (!IsTilled)
        {
            Toast.Show("Hãy cuốc đất");
            return;
        }

        if (IsWatered)
        {
            Toast.Show("Đã tưới rồi");
            return;
        }

        IsWatered = true;

        if (Crop != null && !IsReady)
        {
            float remaining = Mathf.Max(0f, Crop.growTime - (float)ElapsedSeconds);
            AddGrowth(remaining * 0.4f);
        }

        UpdateSoilVisual();
        Toast.Show("Đã tưới nước");
        AudioManager.Instance?.Play("plant");
    }

    public void Plant(CropData cropToPlant = null)
    {
        if (!IsTilled)
        {
            Toast.Show("Hãy cuốc đất");
            return;
        }

        if (Crop != null)
        {
            Toast.Show("Ô đã có cây");
            return;
        }

        var gm = GameManager.Instance;
        if (cropToPlant == null)
        {
            var active = gm != null ? gm.GetActiveHotbarItem() : null;
            if (active != null && active.kind == ItemKind.Seed)
                cropToPlant = active.crop;
            else if (gm != null)
                cropToPlant = gm.SelectedCrop;
        }

        if (cropToPlant == null || gm == null || gm.SeedCount(cropToPlant) <= 0)
        {
            Toast.Show("Hãy mua hạt ở NPC trồng cây hoặc chọn hạt từ túi đồ");
            return;
        }

        if (!gm.TryConsumeSeed(cropToPlant)) return;
        SetCrop(cropToPlant, DateTime.UtcNow.Ticks);

        // Tuoi san truoc khi trong van duoc huong bonus
        if (IsWatered)
            AddGrowth(Crop.growTime * 0.4f);

        Toast.Show($"Đã gieo {cropToPlant.cropName} (Còn: {gm.SeedCount(cropToPlant)})");
        AudioManager.Instance?.Play("plant");
    }

    void UseHand()
    {
        if (Crop == null)
        {
            Toast.Show(IsTilled ? "Cầm Hạt giống để gieo cây" : "Cầm Cuốc để cuốc đất (mua ở NPC nông sản)");
            return;
        }

        if (IsReady)
        {
            Harvest();
            return;
        }

        float left = Mathf.Max(0f, Crop.growTime - (float)ElapsedSeconds);
        Toast.Show($"Còn {Mathf.Ceil(left)} giây nữa");
    }

    void Harvest()
    {
        GameManager.Instance.AddHarvest(Crop);
        AudioManager.Instance?.Play("harvest");
        PopEffect.Spawn(Crop.icon, transform.position);
        Crop = null;
        PlantedTicks = 0;
        IsWatered = false;
        // Giữ đất đã cuốc sau thu hoạch
        UpdateVisual();
        UpdateSoilVisual();
    }

    public void SetCrop(CropData crop, long ticks)
    {
        Crop = crop;
        PlantedTicks = ticks;
        if (crop != null)
            IsTilled = true;
        UpdateVisual();
        UpdateSoilVisual();
    }

    public void SetState(CropData crop, long ticks, bool tilled, bool watered)
    {
        Crop = crop;
        PlantedTicks = ticks;
        IsTilled = tilled || crop != null;
        IsWatered = watered;
        UpdateVisual();
        UpdateSoilVisual();
    }

    // Thời tiết, mùa, tạm dừng đều gọi hàm này
    public void AddGrowth(float seconds)
    {
        if (Crop == null) return;
        PlantedTicks -= (long)((double)seconds * TimeSpan.TicksPerSecond);
    }

    void Update()
    {
        if (Crop != null) UpdateVisual();
    }

    void UpdateVisual()
    {
        if (cropRenderer == null) return;

        if (Crop == null || Crop.growthSprites == null || Crop.growthSprites.Length == 0)
        {
            cropRenderer.sprite = null;
            return;
        }

        int count = Crop.growthSprites.Length;
        int idx = Mathf.Min((int)(Progress * count), count - 1);
        cropRenderer.sprite = Crop.growthSprites[idx];
        cropRenderer.color = IsReady ? Color.white : new Color(0.8f, 0.8f, 0.8f, 1f);
    }

    void UpdateSoilVisual()
    {
        if (soilRenderer == null) return;

        if (IsWatered)
        {
            if (wateredSprite != null)
            {
                soilRenderer.sprite = wateredSprite;
                soilRenderer.color = Color.white;
            }
            else
            {
                soilRenderer.color = WateredTint;
            }
        }
        else if (IsTilled)
        {
            if (tilledSprite != null)
            {
                soilRenderer.sprite = tilledSprite;
                soilRenderer.color = Color.white;
            }
            else
            {
                soilRenderer.color = TilledTint;
            }
        }
        else
        {
            if (untilledSprite != null)
            {
                soilRenderer.sprite = untilledSprite;
                soilRenderer.color = Color.white;
            }
            else
            {
                soilRenderer.color = UntilledTint;
            }
        }
    }
}
