using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý giao diện cửa hàng (Shop UI):
/// - Hiển thị bảng cửa hàng khi nói chuyện với NPC.
/// - Tự động tạo danh sách các nút mua (Hạt giống hoặc Động vật).
/// - Xử lý trừ tiền, cộng hạt giống/con vật khi bấm mua.
/// - Đóng bảng khi bấm nút X, phím ESC hoặc phím Q.
/// </summary>
public class ShopUI : MonoBehaviour
{
    // ==========================================
    // 1. SINGLETON & TRẠNG THÁI TOÀN CỤC
    // ==========================================
    public static ShopUI Instance { get; private set; }
    public static bool IsOpen { get; private set; }

    // ==========================================
    // 2. CẤU HÌNH GIAO DIỆN TRONG INSPECTOR
    // ==========================================
    [Header("Các Thành Phần Giao Diện (UI)")]
    [SerializeField] GameObject panel;              // Bảng panel cửa hàng chính
    [SerializeField] TMP_Text titleText;            // Tiêu đề cửa hàng
    [SerializeField] TMP_Text greetingText;         // Lời chào của NPC
    [SerializeField] Transform offerRoot;           // Nơi chứa các nút mua hàng
    [SerializeField] Button offerButtonPrefab;      // Mẫu prefab nút mua hàng
    [SerializeField] Button closeButton;            // Nút bấm đóng cửa hàng (dấu X)

    NpcShop currentShop;                            // NPC Shop đang mở hiện tại
    readonly List<GameObject> spawnedButtons = new(); // Danh sách các nút mua đã tạo

    // ==========================================
    // 3. VÒNG ĐỜI KHỞI TẠO (AWAKE & DESTROY)
    // ==========================================
    void Awake()
    {
        Instance = this;
        IsOpen = false;

        if (panel != null) panel.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ==========================================
    // 4. PHÍM TẮT ĐÓNG SHOP (ESC / Q)
    // ==========================================
    void Update()
    {
        if (!IsOpen) return;

        // Cho phép bấm ESC hoặc Q để đóng nhanh cửa hàng
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Q))
        {
            Close();
        }
    }

    // ==========================================
    // 5. MỞ & ĐÓNG CỬA HÀNG
    // ==========================================
    /// <summary>
    /// Mở cửa hàng với dữ liệu từ NPC truyền vào
    /// </summary>
    public void Open(NpcShop shop)
    {
        if (shop == null || panel == null) return;

        currentShop = shop;
        IsOpen = true;
        panel.SetActive(true);

        // Đảm bảo thông báo Toast nổi lên trên cùng để người chơi luôn nhìn thấy
        BringToastToFront();

        // Cập nhật tiêu đề và lời thoại của NPC
        if (titleText != null) titleText.text = shop.ShopTitle;
        if (greetingText != null) greetingText.text = shop.Greeting;

        // Sinh danh sách các mặt hàng của NPC này
        RebuildOffers();

        AudioManager.Instance?.Play("click");
    }

    /// <summary>
    /// Đóng cửa hàng và dọn dẹp các nút bấm
    /// </summary>
    public void Close()
    {
        IsOpen = false;
        currentShop = null;

        if (panel != null) panel.SetActive(false);
        ClearOffers();
    }

    // ==========================================
    // 6. XÂY DỰNG DANH SÁCH MẶT HÀNG (OFFERS)
    // ==========================================
    void RebuildOffers()
    {
        ClearOffers();
        if (currentShop == null || offerRoot == null || offerButtonPrefab == null) return;

        foreach (var offer in currentShop.Offers)
        {
            if (offer == null) continue;

            // Tạo nút bấm từ prefab mẫu
            var btn = Instantiate(offerButtonPrefab, offerRoot);
            btn.gameObject.SetActive(true);

            // Gán chữ hiển thị giá tiền và thông tin mặt hàng
            var label = btn.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.text = GetOfferLabel(offer);
            }

            // Gắn sự kiện khi bấm nút mua
            var capturedOffer = offer;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnBuyItem(capturedOffer));

            spawnedButtons.Add(btn.gameObject);
        }
    }

    /// <summary>
    /// Tạo chuỗi văn bản hiển thị trên nút mua (Tên món + Giá tiền + Tồn kho)
    /// </summary>
    string GetOfferLabel(ShopOffer offer)
    {
        // 1. Mặt hàng Hạt Giống: Hiển thị giá và số hạt đang có trong túi
        if (offer.kind == ShopOfferKind.Seed && offer.seedCrop != null)
        {
            int currentSeeds = GameManager.Instance != null ? GameManager.Instance.SeedCount(offer.seedCrop) : 0;
            return $"Mua hạt {offer.seedCrop.cropName} ({offer.seedCrop.seedPrice}$)  [Trong túi: {currentSeeds}]";
        }

        // 2. Mặt hàng Động Vật (Gà, Bò, Cừu): Hiển thị giá nâng cấp hoặc thông báo hết chuồng
        if (offer.kind == ShopOfferKind.Animal && offer.animalShop != null)
        {
            if (offer.animalShop.IsSoldOut)
                return $"Đã đạt tối đa số lượng {offer.animalShop.ItemName}";

            return $"Mua {offer.animalShop.ItemName} ({offer.animalShop.NextCost}$)";
        }

        // 3. Nhãn mặc định
        return string.IsNullOrEmpty(offer.label) ? "Mua" : offer.label;
    }

    // ==========================================
    // 7. XỬ LÝ MUA HÀNG (ON BUY)
    // ==========================================
    void OnBuyItem(ShopOffer offer)
    {
        if (offer == null || GameManager.Instance == null) return;

        bool buySuccess = false;

        // Xử lý mua Hạt giống
        if (offer.kind == ShopOfferKind.Seed && offer.seedCrop != null)
        {
            buySuccess = GameManager.Instance.TryBuySeed(offer.seedCrop);
        }
        // Xử lý mua Động vật
        else if (offer.kind == ShopOfferKind.Animal && offer.animalShop != null)
        {
            buySuccess = offer.animalShop.TryBuy();
        }

        // Nếu mua thành công -> Cập nhật lại nhãn các nút (cập nhật số lượng mới trong túi và giá mới)
        if (buySuccess)
        {
            RebuildOffers();
        }
    }

    /// <summary>
    /// Xóa toàn bộ các nút mua đã sinh ra trước đó
    /// </summary>
    void ClearOffers()
    {
        foreach (var btn in spawnedButtons)
        {
            if (btn != null) Destroy(btn);
        }
        spawnedButtons.Clear();
    }

    /// <summary>
    /// Đảm bảo Toast thông báo luôn nằm trên cùng của Canvas
    /// </summary>
    static void BringToastToFront()
    {
        var toast = GameObject.Find("ToastRoot");
        var shop = GameObject.Find("ShopPanel");
        if (toast == null || shop == null) return;

        shop.transform.SetAsLastSibling();
        toast.transform.SetAsLastSibling();
    }
}
