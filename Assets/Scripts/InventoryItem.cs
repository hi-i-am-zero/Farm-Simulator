using System;
using UnityEngine;

public enum ItemKind
{
    Empty,
    Tool,
    Seed,
    Harvested
}

/// <summary>
/// Đại diện cho 1 vật phẩm bất kỳ trong game (Công cụ, Hạt giống, Nông sản)
/// Có thể chuyển qua lại tự do giữa Túi đồ (Inventory) và Thanh công cụ (Hotbar).
/// </summary>
[Serializable]
public class InventoryItem
{
    public ItemKind kind = ItemKind.Empty;
    public ToolData tool;
    public CropData crop;
    public int count = 1;

    public bool IsEmpty =>
        kind == ItemKind.Empty ||
        (kind == ItemKind.Tool && tool == null) ||
        (kind != ItemKind.Tool && (crop == null || count <= 0));

    public string Name
    {
        get
        {
            if (IsEmpty) return "";
            if (kind == ItemKind.Tool) return tool != null ? tool.displayName : "";
            if (kind == ItemKind.Seed) return crop != null ? $"Hạt {crop.cropName}" : "";
            return crop != null ? crop.cropName : "";
        }
    }

    public Sprite Icon
    {
        get
        {
            if (IsEmpty) return null;
            if (kind == ItemKind.Tool) return tool != null ? tool.icon : null;
            if (kind == ItemKind.Seed)
            {
                if (crop != null && crop.seedIcon != null) return crop.seedIcon;
                var seedTool = GameManager.Instance != null ? GameManager.Instance.GetToolByType(ToolType.Seed) : null;
                if (seedTool != null && seedTool.icon != null) return seedTool.icon;
            }
            return crop != null ? crop.icon : null;
        }
    }

    public static InventoryItem FromTool(ToolData t)
    {
        if (t == null) return new InventoryItem();
        return new InventoryItem { kind = ItemKind.Tool, tool = t, count = 1 };
    }

    public static InventoryItem FromSeed(CropData c, int count)
    {
        if (c == null || count <= 0) return new InventoryItem();
        return new InventoryItem { kind = ItemKind.Seed, crop = c, count = count };
    }

    public static InventoryItem FromHarvested(CropData c, int count)
    {
        if (c == null || count <= 0) return new InventoryItem();
        return new InventoryItem { kind = ItemKind.Harvested, crop = c, count = count };
    }

    public InventoryItem Clone()
    {
        return new InventoryItem
        {
            kind = this.kind,
            tool = this.tool,
            crop = this.crop,
            count = this.count
        };
    }
}

