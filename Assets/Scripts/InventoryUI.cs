using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance { get; private set; }

    [SerializeField] GameObject panel;
    [SerializeField] Transform content;
    [SerializeField] InventorySlot slotPrefab;
    [SerializeField] TMP_Text emptyLabel;
    [SerializeField] TMP_Text infoText;
    [SerializeField] Button closeButton;
    [SerializeField] Button sellOneButton;
    [SerializeField] Button sellAllButton;
    [SerializeField] int slotCount = 24;

    public bool IsPanelOpen
    {
        get
        {
            if (panel != null) return panel.activeInHierarchy;
            if (content != null) return content.gameObject.activeInHierarchy;
            return false;
        }
    }

    public InventoryItem SelectedItem { get; private set; }

    readonly List<InventorySlot> spawned = new();
    bool lastOpenState = false;

    void Awake()
    {
        Instance = this;
        ResolvePanel();
        NormalizePanelLayout();

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
        if (sellOneButton != null)
            sellOneButton.onClick.AddListener(OnSellOne);
        if (sellAllButton != null)
            sellAllButton.onClick.AddListener(OnSellAll);
    }

    void ResolvePanel()
    {
        if (panel == null && content != null)
        {
            Transform cur = content;
            while (cur != null)
            {
                if (cur.name == "InventoryPanel")
                {
                    panel = cur.gameObject;
                    break;
                }
                cur = cur.parent;
            }
        }

        if (panel == null)
        {
            var p = GameObject.Find("InventoryPanel");
            if (p != null) panel = p;
        }
    }

    void NormalizePanelLayout()
    {
        if (panel == null) return;

        var panelRt = panel.GetComponent<RectTransform>();
        if (panelRt != null)
        {
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.anchoredPosition = new Vector2(0f, 25f);
            panelRt.sizeDelta = new Vector2(760f, 440f);
        }

        if (content != null)
        {
            var contentRt = content.GetComponent<RectTransform>();
            if (contentRt != null)
            {
                contentRt.anchorMin = Vector2.zero;
                contentRt.anchorMax = Vector2.one;
                contentRt.offsetMin = new Vector2(20, 80);
                contentRt.offsetMax = new Vector2(-20, -52);
            }

            if (content.TryGetComponent(out GridLayoutGroup grid))
            {
                grid.cellSize = new Vector2(80, 80);
                grid.spacing = new Vector2(10, 10);
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = 8;
                grid.childAlignment = TextAnchor.UpperCenter;
            }
        }

        EnsureInfoBar();
        EnsureCloseButton();
    }

    void EnsureCloseButton()
    {
        if (closeButton != null || panel == null) return;
        var body = panel.transform.Find("WoodRim/Body") ?? panel.transform;
        var existing = body.Find("CloseButton");
        if (existing != null)
        {
            closeButton = existing.GetComponent<Button>();
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
                closeButton.onClick.AddListener(Close);
                return;
            }
        }

        var btnGo = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(body, false);
        var rt = btnGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-18, -18);
        rt.sizeDelta = new Vector2(30, 30);
        btnGo.GetComponent<Image>().color = new Color(0.65f, 0.22f, 0.18f, 0.95f);

        var txtGo = new GameObject("Text", typeof(RectTransform));
        txtGo.transform.SetParent(btnGo.transform, false);
        var txtRt = txtGo.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;
        var tmp = txtGo.AddComponent<TextMeshProUGUI>();
        tmp.text = "✕";
        tmp.fontSize = 18;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        closeButton = btnGo.GetComponent<Button>();
        closeButton.onClick.AddListener(Close);
    }

    void EnsureInfoBar()
    {
        if (panel == null) return;
        var body = panel.transform.Find("WoodRim/Body") ?? panel.transform;

        var footer = body.Find("InfoFooter");
        GameObject footGo;
        if (footer == null)
        {
            footGo = new GameObject("InfoFooter", typeof(RectTransform), typeof(Image));
            footGo.transform.SetParent(body, false);
            var footRt = footGo.GetComponent<RectTransform>();
            footRt.anchorMin = new Vector2(0, 0);
            footRt.anchorMax = new Vector2(1, 0);
            footRt.pivot = new Vector2(0.5f, 0f);
            footRt.anchoredPosition = new Vector2(0, 12);
            footRt.sizeDelta = new Vector2(-40, 56);
            footGo.GetComponent<Image>().color = new Color(0.18f, 0.13f, 0.08f, 0.94f);
        }
        else
        {
            footGo = footer.gameObject;
        }

        if (infoText == null)
        {
            var txt = footGo.transform.Find("InfoText");
            if (txt != null)
            {
                infoText = txt.GetComponent<TMP_Text>();
            }
            else
            {
                var go = new GameObject("InfoText", typeof(RectTransform));
                go.transform.SetParent(footGo.transform, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(16, 4);
                rt.offsetMax = new Vector2(-190, -4);
                infoText = go.AddComponent<TextMeshProUGUI>();
                infoText.fontSize = 15;
                infoText.color = Color.white;
                infoText.alignment = TextAlignmentOptions.Left;
            }
        }

        if (sellOneButton == null)
        {
            var b = footGo.transform.Find("SellOneBtn");
            if (b != null) sellOneButton = b.GetComponent<Button>();
            else
            {
                var go = CreateSimpleBtn(footGo.transform, "SellOneBtn", "Bán 1",
                    new Vector2(1f, 0.5f), new Vector2(-96, 0), new Vector2(82, 34),
                    new Color(0.82f, 0.45f, 0.12f, 0.95f));
                sellOneButton = go.GetComponent<Button>();
                sellOneButton.onClick.AddListener(OnSellOne);
            }
        }

        if (sellAllButton == null)
        {
            var b = footGo.transform.Find("SellAllBtn");
            if (b != null) sellAllButton = b.GetComponent<Button>();
            else
            {
                var go = CreateSimpleBtn(footGo.transform, "SellAllBtn", "Bán hết",
                    new Vector2(1f, 0.5f), new Vector2(-8, 0), new Vector2(82, 34),
                    new Color(0.18f, 0.55f, 0.28f, 0.95f));
                sellAllButton = go.GetComponent<Button>();
                sellAllButton.onClick.AddListener(OnSellAll);
            }
        }
    }

    static GameObject CreateSimpleBtn(Transform parent, string name, string label,
        Vector2 anchor, Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        go.GetComponent<Image>().color = color;

        var txtGo = new GameObject("Text", typeof(RectTransform));
        txtGo.transform.SetParent(go.transform, false);
        var txtRt = txtGo.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;
        var tmp = txtGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 14;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        return go;
    }

    void Start()
    {
        ResolvePanel();
        NormalizePanelLayout();

        var gm = GameManager.Instance;
        if (gm != null)
        {
            gm.OnInventoryChanged += Refresh;
            gm.OnSeedsChanged += Refresh;
            gm.OnHotbarChanged += Refresh;
        }

        Refresh();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        var gm = GameManager.Instance;
        if (gm != null)
        {
            gm.OnInventoryChanged -= Refresh;
            gm.OnSeedsChanged -= Refresh;
            gm.OnHotbarChanged -= Refresh;
        }
    }

    void Update()
    {
        bool open = IsPanelOpen;
        if (open != lastOpenState)
        {
            lastOpenState = open;
            if (open)
            {
                ClearSelection();
                Refresh();
            }
            else
            {
                ClearSelection();
            }
        }

        if (!open) return;

        // Phím ESC đóng túi đồ
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
            return;
        }

        // Nếu đang chọn 1 vật phẩm trong túi đồ, bấm phím 1..6 để gán ngay vào ô đó trên Toolbar!
        if (SelectedItem != null && !SelectedItem.IsEmpty && GameManager.Instance != null)
        {
            for (int i = 0; i < GameManager.HotbarSlotCount; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i))
                {
                    GameManager.Instance.AssignItemToHotbar(i, SelectedItem);
                    Toast.Show($"Đã gán {SelectedItem.Name} vào ô {i + 1} thanh công cụ");
                    ClearSelection();
                    break;
                }
            }
        }
    }

    public void Close()
    {
        ResolvePanel();
        if (panel != null) panel.SetActive(false);
        ClearSelection();
    }

    public void SelectItem(InventoryItem item)
    {
        if (item == null || item.IsEmpty)
        {
            ClearSelection();
            return;
        }

        // Click lại vào cùng 1 món thì huỷ chọn
        if (SelectedItem != null && GameManager.Instance != null && GameManager.Instance.IsSameItem(SelectedItem, item))
        {
            ClearSelection();
            return;
        }

        SelectedItem = item;
        Refresh();
    }

    public void ClearSelection()
    {
        SelectedItem = null;
        Refresh();
    }

    void UpdateInfoBar()
    {
        var gm = GameManager.Instance;
        if (infoText == null) return;

        if (SelectedItem == null || SelectedItem.IsEmpty || gm == null)
        {
            infoText.text = "<color=#78350F><b>TÚI ĐỒ NÔNG TRẠI</b></color>  —  <color=#CBD5E1>Click chọn vật phẩm để xem hoặc gán phím 1 - 6</color>";
            if (sellOneButton != null) sellOneButton.gameObject.SetActive(false);
            if (sellAllButton != null) sellAllButton.gameObject.SetActive(false);
            return;
        }

        string typeStr = SelectedItem.kind switch
        {
            ItemKind.Tool => "Công cụ",
            ItemKind.Seed => "Hạt giống",
            ItemKind.Harvested => "Nông sản",
            _ => "Vật phẩm"
        };

        int slotIdx = gm.GetHotbarSlotOf(SelectedItem);
        string hotbarStr = slotIdx >= 0 ? $"<color=#60A5FA><b>[Ô {slotIdx + 1} Toolbar]</b></color>" : "<color=#94A3B8>[Chưa gán Toolbar]</color>";
        string countStr = (SelectedItem.count > 1 || SelectedItem.kind == ItemKind.Seed) ? $"  |  Số lượng: <b>{SelectedItem.count}</b>" : "";

        infoText.text = $"<b><color=#FBBF24>{SelectedItem.Name}</color></b> <color=#94A3B8>({typeStr})</color>{countStr}   {hotbarStr}\n<size=15><color=#93C5FD><b>[>]</b> Bấm phím <b>1 - 6</b> hoặc click ô Toolbar để trang bị  |  Click lại để bỏ chọn</color></size>";

        // Hiển thị nút bán nếu là nông sản thu hoạch
        bool isHarvest = SelectedItem.kind == ItemKind.Harvested && SelectedItem.crop != null && SelectedItem.count > 0;
        if (sellOneButton != null)
        {
            sellOneButton.gameObject.SetActive(isHarvest);
            if (isHarvest)
            {
                int price = gm.GetSellPrice(SelectedItem.crop);
                var t = sellOneButton.GetComponentInChildren<TMP_Text>();
                if (t != null) t.text = $"Bán 1 (+{price}$)";
            }
        }

        if (sellAllButton != null)
        {
            sellAllButton.gameObject.SetActive(isHarvest);
            if (isHarvest)
            {
                int totalPrice = gm.GetSellPrice(SelectedItem.crop) * SelectedItem.count;
                var t = sellAllButton.GetComponentInChildren<TMP_Text>();
                if (t != null) t.text = $"Bán hết (+{totalPrice}$)";
            }
        }
    }

    void OnSellOne()
    {
        if (SelectedItem == null || SelectedItem.crop == null || GameManager.Instance == null) return;
        var crop = SelectedItem.crop;
        GameManager.Instance.SellOne(crop);
        int left = GameManager.Instance.Count(crop);
        if (left > 0) SelectedItem.count = left;
        else ClearSelection();
    }

    void OnSellAll()
    {
        if (SelectedItem == null || SelectedItem.crop == null || GameManager.Instance == null) return;
        var crop = SelectedItem.crop;
        int count = GameManager.Instance.Count(crop);
        GameManager.Instance.SellAmount(crop, count);
        ClearSelection();
    }

    public void Refresh()
    {
        UpdateInfoBar();

        if (content == null || slotPrefab == null || GameManager.Instance == null) return;

        foreach (var s in spawned)
            if (s != null) Destroy(s.gameObject);
        spawned.Clear();

        var gm = GameManager.Instance;
        var allItems = new List<InventoryItem>();

        // 1. Toàn bộ công cụ đã sở hữu
        var tb = ToolsToolbar.Instance ?? FindAnyObjectByType<ToolsToolbar>(FindObjectsInactive.Include);
        if (tb != null && tb.Tools != null)
        {
            foreach (var t in tb.Tools)
            {
                if (t != null && t.type != ToolType.Hand && t.type != ToolType.Seed && gm.OwnsTool(t))
                {
                    allItems.Add(InventoryItem.FromTool(t));
                }
            }
        }

        // 2. Toàn bộ Hạt giống đang có trong túi
        foreach (var kv in gm.Seeds)
        {
            if (kv.Key != null && kv.Value > 0)
            {
                allItems.Add(InventoryItem.FromSeed(kv.Key, kv.Value));
            }
        }

        // 3. Toàn bộ Nông sản thu hoạch đang có trong kho
        foreach (var kv in gm.Harvested)
        {
            if (kv.Key != null && kv.Value > 0)
            {
                allItems.Add(InventoryItem.FromHarvested(kv.Key, kv.Value));
            }
        }

        if (emptyLabel != null)
            emptyLabel.gameObject.SetActive(allItems.Count == 0);

        int total = Mathf.Max(slotCount, allItems.Count);

        for (int i = 0; i < total; i++)
        {
            var slot = Instantiate(slotPrefab, content);
            slot.gameObject.SetActive(true);
            if (i < allItems.Count)
            {
                var it = allItems[i];
                bool isSelected = SelectedItem != null && gm.IsSameItem(SelectedItem, it);
                slot.Bind(it, isSelected);
            }
            else
            {
                slot.Bind(null, false);
            }
            spawned.Add(slot);
        }
    }
}
