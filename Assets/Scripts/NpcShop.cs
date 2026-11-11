using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Gắn vào NPC để tạo thành một Cửa hàng buôn bán:
/// - MUA: Người chơi mua từ NPC (Hạt giống, Động vật...)
/// - BÁN: NPC thu mua sản lượng từ người chơi (Nông sản, Trứng, Sữa, Lông cừu...)
/// Khi người chơi đến gần và nhấn phím Tương tác (E / Space), cửa hàng này sẽ mở ra.
/// </summary>
public class NpcShop : MonoBehaviour, IInteractable
{
    [Header("Thông Tin Cửa Hàng")]
    [SerializeField] string shopTitle = "Cửa hàng";
    [SerializeField] string greeting = "Chào bạn! Bạn muốn mua hay bán gì hôm nay?";

    [Header("Danh Sách Mua (Người chơi mua từ NPC)")]
    [SerializeField] ShopOffer[] offers = new ShopOffer[0];
    [SerializeField] ShopOffer[] buyOffers = new ShopOffer[0];

    [Header("Danh Sách Bán (NPC thu mua từ Người chơi)")]
    [SerializeField] CropData[] sellCrops = new CropData[0];

    // Thuộc tính công khai để ShopUI đọc dữ liệu
    public string ShopTitle => shopTitle;
    public string Greeting => greeting;
    public ShopOffer[] BuyOffers => (buyOffers != null && buyOffers.Length > 0) ? buyOffers : offers;

    public CropData[] SellCrops
    {
        get
        {
            if (sellCrops != null && sellCrops.Length > 0)
                return sellCrops;

            // Tự động tìm sản lượng mặc định theo tên NPC nếu chưa gán trong Inspector
            return GetDefaultSellCropsByName(gameObject.name);
        }
    }

    /// <summary>
    /// Cập nhật toàn bộ thông tin cửa hàng NPC trực tiếp từ script khởi tạo.
    /// </summary>
    public void Configure(string title, string shopGreeting, ShopOffer[] buy, CropData[] sell)
    {
        shopTitle = title;
        greeting = shopGreeting;
        offers = buy ?? new ShopOffer[0];
        buyOffers = offers;
        sellCrops = sell ?? new CropData[0];
    }

    /// <summary>
    /// Kích hoạt khi Player đứng gần NPC và bấm phím tương tác
    /// </summary>
    public void Interact()
    {
        if (Time.timeScale == 0f) return;

        var shopUi = ShopUI.Instance;
        if (shopUi == null)
            shopUi = FindAnyObjectByType<ShopUI>(FindObjectsInactive.Include);

        if (shopUi == null)
        {
            Toast.Show("Chưa tìm thấy giao diện cửa hàng (ShopUI)!");
            Debug.LogWarning("[NpcShop] Không tìm thấy ShopUI trong cảnh.");
            return;
        }

        if (shopUi.IsPanelVisible) return;

        // Mở bảng cửa hàng với dữ liệu của NPC này
        shopUi.Open(this);
    }

    void OnMouseDown()
    {
        // Không nhận click chuột xuyên qua giao diện UI đang mở (nút Mua nằm phía trên collider NPC)
        if (Time.timeScale == 0f || ShopUI.IsOpen) return;
        var es = EventSystem.current;
        if (es != null && es.IsPointerOverGameObject()) return;

        // Cho phép click chuột trực tiếp vào NPC khi đứng gần
        var playerCtrl = FindAnyObjectByType<PlayerController>();
        var player = playerCtrl != null ? playerCtrl.gameObject : GameObject.Find("Player");
        if (player != null && Vector2.Distance(player.transform.position, transform.position) <= 4.0f)
        {
            Interact();
        }
    }

    /// <summary>
    /// Tìm sản lượng tương ứng theo tên NPC (Gà -> Trứng, Bò -> Sữa, Cừu -> Lông, Cây -> 4 Nông sản)
    /// </summary>
    static CropData[] GetDefaultSellCropsByName(string npcName)
    {
        string lower = npcName.ToLowerInvariant();
        var list = new List<CropData>();

        if (lower.Contains("chick")) AddCropIfFound(list, "Trứng", "Egg");
        else if (lower.Contains("cow")) AddCropIfFound(list, "Sữa bò", "Milk");
        else if (lower.Contains("sheep")) AddCropIfFound(list, "Lông cừu", "Wool");
        else if (lower.Contains("seed") || lower.Contains("crop"))
        {
            AddCropIfFound(list, "Lúa", "Rice");
            AddCropIfFound(list, "Đậu", "Bean", "Carrot");
            AddCropIfFound(list, "Ngô", "Bắp", "Corn");
            AddCropIfFound(list, "Cà chua", "Tomato");
        }

        return list.ToArray();
    }

    static void AddCropIfFound(List<CropData> list, params string[] searchNames)
    {
        var found = FindCrop(searchNames);
        if (found != null) list.Add(found);
    }

    static CropData FindCrop(params string[] searchNames)
    {
        var allCrops = Resources.FindObjectsOfTypeAll<CropData>();
        foreach (var c in allCrops)
        {
            if (c == null) continue;
            foreach (var name in searchNames)
            {
                if (c.cropName.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    c.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return c;
                }
            }
        }
        return null;
    }
}
