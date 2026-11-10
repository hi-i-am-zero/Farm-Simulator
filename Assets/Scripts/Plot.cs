using System;
using UnityEngine;

public class Plot : MonoBehaviour, IInteractable, IGrowable
{
    static readonly Color UntilledTint = new(0.72f, 0.68f, 0.58f, 1f);
    static readonly Color TilledTint = Color.white;
    static readonly Color WateredTint = new(0.55f, 0.72f, 0.95f, 1f);

    [SerializeField] SpriteRenderer cropRenderer;
    [SerializeField] SpriteRenderer soilRenderer;

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
        UpdateSoilVisual();
    }

    void OnEnable() => Growables.All.Add(this);
    void OnDisable() => Growables.All.Remove(this);

    public void Interact()
    {
        var tool = GameManager.Instance != null ? GameManager.Instance.SelectedTool : null;

        // Chưa mua/chọn tool: vẫn cho thu hoạch cây chín
        if (tool == null)
        {
            if (IsReady) Harvest();
            else Toast.Show("Hãy mua công cụ ở thanh dưới");
            return;
        }

        switch (tool.type)
        {
            case ToolType.Hoe:
                Till();
                break;
            case ToolType.WateringCan:
                Water();
                break;
            case ToolType.Seed:
                Plant();
                break;
            default:
                UseHand();
                break;
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

    void Plant()
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
        if (gm.SelectedCrop == null) { Toast.Show("Hãy mua hạt ở NPC trồng cây"); return; }
        if (!gm.TryConsumeSeed(gm.SelectedCrop)) return;
        SetCrop(gm.SelectedCrop, DateTime.UtcNow.Ticks);
        AudioManager.Instance?.Play("plant");
    }

    void UseHand()
    {
        if (Crop == null)
        {
            Toast.Show(IsTilled ? "Mua hạt ở NPC rồi chọn tool Hạt" : "Chọn cuốc để làm đất");
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
        if (Crop == null || Crop.growthSprites == null || Crop.growthSprites.Length == 0)
        {
            if (cropRenderer != null)
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
            soilRenderer.color = WateredTint;
        else if (IsTilled)
            soilRenderer.color = TilledTint;
        else
            soilRenderer.color = UntilledTint;
    }
}
