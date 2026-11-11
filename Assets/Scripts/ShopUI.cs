using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Quản lý giao diện Cửa hàng NPC (ShopUI):
/// - Tab MUA: Động vật (10 suất tăng giá dần) và Hạt giống cây trồng.
/// - Tab BÁN: Nông sản / sản phẩm chăn nuôi kèm bộ chọn [-], [+], Bán X, Bán hết.
/// </summary>
public class ShopUI : MonoBehaviour
{
    public static ShopUI Instance { get; private set; }
    public static bool IsOpen { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Instance = null;
        IsOpen = false;
    }

    public enum ShopTab
    {
        Buy,
        Sell
    }

    [Header("Thành Phần Chính")]
    [SerializeField] GameObject panel;
    [SerializeField] TMP_Text titleText;
    [SerializeField] TMP_Text greetingText;
    [SerializeField] TMP_Text moneyText;
    [SerializeField] Button closeButton;

    [Header("Tab Mua / Bán")]
    [SerializeField] Button buyTabButton;
    [SerializeField] Button sellTabButton;
    [SerializeField] TMP_Text buyTabText;
    [SerializeField] TMP_Text sellTabText;

    [Header("Khu Vực Danh Sách")]
    [SerializeField] Transform contentRoot;
    [SerializeField] ScrollRect scrollRect;

    NpcShop currentShop;
    ShopTab currentTab = ShopTab.Buy;
    readonly List<GameObject> spawnedItems = new();
    readonly Dictionary<CropData, int> selectedSellAmounts = new();

    static readonly Color ColorActiveTab = new(0.12f, 0.48f, 0.22f, 1f);
    static readonly Color ColorInactiveTab = new(0.28f, 0.22f, 0.18f, 0.95f);
    static readonly Color ColorRowBg = new(0.14f, 0.16f, 0.20f, 0.95f);
    static readonly Color ColorBtnGreen = new(0.13f, 0.58f, 0.28f, 1f);
    static readonly Color ColorBtnWarn = new(0.55f, 0.25f, 0.22f, 1f);
    static readonly Color ColorBtnOrange = new(0.85f, 0.45f, 0.08f, 1f);
    static readonly Color ColorBtnAdjust = new(0.25f, 0.28f, 0.35f, 1f);

    public bool IsPanelVisible => IsOpen && (panel == null || panel.activeInHierarchy);
    int CurrentMoney => GameManager.Instance != null ? GameManager.Instance.Money : 0;

    void Awake()
    {
        Instance = this;
        IsOpen = false;

        if (panel != null) panel.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (buyTabButton != null) buyTabButton.onClick.AddListener(() => SetTab(ShopTab.Buy));
        if (sellTabButton != null) sellTabButton.onClick.AddListener(() => SetTab(ShopTab.Sell));
    }

    void Start()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.OnMoneyChanged += UpdateMoneyDisplay;
        GameManager.Instance.OnInventoryChanged += OnInventoryUpdated;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            IsOpen = false;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnMoneyChanged -= UpdateMoneyDisplay;
            GameManager.Instance.OnInventoryChanged -= OnInventoryUpdated;
        }
    }

    void Update()
    {
        if (!IsOpen) return;

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Q))
            Close();
    }

    public void Open(NpcShop shop)
    {
        if (shop == null) return;

        EnsureUiBuilt();
        NormalizeShopPanelLayout();
        ClearUiSelection();

        currentShop = shop;
        IsOpen = true;
        currentTab = ShopTab.Buy;
        selectedSellAmounts.Clear();

        if (panel != null) panel.SetActive(true);
        BringToastToFront();

        if (titleText != null) titleText.text = shop.ShopTitle;
        if (greetingText != null) greetingText.text = shop.Greeting;

        UpdateMoneyDisplay(CurrentMoney);
        UpdateTabButtonsVisual();
        RebuildContent(true);

        if (AudioManager.Instance != null) AudioManager.Instance.Play("click");
    }

    public void Close()
    {
        IsOpen = false;
        currentShop = null;

        if (panel != null) panel.SetActive(false);
        ClearSpawnedItems();
    }

    public void SetTab(ShopTab tab)
    {
        if (currentTab == tab && spawnedItems.Count > 0) return;

        currentTab = tab;
        UpdateTabButtonsVisual();
        RebuildContent(true);
        if (AudioManager.Instance != null) AudioManager.Instance.Play("click");
    }

    void UpdateTabButtonsVisual()
    {
        SetTabButtonStyle(buyTabButton, buyTabText, currentTab == ShopTab.Buy);
        SetTabButtonStyle(sellTabButton, sellTabText, currentTab == ShopTab.Sell);
    }

    static void SetTabButtonStyle(Button btn, TMP_Text label, bool active)
    {
        if (btn != null && btn.TryGetComponent(out Image img))
            img.color = active ? ColorActiveTab : ColorInactiveTab;
        if (label != null)
            label.fontStyle = active ? FontStyles.Bold : FontStyles.Normal;
    }

    void UpdateMoneyDisplay(int money)
    {
        if (moneyText != null)
            moneyText.text = $"Tiền túi: <color=#FFD700><b>{money}$</b></color>";
    }

    void OnInventoryUpdated()
    {
        if (!IsOpen || currentShop == null) return;
        RebuildContent(false);
    }

    void RebuildContent(bool resetScroll = true)
    {
        ClearUiSelection();
        ClearSpawnedItems();

        if (contentRoot != null)
        {
            // Tách và ẩn ngay lập tức để Destroy cuối frame không làm sai bố cục hiện tại
            for (int i = contentRoot.childCount - 1; i >= 0; i--)
            {
                var child = contentRoot.GetChild(i);
                if (child == null) continue;
                child.gameObject.SetActive(false);
                child.SetParent(null, false);
            }
        }

        if (currentShop == null || contentRoot == null) return;

        if (currentTab == ShopTab.Buy) RebuildBuyOffers();
        else RebuildSellOffers();

        RefreshLayoutAndScroll(resetScroll);
    }

    void RefreshLayoutAndScroll(bool resetScroll)
    {
        if (contentRoot is RectTransform contentRt)
        {
            Canvas.ForceUpdateCanvases();
            if (contentRt.parent is RectTransform parentRt)
                LayoutRebuilder.ForceRebuildLayoutImmediate(parentRt);
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRt);
            Canvas.ForceUpdateCanvases();

            if (resetScroll && scrollRect != null)
            {
                scrollRect.velocity = Vector2.zero;
                contentRt.anchoredPosition = Vector2.zero;
                scrollRect.verticalNormalizedPosition = 1f;
                Canvas.ForceUpdateCanvases();
            }
        }
        else
        {
            Canvas.ForceUpdateCanvases();
        }
    }

    // ==========================================
    // NỘI DUNG TAB MUA
    // ==========================================
    void RebuildBuyOffers()
    {
        var offers = currentShop.BuyOffers;
        if (offers == null || offers.Length == 0)
        {
            CreateEmptyNotice("Cửa hàng này hiện không bán món nào.");
            return;
        }

        foreach (var offer in offers)
        {
            if (offer == null) continue;

            if (offer.kind == ShopOfferKind.Animal && offer.animalShop != null)
                RebuildAnimalBuySlots(offer.animalShop);
            else if (offer.kind == ShopOfferKind.Seed && offer.seedCrop != null)
                RebuildSeedBuySlot(offer.seedCrop);
        }
    }

    void RebuildAnimalBuySlots(PlotUnlocker unlocker)
    {
        if (unlocker == null) return;

        int totalSlots = Mathf.Max(10, unlocker.LockedCount);
        int playerMoney = CurrentMoney;

        for (int i = 0; i < totalSlots; i++)
        {
            int slotIndex = i;
            int cost = unlocker.GetCostForIndex(slotIndex);
            bool isOwned = slotIndex < unlocker.UnlockedCount;
            bool isNextToBuy = slotIndex == unlocker.UnlockedCount;
            bool canAfford = playerMoney >= cost;

            var row = CreateItemRow();
            string priceHex = (!isOwned && isNextToBuy && !canAfford) ? "#F87171" : "#FFD700";
            string slotLabel = $"<b>{unlocker.ItemName} #{slotIndex + 1}</b>  -  Giá: <color={priceHex}><b>{cost}$</b></color>";
            CreateText(row.transform, slotLabel, 20, TextAlignmentOptions.Left, new Vector2(280, 48));

            if (isOwned)
            {
                CreateText(row.transform, "<color=#F87171><b>ĐÃ BÁN</b></color>", 19, TextAlignmentOptions.Right, new Vector2(160, 40));
            }
            else if (isNextToBuy)
            {
                Color btnColor = canAfford ? ColorBtnGreen : ColorBtnWarn;
                var buyBtn = CreateSimpleButton(row.transform, $"Mua ({cost}$)", btnColor, new Vector2(160, 42));
                var capturedUnlocker = unlocker;
                buyBtn.onClick.AddListener(() => HandleBuyAnimalClick(capturedUnlocker));
            }
            else
            {
                CreateText(row.transform, $"<color=#64748B>(Cần mua con #{slotIndex})</color>", 17, TextAlignmentOptions.Right, new Vector2(160, 40));
            }
        }
    }

    void HandleBuyAnimalClick(PlotUnlocker unlocker)
    {
        if (unlocker == null)
        {
            Toast.Show("Lỗi dữ liệu cửa hàng vật nuôi!");
            return;
        }

        if (unlocker.TryBuy())
        {
            UpdateMoneyDisplay(CurrentMoney);
            RebuildContent(false);
        }
    }

    void RebuildSeedBuySlot(CropData crop)
    {
        int ownedSeeds = GameManager.Instance != null ? GameManager.Instance.SeedCount(crop) : 0;
        var row = CreateItemRow();

        string desc = $"<b>Hạt {crop.cropName}</b>  -  Giá: <color=#FFD700><b>{crop.seedPrice}$</b></color>\n<size=17><color=#CBD5E1>Trong túi: <b>{ownedSeeds}</b> hạt</color></size>";
        CreateText(row.transform, desc, 20, TextAlignmentOptions.Left, new Vector2(310, 48));

        var buyBtn = CreateSimpleButton(row.transform, "Mua 1 hạt", ColorBtnGreen, new Vector2(130, 42));
        var capturedCrop = crop;
        buyBtn.onClick.AddListener(() =>
        {
            if (GameManager.Instance != null && GameManager.Instance.TryBuySeed(capturedCrop))
            {
                UpdateMoneyDisplay(CurrentMoney);
                RebuildContent(false);
            }
        });
    }

    // ==========================================
    // NỘI DUNG TAB BÁN
    // ==========================================
    void RebuildSellOffers()
    {
        var crops = currentShop.SellCrops;
        if (crops == null || crops.Length == 0)
        {
            CreateEmptyNotice("NPC này hiện không thu mua sản phẩm nào.");
            return;
        }

        foreach (var crop in crops)
        {
            if (crop == null) continue;

            int owned = GameManager.Instance != null ? GameManager.Instance.Count(crop) : 0;
            int unitPrice = GameManager.Instance != null ? GameManager.Instance.GetSellPrice(crop) : crop.sellPrice;

            if (!selectedSellAmounts.TryGetValue(crop, out int chosenQty) || chosenQty <= 0)
                chosenQty = 1;
            if (owned > 0 && chosenQty > owned)
                chosenQty = owned;
            selectedSellAmounts[crop] = chosenQty;

            var row = CreateItemRow();
            string infoText = $"<b>{crop.cropName}</b>  -  Giá thu: <color=#FFD700><b>{unitPrice}$</b></color>\n<size=17><color=#CBD5E1>Đang có: <b>{owned}</b></color></size>";
            CreateText(row.transform, infoText, 20, TextAlignmentOptions.Left, new Vector2(190, 48));

            if (owned <= 0)
            {
                CreateText(row.transform, "<color=#64748B><i>(Chưa có trong kho)</i></color>", 18, TextAlignmentOptions.Right, new Vector2(250, 40));
                continue;
            }

            var capturedCrop = crop;

            var btnMinus = CreateSimpleButton(row.transform, "-", ColorBtnAdjust, new Vector2(36, 36));
            btnMinus.onClick.AddListener(() => AdjustSellAmount(capturedCrop, -1, owned));

            CreateText(row.transform, $"<b>{chosenQty}</b>", 21, TextAlignmentOptions.Center, new Vector2(40, 36));

            var btnPlus = CreateSimpleButton(row.transform, "+", ColorBtnAdjust, new Vector2(36, 36));
            btnPlus.onClick.AddListener(() => AdjustSellAmount(capturedCrop, +1, owned));

            var btnSellChosen = CreateSimpleButton(row.transform, $"Bán {chosenQty} (+{chosenQty * unitPrice}$)", ColorBtnGreen, new Vector2(125, 40));
            btnSellChosen.onClick.AddListener(() => SellCropAndRefresh(capturedCrop, chosenQty));

            var btnSellAll = CreateSimpleButton(row.transform, $"Hết (+{owned * unitPrice}$)", ColorBtnOrange, new Vector2(95, 40));
            btnSellAll.onClick.AddListener(() => SellCropAndRefresh(capturedCrop, owned));
        }
    }

    void AdjustSellAmount(CropData crop, int delta, int maxOwned)
    {
        int current = selectedSellAmounts.TryGetValue(crop, out int val) ? val : 1;
        int next = Mathf.Clamp(current + delta, 1, maxOwned);
        if (next == current) return;

        selectedSellAmounts[crop] = next;
        RebuildContent(false);
        if (AudioManager.Instance != null) AudioManager.Instance.Play("click");
    }

    void SellCropAndRefresh(CropData crop, int amount)
    {
        if (GameManager.Instance == null || !GameManager.Instance.SellAmount(crop, amount)) return;

        selectedSellAmounts[crop] = 1;
        UpdateMoneyDisplay(CurrentMoney);
        RebuildContent(false);
    }

    // ==========================================
    // HELPER TẠO DÒNG & NÚT UI
    // ==========================================
    GameObject CreateItemRow()
    {
        var row = new GameObject("ItemRow", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        row.transform.SetParent(contentRoot, false);

        var img = row.GetComponent<Image>();
        img.color = ColorRowBg;
        img.raycastTarget = false;

        ApplyLayoutSize(row, new Vector2(510, 58), setMinWidth: false);

        var hlg = row.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlHeight = false;
        hlg.childControlWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.padding = new RectOffset(12, 12, 6, 6);

        spawnedItems.Add(row);
        return row;
    }

    TMP_Text CreateText(Transform parent, string text, float fontSize, TextAlignmentOptions align, Vector2 size)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        ApplyLayoutSize(go, size);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        ApplyDefaultFont(tmp);
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = align;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        return tmp;
    }

    Button CreateSimpleButton(Transform parent, string text, Color color, Vector2 size)
    {
        var go = new GameObject("Button_" + text, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        ApplyLayoutSize(go, size);

        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = true;

        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        btn.interactable = true;
        var nav = btn.navigation;
        nav.mode = Navigation.Mode.None;
        btn.navigation = nav;

        var tmpGo = new GameObject("Label", typeof(RectTransform));
        tmpGo.transform.SetParent(go.transform, false);
        var tmpRt = tmpGo.GetComponent<RectTransform>();
        tmpRt.anchorMin = Vector2.zero;
        tmpRt.anchorMax = Vector2.one;
        tmpRt.offsetMin = Vector2.zero;
        tmpRt.offsetMax = Vector2.zero;

        var tmp = tmpGo.AddComponent<TextMeshProUGUI>();
        ApplyDefaultFont(tmp);
        tmp.text = text;
        tmp.fontSize = 18;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;

        return btn;
    }

    static void ApplyLayoutSize(GameObject go, Vector2 size, bool setMinWidth = true)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = size;

        var le = go.GetComponent<LayoutElement>();
        if (setMinWidth) le.minWidth = size.x;
        le.minHeight = size.y;
        le.preferredWidth = size.x;
        le.preferredHeight = size.y;
    }

    static void ApplyDefaultFont(TMP_Text tmp)
    {
        if (tmp != null && tmp.font == null && TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;
    }

    void CreateEmptyNotice(string message)
    {
        var row = new GameObject("EmptyNotice", typeof(RectTransform));
        row.transform.SetParent(contentRoot, false);
        row.GetComponent<RectTransform>().sizeDelta = new Vector2(500, 60);

        var tmp = CreateText(row.transform, message, 19, TextAlignmentOptions.Center, new Vector2(500, 60));
        tmp.color = new Color(0.9f, 0.9f, 0.9f, 0.85f);
        spawnedItems.Add(row);
    }

    void ClearSpawnedItems()
    {
        foreach (var item in spawnedItems)
        {
            if (item == null) continue;
            item.SetActive(false);
            item.transform.SetParent(null, false);
            Destroy(item);
        }
        spawnedItems.Clear();
    }

    // ==========================================
    // CHUẨN HÓA BỐ CỤC & TỰ NỐI THAM CHIẾU UI
    // ==========================================
    static void ClearUiSelection()
    {
        var es = EventSystem.current;
        if (es != null && es.currentSelectedGameObject != null)
            es.SetSelectedGameObject(null);
    }

    void NormalizeShopPanelLayout()
    {
        if (panel != null)
        {
            panel.transform.localScale = Vector3.one;

            if (panel.TryGetComponent(out CanvasGroup cg))
            {
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }

            var canvas = panel.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                var raycaster = canvas.GetComponent<GraphicRaycaster>();
                if (raycaster == null) raycaster = canvas.gameObject.AddComponent<GraphicRaycaster>();
                raycaster.enabled = true;
            }

            // Giữ TabBar tách rõ khỏi ScrollView để không che mất nút hàng đầu
            if (panel.transform.Find("Box/Face/TabBar") is RectTransform tabBar)
            {
                tabBar.localScale = Vector3.one;
                tabBar.anchoredPosition = new Vector2(0f, -116f);
                tabBar.sizeDelta = new Vector2(540f, 40f);
            }
        }

        if (scrollRect != null)
        {
            if (scrollRect.TryGetComponent(out RectTransform scrollRt))
            {
                scrollRt.localScale = Vector3.one;
                scrollRt.anchorMin = new Vector2(0.5f, 0.5f);
                scrollRt.anchorMax = new Vector2(0.5f, 0.5f);
                scrollRt.pivot = new Vector2(0.5f, 0.5f);
                scrollRt.anchoredPosition = new Vector2(0f, -48f);
                scrollRt.sizeDelta = new Vector2(550f, 274f);
            }

            if (scrollRect.viewport != null)
            {
                scrollRect.viewport.localScale = Vector3.one;
                if (scrollRect.viewport.TryGetComponent(out RectMask2D mask))
                    mask.enabled = true;
            }
        }

        if (contentRoot is RectTransform crt)
        {
            crt.localScale = Vector3.one;
            crt.anchorMin = new Vector2(0f, 1f);
            crt.anchorMax = new Vector2(1f, 1f);
            crt.pivot = new Vector2(0.5f, 1f);
            if (crt.TryGetComponent(out VerticalLayoutGroup vlg)) vlg.enabled = true;
            if (crt.TryGetComponent(out ContentSizeFitter csf))
            {
                csf.enabled = true;
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
            if (scrollRect != null && scrollRect.content == null)
                scrollRect.content = crt;
        }

        DisableChildTextRaycast(buyTabButton);
        DisableChildTextRaycast(sellTabButton);
        DisableChildTextRaycast(closeButton);
    }

    static void DisableChildTextRaycast(Button btn)
    {
        if (btn == null) return;
        btn.interactable = true;
        if (btn.targetGraphic != null) btn.targetGraphic.raycastTarget = true;
        foreach (var tmp in btn.GetComponentsInChildren<TMP_Text>(true))
        {
            if (tmp != null) tmp.raycastTarget = false;
        }
    }

    void EnsureUiBuilt()
    {
        if (panel == null)
        {
            panel = GameObject.Find("ShopPanel");
            if (panel == null)
            {
                var canvas = FindAnyObjectByType<Canvas>();
                var foundPanel = canvas != null ? canvas.transform.Find("ShopPanel") : null;
                if (foundPanel != null) panel = foundPanel.gameObject;
            }
        }

        if (contentRoot == null)
        {
            var foundContent = panel != null ? panel.transform.Find("Box/Face/ScrollView/Viewport/Content") : null;
            if (foundContent != null) contentRoot = foundContent;
            else
            {
                var offers = GameObject.Find("Offers");
                if (offers != null) contentRoot = offers.transform;
            }
        }

        var face = panel != null ? panel.transform.Find("Box/Face") : null;
        if (face != null)
        {
            var tabBar = face.Find("TabBar");
            if (tabBar != null)
            {
                if (buyTabButton == null)
                    BindTabButton(tabBar, "TabBuyBtn", "Button_MUA HÀNG", ShopTab.Buy, ref buyTabButton, ref buyTabText);
                if (sellTabButton == null)
                    BindTabButton(tabBar, "TabSellBtn", "Button_BÁN SẢN LƯỢNG", ShopTab.Sell, ref sellTabButton, ref sellTabText);
            }

            if (moneyText == null)
            {
                var moneyGo = face.Find("MoneyText");
                if (moneyGo != null) moneyText = moneyGo.GetComponent<TMP_Text>();
            }
        }

        if (scrollRect == null && panel != null)
            scrollRect = panel.GetComponentInChildren<ScrollRect>();
    }

    void BindTabButton(Transform tabBar, string primaryName, string fallbackName, ShopTab tab, ref Button targetBtn, ref TMP_Text targetText)
    {
        var btnGo = tabBar.Find(primaryName);
        if (btnGo == null) btnGo = tabBar.Find(fallbackName);
        if (btnGo == null || !btnGo.TryGetComponent(out Button btn)) return;

        targetBtn = btn;
        targetText = btnGo.GetComponentInChildren<TMP_Text>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => SetTab(tab));
    }

    static void BringToastToFront()
    {
        var toast = GameObject.Find("ToastRoot");
        var shop = GameObject.Find("ShopPanel");
        if (toast == null || shop == null) return;

        foreach (var tmp in toast.GetComponentsInChildren<TMP_Text>(true))
        {
            tmp.raycastTarget = false;
            ApplyDefaultFont(tmp);
        }

        shop.transform.SetAsLastSibling();
        toast.transform.SetAsLastSibling();
    }
}
