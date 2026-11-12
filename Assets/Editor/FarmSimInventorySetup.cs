#if UNITY_EDITOR
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Inventory panel kieu centered popup: khung go dep, ro rang, khong bi che toolbar.
/// Menu: FarmSim → Tao Inventory Panel
/// </summary>
public static class FarmSimInventorySetup
{
    const string MainScenePath = "Assets/Scenes/Main.unity";
    const string AutoFlag = "Assets/Editor/RUN_INVENTORY_SETUP.flag";
    const string MainMenuFlag = "Assets/Editor/RUN_MAINMENU_SETUP.flag";
    const string SlotPrefabPath = "Assets/Prefabs/InventorySlot.prefab";

    static readonly Color WoodOuter = new(0.24f, 0.16f, 0.10f, 0.98f);
    static readonly Color WoodInner = new(0.55f, 0.38f, 0.22f, 1f);
    static readonly Color Cream = new(0.96f, 0.93f, 0.85f, 1f);
    static readonly Color SlotEmpty = new(0.85f, 0.80f, 0.72f, 0.45f);
    static readonly Color SlotFilled = new(0.96f, 0.92f, 0.84f, 1f);
    static readonly Color SlotSelected = new(1.0f, 0.88f, 0.35f, 1f);

    [InitializeOnLoadMethod]
    static void AutoRunIfFlagged()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        TryAutoRun();
    }

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
            TryAutoRun();
    }

    static void TryAutoRun()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        if (File.Exists(MainMenuFlag))
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(MainMenuFlag)) return;
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                try { File.Delete(MainMenuFlag); } catch { /* ignore */ }
                try
                {
                    BuildMainMenuInternal(showDialog: false);
                    Debug.Log("[FarmSim] MainMenu auto-built via InventorySetup.");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError("[FarmSim] MainMenu auto loi: " + ex);
                }
            };
        }

        if (!File.Exists(AutoFlag)) return;
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(AutoFlag)) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            try { File.Delete(AutoFlag); } catch { /* ignore */ }
            Debug.Log("[FarmSim] Auto inventory setup (style popup)...");
            try
            {
                Execute();
                Debug.Log("[FarmSim] Da tao Inventory kieu popup giua man hinh (phim I).");
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[FarmSim] Inventory setup loi: " + ex);
            }
        };
    }

    [MenuItem("FarmSim/Tao Inventory Panel", false, 20)]
    public static void ExecuteFromMenu()
    {
        Execute();
        EditorUtility.DisplayDialog("Xong", "Da cap nhat Inventory Panel dep tren Main.", "OK");
    }

    [MenuItem("FarmSim/Tao MainMenu", false, 25)]
    public static void BuildMainMenuFromInventoryMenu()
    {
        BuildMainMenuInternal(showDialog: true);
    }

    public static void BuildMainMenuInternal(bool showDialog)
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");

        foreach (var root in scene.GetRootGameObjects().ToArray())
        {
            if (root.name == "Main Camera" || root.name == "Global Light 2D") continue;
            Object.DestroyImmediate(root);
        }

        var menuGo = new GameObject("MainMenuRoot");
        var menu = menuGo.AddComponent<MainMenu>();

        Sprite preview = null;
        foreach (var a in AssetDatabase.LoadAllAssetsAtPath("Assets/Art/TinyFarm/Preview.png"))
            if (a is Sprite s) { preview = s; break; }

        var so = new SerializedObject(menu);
        so.FindProperty("backgroundSprite").objectReferenceValue = preview;
        so.ApplyModifiedPropertiesWithoutUndo();

        menu.BuildDefaultUi();

        so = new SerializedObject(menu);
        so.FindProperty("continueButton").objectReferenceValue = GameObject.Find("ContinueButton")?.GetComponent<Button>();
        so.FindProperty("newGameButton").objectReferenceValue = GameObject.Find("NewGameButton")?.GetComponent<Button>();
        so.FindProperty("quitButton").objectReferenceValue = GameObject.Find("QuitButton")?.GetComponent<Button>();
        so.FindProperty("newGameLabel").objectReferenceValue = GameObject.Find("NewGameButton")?.GetComponentInChildren<TMP_Text>();
        so.FindProperty("backgroundSprite").objectReferenceValue = preview;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        if (showDialog)
            EditorUtility.DisplayDialog("Xong", "Da tao MainMenu.", "OK");
    }

    public static void Execute()
    {
        EnsureFolders();
        var scene = EditorSceneManager.OpenScene(MainScenePath);

        var canvas = GameObject.Find("Canvas");
        if (canvas == null) throw new System.Exception("Khong tim thay Canvas trong Main.");

        DestroyNamed(canvas.transform, "InventoryPanel");
        var oldRoot = GameObject.Find("InventoryUIRoot");
        if (oldRoot != null) Object.DestroyImmediate(oldRoot);

        // Force rebuild slot prefab
        if (AssetDatabase.LoadAssetAtPath<GameObject>(SlotPrefabPath) != null)
            AssetDatabase.DeleteAsset(SlotPrefabPath);
        var slotPrefab = EnsureSlotPrefab();

        // 1. Khung ngoai go am ap - Can giua man hinh (khong de len Toolbar phia duoi!)
        var panel = new GameObject("InventoryPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvas.transform, false);
        panel.GetComponent<Image>().color = WoodOuter;
        var panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.anchoredPosition = new Vector2(0f, 25f);
        panelRt.sizeDelta = new Vector2(760f, 440f);

        // 2. Vien go sang
        var mid = new GameObject("WoodRim", typeof(RectTransform), typeof(Image));
        mid.transform.SetParent(panel.transform, false);
        mid.GetComponent<Image>().color = WoodInner;
        var midRt = mid.GetComponent<RectTransform>();
        midRt.anchorMin = Vector2.zero;
        midRt.anchorMax = Vector2.one;
        midRt.offsetMin = new Vector2(6, 6);
        midRt.offsetMax = new Vector2(-6, -6);

        // 3. Than giay/go kem am ap
        var body = new GameObject("Body", typeof(RectTransform), typeof(Image));
        body.transform.SetParent(mid.transform, false);
        body.GetComponent<Image>().color = Cream;
        var bodyRt = body.GetComponent<RectTransform>();
        bodyRt.anchorMin = Vector2.zero;
        bodyRt.anchorMax = Vector2.one;
        bodyRt.offsetMin = new Vector2(5, 5);
        bodyRt.offsetMax = new Vector2(-5, -5);

        // 4. Tieu de tren dau
        var title = CreateTmp(body.transform, "Title", "TÚI ĐỒ (INVENTORY)",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -22), new Vector2(400, 36),
            TextAlignmentOptions.Center);
        title.fontSize = 24;
        title.fontStyle = FontStyles.Bold;
        title.color = new Color(0.20f, 0.12f, 0.05f, 1f);
        title.raycastTarget = false;

        // Nut dong [X] o goc tren phai
        var closeBtn = CreateButton(body.transform, "CloseButton", "✕",
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-18, -18), new Vector2(30, 30));
        closeBtn.GetComponent<Image>().color = new Color(0.65f, 0.22f, 0.18f, 0.95f);
        var closeTmp = closeBtn.GetComponentInChildren<TMP_Text>();
        if (closeTmp != null) closeTmp.fontSize = 18;

        // 5. Luoi o vat pham (8 cot x 3 dong = 24 o)
        var content = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup));
        content.transform.SetParent(body.transform, false);
        var contentRt = content.GetComponent<RectTransform>();
        contentRt.anchorMin = Vector2.zero;
        contentRt.anchorMax = Vector2.one;
        contentRt.offsetMin = new Vector2(20, 80);
        contentRt.offsetMax = new Vector2(-20, -52);
        var grid = content.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(80, 80);
        grid.spacing = new Vector2(10, 10);
        grid.padding = new RectOffset(4, 4, 4, 4);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 8;
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;

        var empty = CreateTmp(body.transform, "EmptyLabel", "Túi đồ hiện đang trống",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 10), new Vector2(360, 40),
            TextAlignmentOptions.Center);
        empty.fontSize = 22;
        empty.color = new Color(0.40f, 0.30f, 0.20f, 0.85f);
        empty.raycastTarget = false;
        empty.gameObject.SetActive(false);

        // 6. Thanh Footer thong tin vat pham & thao tac o duoi cung
        var footer = new GameObject("InfoFooter", typeof(RectTransform), typeof(Image));
        footer.transform.SetParent(body.transform, false);
        var footRt = footer.GetComponent<RectTransform>();
        footRt.anchorMin = new Vector2(0, 0);
        footRt.anchorMax = new Vector2(1, 0);
        footRt.pivot = new Vector2(0.5f, 0f);
        footRt.anchoredPosition = new Vector2(0, 12);
        footRt.sizeDelta = new Vector2(-40, 56);
        footer.GetComponent<Image>().color = new Color(0.18f, 0.13f, 0.08f, 0.94f);

        var infoText = CreateTmp(footer.transform, "InfoText",
            "<color=#78350F><b>TÚI ĐỒ NÔNG TRẠI</b></color>  —  <color=#CBD5E1>Click chọn vật phẩm để xem hoặc gán phím 1 - 6</color>",
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            TextAlignmentOptions.Left);
        infoText.rectTransform.offsetMin = new Vector2(16, 4);
        infoText.rectTransform.offsetMax = new Vector2(-190, -4);
        infoText.fontSize = 15;
        infoText.color = Color.white;
        infoText.raycastTarget = false;

        var sellOneBtn = CreateButton(footer.transform, "SellOneBtn", "Bán 1",
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-96, 0), new Vector2(82, 34));
        sellOneBtn.GetComponent<Image>().color = new Color(0.82f, 0.45f, 0.12f, 0.95f);
        var sellOneTmp = sellOneBtn.GetComponentInChildren<TMP_Text>();
        if (sellOneTmp != null) sellOneTmp.fontSize = 14;
        sellOneBtn.SetActive(false);

        var sellAllBtn = CreateButton(footer.transform, "SellAllBtn", "Bán hết",
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-8, 0), new Vector2(82, 34));
        sellAllBtn.GetComponent<Image>().color = new Color(0.18f, 0.55f, 0.28f, 0.95f);
        var sellAllTmp = sellAllBtn.GetComponentInChildren<TMP_Text>();
        if (sellAllTmp != null) sellAllTmp.fontSize = 14;
        sellAllBtn.SetActive(false);

        panel.SetActive(false);

        // 7. Khoi tao InventoryUIRoot va gan cac thanh phan vao InventoryUI
        var uiRoot = new GameObject("InventoryUIRoot");
        var invUi = uiRoot.AddComponent<InventoryUI>();
        var uiSo = new SerializedObject(invUi);
        uiSo.FindProperty("panel").objectReferenceValue = panel;
        uiSo.FindProperty("content").objectReferenceValue = content.transform;
        uiSo.FindProperty("slotPrefab").objectReferenceValue = slotPrefab;
        uiSo.FindProperty("emptyLabel").objectReferenceValue = empty;
        uiSo.FindProperty("infoText").objectReferenceValue = infoText;
        uiSo.FindProperty("closeButton").objectReferenceValue = closeBtn.GetComponent<Button>();
        uiSo.FindProperty("sellOneButton").objectReferenceValue = sellOneBtn.GetComponent<Button>();
        uiSo.FindProperty("sellAllButton").objectReferenceValue = sellAllBtn.GetComponent<Button>();
        uiSo.FindProperty("slotCount").intValue = 24;
        uiSo.ApplyModifiedPropertiesWithoutUndo();

        // Ket noi phim I va nut InventoryButton toi panel moi nay
        var invBtn = canvas.transform.Find("InventoryButton");
        if (invBtn != null)
        {
            var toggle = invBtn.GetComponent<PanelToggle>();
            if (toggle != null)
            {
                var tSo = new SerializedObject(toggle);
                tSo.FindProperty("panel").objectReferenceValue = panel;
                tSo.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        var orders = canvas.transform.Find("OrdersPanel");
        if (orders != null)
            panel.transform.SetSiblingIndex(orders.GetSiblingIndex() + 1);
        else
            panel.transform.SetAsLastSibling();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, MainScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[FarmSim] Centered Inventory wired → " + MainScenePath);
    }

    static InventorySlot EnsureSlotPrefab()
    {
        EnsureFolders();

        var root = new GameObject("InventorySlot", typeof(RectTransform), typeof(Image), typeof(Button));
        var rootImg = root.GetComponent<Image>();
        rootImg.color = SlotEmpty;
        var rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(80, 80);

        // Inset border
        var inset = new GameObject("Inset", typeof(RectTransform), typeof(Image));
        inset.transform.SetParent(root.transform, false);
        var insetRt = inset.GetComponent<RectTransform>();
        insetRt.anchorMin = Vector2.zero;
        insetRt.anchorMax = Vector2.one;
        insetRt.offsetMin = new Vector2(2, 2);
        insetRt.offsetMax = new Vector2(-2, -2);
        var insetImg = inset.GetComponent<Image>();
        insetImg.color = SlotFilled;
        insetImg.raycastTarget = false;

        // Icon (To, dep, o giua, khong chu che!)
        var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(root.transform, false);
        var iconRt = iconGo.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.5f, 0.5f);
        iconRt.anchorMax = new Vector2(0.5f, 0.5f);
        iconRt.pivot = new Vector2(0.5f, 0.5f);
        iconRt.anchoredPosition = Vector2.zero;
        iconRt.sizeDelta = new Vector2(50, 50);
        var icon = iconGo.GetComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        icon.enabled = false;

        // Count Badge (goc duoi phai kem nen toi x{count})
        var countBg = new GameObject("CountBg", typeof(RectTransform), typeof(Image));
        countBg.transform.SetParent(root.transform, false);
        var countBgRt = countBg.GetComponent<RectTransform>();
        countBgRt.anchorMin = new Vector2(1, 0);
        countBgRt.anchorMax = new Vector2(1, 0);
        countBgRt.pivot = new Vector2(1, 0);
        countBgRt.anchoredPosition = new Vector2(-3, 3);
        countBgRt.sizeDelta = new Vector2(36, 19);
        var countBgImg = countBg.GetComponent<Image>();
        countBgImg.color = new Color(0.10f, 0.10f, 0.14f, 0.88f);
        countBgImg.raycastTarget = false;

        var countTmp = CreateTmp(countBg.transform, "Count", "x0",
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            TextAlignmentOptions.Center);
        countTmp.fontSize = 12;
        countTmp.fontStyle = FontStyles.Bold;
        countTmp.color = Color.white;
        countTmp.raycastTarget = false;
        countBg.SetActive(false);

        // Hotbar Badge (goc tren trai, nen xanh vien [1]..[6])
        var badgeBg = new GameObject("HotbarBadge", typeof(RectTransform), typeof(Image));
        badgeBg.transform.SetParent(root.transform, false);
        var badgeRt = badgeBg.GetComponent<RectTransform>();
        badgeRt.anchorMin = new Vector2(0, 1);
        badgeRt.anchorMax = new Vector2(0, 1);
        badgeRt.pivot = new Vector2(0, 1);
        badgeRt.anchoredPosition = new Vector2(3, -3);
        badgeRt.sizeDelta = new Vector2(19, 18);
        var badgeImg = badgeBg.GetComponent<Image>();
        badgeImg.color = new Color(0.15f, 0.42f, 0.90f, 0.95f);
        badgeImg.raycastTarget = false;

        var badgeTmp = CreateTmp(badgeBg.transform, "BadgeText", "1",
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            TextAlignmentOptions.Center);
        badgeTmp.fontSize = 11;
        badgeTmp.fontStyle = FontStyles.Bold;
        badgeTmp.color = Color.white;
        badgeTmp.raycastTarget = false;
        badgeBg.SetActive(false);

        var slotComp = root.AddComponent<InventorySlot>();
        var so = new SerializedObject(slotComp);
        so.FindProperty("background").objectReferenceValue = rootImg;
        so.FindProperty("icon").objectReferenceValue = icon;
        so.FindProperty("countText").objectReferenceValue = countTmp;
        so.FindProperty("badgeText").objectReferenceValue = badgeTmp;
        so.FindProperty("selectButton").objectReferenceValue = root.GetComponent<Button>();
        so.FindProperty("emptyColor").colorValue = SlotEmpty;
        so.FindProperty("filledColor").colorValue = SlotFilled;
        so.FindProperty("selectedColor").colorValue = SlotSelected;
        so.ApplyModifiedPropertiesWithoutUndo();

        root.SetActive(false);
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, SlotPrefabPath);
        Object.DestroyImmediate(root);
        return prefab.GetComponent<InventorySlot>();
    }

    static void DestroyNamed(Transform parent, string name)
    {
        var t = parent.Find(name);
        if (t != null) Object.DestroyImmediate(t.gameObject);
    }

    static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/Editor"))
            AssetDatabase.CreateFolder("Assets", "Editor");
    }

    static TMP_Text CreateTmp(Transform parent, string name, string content,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size,
        TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        if (Mathf.Approximately(anchorMin.x, 0f) && Mathf.Approximately(anchorMax.x, 1f)
            && Mathf.Approximately(anchorMin.y, 0f) && Mathf.Approximately(anchorMax.y, 1f))
        {
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = 28;
        tmp.alignment = align;
        tmp.color = Color.white;
        return tmp;
    }

    static GameObject CreateButton(Transform parent, string name, string label,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        go.GetComponent<Image>().color = new Color(0.18f, 0.45f, 0.28f, 0.95f);

        var text = CreateTmp(go.transform, "Text", label,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
        text.rectTransform.offsetMin = Vector2.zero;
        text.rectTransform.offsetMax = Vector2.zero;
        text.fontSize = 20;
        return go;
    }
}
#endif
