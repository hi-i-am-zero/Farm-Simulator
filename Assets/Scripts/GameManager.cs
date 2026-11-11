using System;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] int startMoney = 80;
    [SerializeField] CropData defaultCrop;

    public int Money { get; set; }
    public CropData SelectedCrop { get; private set; }
    public ToolData SelectedTool { get; private set; }
    public readonly Dictionary<CropData, int> Harvested = new();
    public readonly Dictionary<CropData, int> Seeds = new();
    readonly HashSet<string> ownedTools = new();

    public event Action<int> OnMoneyChanged;
    public event Action<CropData> OnCropSelected;
    public event Action<ToolData> OnToolSelected;
    public event Action OnInventoryChanged;
    public event Action OnToolsOwnedChanged;
    public event Action OnSeedsChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => Instance = null;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Money = startMoney;
        SelectedCrop = defaultCrop;
        SelectedTool = null;

        // Dam bao co PlotFieldBootstrap (dat o dat neu scene thieu)
        if (GetComponent<PlotFieldBootstrap>() == null)
            gameObject.AddComponent<PlotFieldBootstrap>();

        // Đảm bảo có NpcShopBootstrap (tạo và định vị 4 NPC Shop + 3 Chuồng động vật luôn đầy đủ)
        if (GetComponent<NpcShopBootstrap>() == null)
            gameObject.AddComponent<NpcShopBootstrap>();
    }

    void Start()
    {
        // Collider cung o origin cua ChickenShop day player luc spawn
        var shop = GameObject.Find("ChickenShop");
        if (shop == null) return;
        foreach (var col in shop.GetComponents<Collider2D>())
        {
            if (col != null && !col.isTrigger)
                Destroy(col);
        }
    }

    public int SeedCount(CropData crop)
    {
        if (crop == null) return 0;
        Seeds.TryGetValue(crop, out int n);
        return n;
    }

    public bool TryBuySeed(CropData crop)
    {
        if (crop == null) return false;
        if (!TrySpend(crop.seedPrice)) return false;
        Seeds.TryGetValue(crop, out int n);
        Seeds[crop] = n + 1;
        SelectCrop(crop);
        OnSeedsChanged?.Invoke();
        Toast.Show($"Đã mua hạt {crop.cropName} (túi: {Seeds[crop]})");
        AudioManager.Instance?.Play("sell");
        return true;
    }

    public bool TryConsumeSeed(CropData crop)
    {
        if (crop == null || SeedCount(crop) <= 0)
        {
            Toast.Show("Hãy mua hạt ở NPC trồng cây");
            return false;
        }

        int left = Seeds[crop] - 1;
        if (left > 0) Seeds[crop] = left;
        else Seeds.Remove(crop);
        OnSeedsChanged?.Invoke();
        return true;
    }

    public List<HarvestSave> GetSeedSaves()
    {
        var list = new List<HarvestSave>();
        foreach (var kv in Seeds)
        {
            if (kv.Key == null || kv.Value <= 0) continue;
            list.Add(new HarvestSave { cropName = kv.Key.cropName, count = kv.Value });
        }
        return list;
    }

    public void ApplySeeds(IEnumerable<HarvestSave> saves, CropDatabase db)
    {
        Seeds.Clear();
        if (saves != null && db != null)
        {
            foreach (var s in saves)
            {
                var crop = db.Find(s.cropName);
                if (crop != null && s.count > 0)
                    Seeds[crop] = s.count;
            }
        }
        EnsureSelectedSeed();
        OnSeedsChanged?.Invoke();
    }

    // Sau load / truoc khi trong: chon crop con hat trong tui
    public void EnsureSelectedSeed()
    {
        if (SelectedCrop != null && SeedCount(SelectedCrop) > 0) return;
        foreach (var kv in Seeds)
        {
            if (kv.Key != null && kv.Value > 0)
            {
                SelectCrop(kv.Key);
                return;
            }
        }
    }

    public void SelectCrop(CropData crop)
    {
        SelectedCrop = crop;
        OnCropSelected?.Invoke(crop);
    }

    public bool OwnsTool(ToolData tool)
    {
        return tool != null && ownedTools.Contains(tool.name);
    }

    public void SelectTool(ToolData tool)
    {
        if (tool != null && !OwnsTool(tool))
        {
            Toast.Show($"Chưa có {tool.displayName}");
            return;
        }

        SelectedTool = tool;
        OnToolSelected?.Invoke(tool);
    }

    public bool TryBuyTool(ToolData tool)
    {
        if (tool == null) return false;
        if (OwnsTool(tool))
        {
            SelectTool(tool);
            return true;
        }

        if (tool.price > 0 && !TrySpend(tool.price)) return false;

        ownedTools.Add(tool.name);
        OnToolsOwnedChanged?.Invoke();
        SelectTool(tool);
        Toast.Show(tool.price > 0 ? $"Đã mua {tool.displayName}" : $"Đã nhận {tool.displayName}");
        AudioManager.Instance?.Play("sell");
        return true;
    }

    public void GrantFreeTools(ToolData[] tools)
    {
        if (tools == null) return;
        bool changed = false;
        foreach (var t in tools)
        {
            if (t == null || t.price > 0 || OwnsTool(t)) continue;
            ownedTools.Add(t.name);
            changed = true;
        }
        if (!changed) return;
        OnToolsOwnedChanged?.Invoke();
        if (SelectedTool == null)
        {
            foreach (var t in tools)
            {
                if (t != null && OwnsTool(t)) { SelectTool(t); break; }
            }
        }
    }

    public List<string> GetOwnedToolIds() => new(ownedTools);

    public void ApplyOwnedTools(IEnumerable<string> ids)
    {
        ownedTools.Clear();
        if (ids != null)
        {
            foreach (var id in ids)
            {
                if (!string.IsNullOrEmpty(id))
                    ownedTools.Add(id);
            }
        }

        if (SelectedTool != null && !OwnsTool(SelectedTool))
            SelectedTool = null;

        OnToolsOwnedChanged?.Invoke();
        OnToolSelected?.Invoke(SelectedTool);
    }

    public bool TrySpend(int amount)
    {
        if (Money < amount) { Toast.Show("Không đủ tiền!"); return false; }
        Money -= amount;
        OnMoneyChanged?.Invoke(Money);
        return true;
    }

    public void AddMoney(int amount)
    {
        Money += amount;
        OnMoneyChanged?.Invoke(Money);
    }

    public void AddHarvest(CropData crop)
    {
        if (crop == null) return;
        Harvested.TryGetValue(crop, out int n);
        Harvested[crop] = n + 1;
        OnInventoryChanged?.Invoke();
    }

    public int Count(CropData crop)
    {
        if (crop == null) return 0;
        Harvested.TryGetValue(crop, out int n);
        return n;
    }

    // Lấy 'amount' món ra khỏi kho (dùng cho đơn hàng)
    public bool TryTake(CropData crop, int amount)
    {
        if (crop == null || amount <= 0 || Count(crop) < amount) return false;
        int left = Harvested[crop] - amount;
        if (left > 0) Harvested[crop] = left;
        else Harvested.Remove(crop);
        OnInventoryChanged?.Invoke();
        return true;
    }

    // Giá bán đã tính hệ số mùa vụ
    public int GetSellPrice(CropData crop)
    {
        float m = WorldEnvironment.Instance != null
            ? WorldEnvironment.Instance.SellMultiplier(crop) : 1f;
        return Mathf.RoundToInt(crop.sellPrice * m);
    }

    // Bán 1 món trong kho
    public void SellOne(CropData crop)
    {
        SellAmount(crop, 1);
    }

    /// <summary>
    /// Bán số lượng tuỳ chọn của một nông sản trong kho
    /// </summary>
    public bool SellAmount(CropData crop, int amount)
    {
        if (crop == null || amount <= 0 || Count(crop) < amount)
        {
            Toast.Show("Chưa có đủ số lượng để bán");
            return false;
        }

        int price = GetSellPrice(crop);
        int total = price * amount;
        if (!TryTake(crop, amount)) return false;

        AddMoney(total);
        bool seasonBonus = price > crop.sellPrice;
        Toast.Show(seasonBonus
            ? $"Bán {amount} {crop.cropName}: +{total}$ (có giá mùa vụ)"
            : $"Bán {amount} {crop.cropName}: +{total}$");
        AudioManager.Instance?.Play("sell");
        return true;
    }

    // Gắn vào nút "Bán hết"
    public void SellAll()
    {
        int total = 0;
        bool seasonBonus = false;

        foreach (var kv in Harvested)
        {
            total += GetSellPrice(kv.Key) * kv.Value;
            if (GetSellPrice(kv.Key) > kv.Key.sellPrice) seasonBonus = true;
        }

        if (total == 0) { Toast.Show("Chưa có gì để bán"); return; }

        Harvested.Clear();
        AddMoney(total);
        OnInventoryChanged?.Invoke();
        Toast.Show(seasonBonus ? $"Bán được {total}$ (có giá mùa vụ)" : $"Bán được {total}$");
        AudioManager.Instance?.Play("sell");
    }

    // Gọi sau khi nạp save để UI cập nhật
    public void RefreshAll()
    {
        OnMoneyChanged?.Invoke(Money);
        OnInventoryChanged?.Invoke();
        OnCropSelected?.Invoke(SelectedCrop);
        OnToolsOwnedChanged?.Invoke();
        OnToolSelected?.Invoke(SelectedTool);
    }
}
