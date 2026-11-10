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
    readonly HashSet<string> ownedTools = new();

    public event Action<int> OnMoneyChanged;
    public event Action<CropData> OnCropSelected;
    public event Action<ToolData> OnToolSelected;
    public event Action OnInventoryChanged;
    public event Action OnToolsOwnedChanged;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Money = startMoney;
        SelectedCrop = defaultCrop;
        SelectedTool = null;
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

        if (!TrySpend(tool.price)) return false;

        ownedTools.Add(tool.name);
        OnToolsOwnedChanged?.Invoke();
        SelectTool(tool);
        Toast.Show($"Đã mua {tool.displayName}");
        AudioManager.Instance?.Play("sell");
        return true;
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
        if (crop == null || Count(crop) <= 0)
        {
            Toast.Show("Chưa có gì để bán");
            return;
        }

        int price = GetSellPrice(crop);
        if (!TryTake(crop, 1)) return;

        AddMoney(price);
        bool seasonBonus = price > crop.sellPrice;
        Toast.Show(seasonBonus
            ? $"Ban 1 {crop.cropName}: +{price}$ (co gia mua vu)"
            : $"Ban 1 {crop.cropName}: +{price}$");
        AudioManager.Instance?.Play("sell");
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
