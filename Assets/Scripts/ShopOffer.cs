using System;
using UnityEngine;

/// <summary>
/// Loại mặt hàng bán trong cửa hàng:
/// - Animal: Mua thêm vật nuôi (Gà, Bò, Cừu) vào chuồng.
/// - Seed: Mua hạt giống cây trồng vào túi đồ.
/// </summary>
public enum ShopOfferKind
{
    Animal,
    Seed
}

/// <summary>
/// Định nghĩa một mặt hàng cụ thể được bày bán trong cửa hàng của NPC.
/// </summary>
[Serializable]
public class ShopOffer
{
    [Tooltip("Nhãn hiển thị tuỳ chọn nếu muốn ghi đè")]
    public string label = "Mua";

    [Tooltip("Loại mặt hàng: Động vật hoặc Hạt giống")]
    public ShopOfferKind kind = ShopOfferKind.Animal;

    [Tooltip("Dùng khi bán Động vật (kết nối tới PlotUnlocker của con vật tương ứng)")]
    public PlotUnlocker animalShop;

    [Tooltip("Dùng khi bán Hạt giống (kết nối tới CropData của loại cây)")]
    public CropData seedCrop;
}
