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

    public const int HotbarSlotCount = 6;
    public readonly InventoryItem[] Hotbar = new InventoryItem[HotbarSlotCount];
    public int ActiveHotbarIndex { get; private set; } = 0;

    public event Action<int> OnMoneyChanged;
    public event Action<CropData> OnCropSelected;
    public event Action<ToolData> OnToolSelected;
    public event Action OnInventoryChanged;
    public event Action OnToolsOwnedChanged;
    public event Action OnSeedsChanged;
    public event Action OnHotbarChanged;

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

        InitializeDefaultHotbarAndTools();
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

    public InventoryItem GetActiveHotbarItem()
    {
        if (ActiveHotbarIndex >= 0 && ActiveHotbarIndex < HotbarSlotCount)
            return Hotbar[ActiveHotbarIndex];
        return null;
    }

    public void SelectHotbarSlot(int index)
    {
        if (index < 0 || index >= HotbarSlotCount) return;
        ActiveHotbarIndex = index;
        SyncActiveSelection();
        OnHotbarChanged?.Invoke();
    }

    public void AssignItemToHotbar(int hotbarIndex, InventoryItem newItem)
    {
        if (hotbarIndex < 0 || hotbarIndex >= HotbarSlotCount) return;

        // Nếu món này đã có ở 1 ô Hotbar khác, ta xóa ô cũ đó để tránh trùng lặp
        for (int i = 0; i < HotbarSlotCount; i++)
        {
            if (i != hotbarIndex && IsSameItem(Hotbar[i], newItem))
            {
                Hotbar[i] = new InventoryItem();
            }
        }

        Hotbar[hotbarIndex] = newItem != null ? newItem.Clone() : new InventoryItem();
        SyncActiveSelection();
        OnHotbarChanged?.Invoke();
        OnInventoryChanged?.Invoke();
    }

    public void ClearHotbarSlot(int hotbarIndex)
    {
        if (hotbarIndex < 0 || hotbarIndex >= HotbarSlotCount) return;
        Hotbar[hotbarIndex] = new InventoryItem();
        SyncActiveSelection();
        OnHotbarChanged?.Invoke();
        OnInventoryChanged?.Invoke();
    }

    public bool IsSameItem(InventoryItem a, InventoryItem b)
    {
        if (a == null || b == null || a.IsEmpty || b.IsEmpty) return false;
        if (a.kind != b.kind) return false;
        if (a.kind == ItemKind.Tool) return a.tool == b.tool;
        return a.crop == b.crop;
    }

    public int GetHotbarSlotOf(InventoryItem item)
    {
        if (item == null || item.IsEmpty) return -1;
        for (int i = 0; i < HotbarSlotCount; i++)
        {
            if (IsSameItem(Hotbar[i], item)) return i;
        }
        return -1;
    }

    public ToolData GetToolByType(ToolType type)
    {
        var tb = ToolsToolbar.Instance ?? FindAnyObjectByType<ToolsToolbar>(FindObjectsInactive.Include);
        if (tb != null && tb.Tools != null)
        {
            foreach (var t in tb.Tools)
                if (t != null && t.type == type) return t;
        }
        return null;
    }

    public void InitializeDefaultHotbarAndTools()
    {
        ownedTools.Add("Hand");

        for (int i = 0; i < HotbarSlotCount; i++)
        {
            if (Hotbar[i] == null)
                Hotbar[i] = new InventoryItem();
        }

        // Nếu đã sở hữu Cuốc (từ save), gán vào ô 0
        var hoe = GetToolByType(ToolType.Hoe);
        if (hoe != null && OwnsTool(hoe) && Hotbar[0].IsEmpty)
        {
            Hotbar[0] = InventoryItem.FromTool(hoe);
        }

        // Nếu đã sở hữu Bình tưới (từ save), gán vào ô 1
        var water = GetToolByType(ToolType.WateringCan);
        if (water != null && OwnsTool(water) && Hotbar[1].IsEmpty)
        {
            Hotbar[1] = InventoryItem.FromTool(water);
        }

        // Hạt giống ban đầu (nếu có defaultCrop và chưa có hạt)
        if (defaultCrop != null && SeedCount(defaultCrop) == 0)
        {
            Seeds[defaultCrop] = 5;
            for (int i = 0; i < HotbarSlotCount; i++)
            {
                if (Hotbar[i].IsEmpty)
                {
                    Hotbar[i] = InventoryItem.FromSeed(defaultCrop, Seeds[defaultCrop]);
                    break;
                }
            }
        }

        SelectHotbarSlot(0);
        OnHotbarChanged?.Invoke();
    }

    public void SyncActiveSelection()
    {
        var item = GetActiveHotbarItem();
        if (item == null || item.IsEmpty)
        {
            SelectedTool = null;
            SelectedCrop = null;
        }
        else if (item.kind == ItemKind.Tool)
        {
            SelectedTool = item.tool;
            SelectedCrop = null;
        }
        else if (item.kind == ItemKind.Seed)
        {
            SelectedCrop = item.crop;
            SelectedTool = GetToolByType(ToolType.Seed);
        }
        else
        {
            SelectedTool = null;
            SelectedCrop = item.crop;
        }

        OnToolSelected?.Invoke(SelectedTool);
        OnCropSelected?.Invoke(SelectedCrop);
    }

    public bool TryBuySeed(CropData crop)
    {
        if (crop == null) return false;
        if (!TrySpend(crop.seedPrice)) return false;
        Seeds.TryGetValue(crop, out int n);
        Seeds[crop] = n + 1;

        // Cập nhật số lượng trên các ô Hotbar
        bool onHotbar = false;
        for (int i = 0; i < HotbarSlotCount; i++)
        {
            if (Hotbar[i] != null && Hotbar[i].kind == ItemKind.Seed && Hotbar[i].crop == crop)
            {
                Hotbar[i].count = Seeds[crop];
                onHotbar = true;
                break;
            }
        }

        // Nếu chưa có trên Hotbar, tự động đặt vào ô trống đầu tiên
        if (!onHotbar)
        {
            for (int i = 0; i < HotbarSlotCount; i++)
            {
                if (Hotbar[i] == null || Hotbar[i].IsEmpty)
                {
                    Hotbar[i] = InventoryItem.FromSeed(crop, Seeds[crop]);
                    break;
                }
            }
        }

        SyncActiveSelection();
        OnSeedsChanged?.Invoke();
        OnHotbarChanged?.Invoke();
        OnInventoryChanged?.Invoke();
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

        for (int i = 0; i < HotbarSlotCount; i++)
        {
            if (Hotbar[i] != null && Hotbar[i].kind == ItemKind.Seed && Hotbar[i].crop == crop)
            {
                Hotbar[i].count = left;
                if (left <= 0)
                    Hotbar[i] = new InventoryItem();
            }
        }

        SyncActiveSelection();
        OnSeedsChanged?.Invoke();
        OnHotbarChanged?.Invoke();
        OnInventoryChanged?.Invoke();
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

    /// <summary>
    /// Chuyển đổi xoay vòng qua các loại hạt giống đang có trong túi đồ
    /// </summary>
    public void CycleNextSeed()
    {
        var available = new List<CropData>();
        foreach (var kv in Seeds)
        {
            if (kv.Key != null && kv.Value > 0)
                available.Add(kv.Key);
        }

        if (available.Count == 0)
        {
            Toast.Show("Túi đã hết hạt giống! Hãy mua ở NPC trồng cây");
            return;
        }

        if (available.Count == 1)
        {
            SelectCrop(available[0]);
            Toast.Show($"Hạt {available[0].cropName} (Túi: {SeedCount(available[0])} hạt)");
            return;
        }

        int currIndex = available.IndexOf(SelectedCrop);
        int nextIndex = (currIndex + 1) % available.Count;
        var nextCrop = available[nextIndex];
        SelectCrop(nextCrop);
        Toast.Show($"Đã chọn: Hạt {nextCrop.cropName} (Túi: {SeedCount(nextCrop)} hạt)");
        AudioManager.Instance?.Play("click");
    }

    public void SelectCrop(CropData crop)
    {
        SelectedCrop = crop;
        OnCropSelected?.Invoke(crop);
    }

    public bool OwnsTool(ToolData tool)
    {
        if (tool == null) return false;
        if (tool.type == ToolType.Hand) return true;
        return ownedTools.Contains(tool.name);
    }

    public bool OwnsTool(string toolName)
    {
        if (string.IsNullOrEmpty(toolName)) return true;
        if (toolName.Equals("Hand", StringComparison.OrdinalIgnoreCase)) return true;
        return ownedTools.Contains(toolName);
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
            Toast.Show($"Bạn đã sở hữu {tool.displayName} rồi!");
            return false;
        }

        if (!TrySpend(tool.price))
        {
            Toast.Show("Không đủ tiền mua công cụ!");
            return false;
        }

        ownedTools.Add(tool.name);

        // Tự động gán vào ô trống đầu tiên trên thanh công cụ Hotbar
        for (int i = 0; i < HotbarSlotCount; i++)
        {
            if (Hotbar[i] == null || Hotbar[i].IsEmpty)
            {
                Hotbar[i] = InventoryItem.FromTool(tool);
                SelectHotbarSlot(i);
                break;
            }
        }

        SyncActiveSelection();
        OnToolsOwnedChanged?.Invoke();
        OnHotbarChanged?.Invoke();
        OnInventoryChanged?.Invoke();
        Toast.Show($"Đã mua {tool.displayName}! Đã gán vào thanh công cụ.");
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
