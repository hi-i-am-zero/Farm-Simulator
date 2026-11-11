using UnityEngine;

/// <summary>
/// Gắn vào NPC để tạo thành một Cửa hàng buôn bán.
/// Khi người chơi đến gần và nhấn phím Tương tác (E / Space), cửa hàng này sẽ mở ra.
/// </summary>
public class NpcShop : MonoBehaviour, IInteractable
{
    [Header("Thông Tin Cửa Hàng")]
    [SerializeField] string shopTitle = "Cửa hàng";
    [SerializeField] string greeting = "Chào bạn, bạn muốn mua gì hôm nay?";

    [Header("Danh Sách Món Hàng")]
    [SerializeField] ShopOffer[] offers = new ShopOffer[0];

    // Thuộc tính công khai để ShopUI đọc dữ liệu
    public string ShopTitle => shopTitle;
    public string Greeting => greeting;
    public ShopOffer[] Offers => offers;

    /// <summary>
    /// Kích hoạt khi Player đứng gần NPC và bấm phím tương tác
    /// </summary>
    public void Interact()
    {
        if (ShopUI.Instance == null)
        {
            Toast.Show("Chưa tìm thấy giao diện cửa hàng (ShopUI)!");
            return;
        }

        // Mở bảng cửa hàng với dữ liệu của NPC này
        ShopUI.Instance.Open(this);
    }
}
