using System;
using UnityEngine;

public class Plot : MonoBehaviour, IInteractable, IGrowable
{
    [SerializeField] SpriteRenderer cropRenderer;

    public CropData Crop { get; private set; }
    public long PlantedTicks { get; private set; }

    double ElapsedSeconds =>
        (DateTime.UtcNow - new DateTime(PlantedTicks, DateTimeKind.Utc)).TotalSeconds;

    float Progress =>
        Crop == null ? 0f : Mathf.Clamp01((float)(ElapsedSeconds / Crop.growTime));

    public bool IsReady => Crop != null && Progress >= 1f;

    void OnEnable() => Growables.All.Add(this);
    void OnDisable() => Growables.All.Remove(this);

    public void Interact()
    {
        if (Crop == null) Plant();
        else if (IsReady) Harvest();
        else
        {
            float left = Mathf.Max(0f, Crop.growTime - (float)ElapsedSeconds);
            Toast.Show($"Còn {Mathf.Ceil(left)} giây nữa");
        }
    }

    void Plant()
    {
        var gm = GameManager.Instance;
        if (gm.SelectedCrop == null) { Toast.Show("Hãy chọn hạt giống"); return; }
        if (!gm.TrySpend(gm.SelectedCrop.seedPrice)) return;
        SetCrop(gm.SelectedCrop, DateTime.UtcNow.Ticks);
        AudioManager.Instance?.Play("plant");
    }

    void Harvest()
    {
        GameManager.Instance.AddHarvest(Crop);
        AudioManager.Instance?.Play("harvest");
        PopEffect.Spawn(Crop.icon, transform.position);
        SetCrop(null, 0);
    }

    public void SetCrop(CropData crop, long ticks)
    {
        Crop = crop;
        PlantedTicks = ticks;
        UpdateVisual();
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
            cropRenderer.sprite = null;
            return;
        }

        int count = Crop.growthSprites.Length;
        int idx = Mathf.Min((int)(Progress * count), count - 1);
        cropRenderer.sprite = Crop.growthSprites[idx];
        cropRenderer.color = IsReady ? Color.white : new Color(0.8f, 0.8f, 0.8f, 1f);
    }
}
