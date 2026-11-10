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
/// Inventory panel kieu 2: thanh day man hinh, khung go, luoi o vuong.
/// Menu: FarmSim → Tao Inventory Panel
/// </summary>
public static class FarmSimInventorySetup
{
    const string MainScenePath = "Assets/Scenes/Main.unity";
    const string AutoFlag = "Assets/Editor/RUN_INVENTORY_SETUP.flag";
    const string MainMenuFlag = "Assets/Editor/RUN_MAINMENU_SETUP.flag";
    const string SlotPrefabPath = "Assets/Prefabs/InventorySlot.prefab";

    static readonly Color WoodOuter = new(0.55f, 0.32f, 0.14f, 0.98f);
    static readonly Color WoodInner = new(0.78f, 0.55f, 0.28f, 1f);
    static readonly Color Cream = new(0.93f, 0.86f, 0.70f, 1f);
    static readonly Color SlotEmpty = new(0.86f, 0.78f, 0.62f, 1f);
    static readonly Color SlotFilled = new(0.93f, 0.86f, 0.70f, 1f);
    static readonly Color SellBtn = new(0.72f, 0.42f, 0.18f, 0.95f);

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
            Debug.Log("[FarmSim] Auto inventory setup (style 2)...");
            try
            {
                Execute();
                EditorUtility.DisplayDialog("FarmSim", "Da tao Inventory kieu thanh day (phim I).", "OK");
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[FarmSim] Inventory setup loi: " + ex);
                EditorUtility.DisplayDialog("FarmSim loi", ex.Message, "OK");
            }
        };
    }

    [MenuItem("FarmSim/Tao Inventory Panel", false, 20)]
    public static void ExecuteFromMenu()
    {
        Execute();
        EditorUtility.DisplayDialog("Xong", "Da cap nhat Inventory Panel kieu 2 tren Main.", "OK");
    }

    [MenuItem("FarmSim/Tao MainMenu", false, 25)]
    public static void BuildMainMenuFromInventoryMenu()
    {
        BuildMainMenuInternal(showDialog: true);
    }

    public static void BuildMainMenuInternal(bool showDialog)
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");

        // Clear old generated UI roots
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
            EditorUtility.DisplayDialog("Xong", "Da tao MainMenu (nen farm + panel go + 3 nut).", "OK");
    }

    public static void Execute()
    {
        EnsureFolders();
        var scene = EditorSceneManager.OpenScene(MainScenePath);

        var canvas = GameObject.Find("Canvas");
        if (canvas == null) throw new System.Exception("Khong tim thay Canvas trong Main.");

        var invTextGo = GameObject.Find("InventoryText");
        if (invTextGo != null)
        {
            var rt = invTextGo.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.sizeDelta = new Vector2(280, 48);
                rt.anchoredPosition = new Vector2(-40, -40);
            }
            var tmp = invTextGo.GetComponent<TMP_Text>();
            if (tmp != null)
            {
                tmp.text = "Kho: 0 (I)";
                tmp.fontSize = 28;
                tmp.alignment = TextAlignmentOptions.TopRight;
            }
        }

        DestroyNamed(canvas.transform, "InventoryPanel");
        DestroyNamed(canvas.transform, "InventoryButton");
        var oldRoot = GameObject.Find("InventoryUIRoot");
        if (oldRoot != null) Object.DestroyImmediate(oldRoot);

        // Force rebuild slot prefab for style 2
        if (AssetDatabase.LoadAssetAtPath<GameObject>(SlotPrefabPath) != null)
            AssetDatabase.DeleteAsset(SlotPrefabPath);
        var slotPrefab = EnsureSlotPrefab();

        // Outer wood frame (bottom center)
        var panel = new GameObject("InventoryPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvas.transform, false);
        panel.GetComponent<Image>().color = WoodOuter;
        var panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0f);
        panelRt.anchorMax = new Vector2(0.5f, 0f);
        panelRt.pivot = new Vector2(0.5f, 0f);
        panelRt.anchoredPosition = new Vector2(0, 24);
        panelRt.sizeDelta = new Vector2(980, 320);

        // Mid wood rim
        var mid = new GameObject("WoodRim", typeof(RectTransform), typeof(Image));
        mid.transform.SetParent(panel.transform, false);
        mid.GetComponent<Image>().color = WoodInner;
        var midRt = mid.GetComponent<RectTransform>();
        midRt.anchorMin = Vector2.zero;
        midRt.anchorMax = Vector2.one;
        midRt.offsetMin = new Vector2(8, 8);
        midRt.offsetMax = new Vector2(-8, -8);

        // Cream body
        var body = new GameObject("Body", typeof(RectTransform), typeof(Image));
        body.transform.SetParent(mid.transform, false);
        body.GetComponent<Image>().color = Cream;
        var bodyRt = body.GetComponent<RectTransform>();
        bodyRt.anchorMin = Vector2.zero;
        bodyRt.anchorMax = Vector2.one;
        bodyRt.offsetMin = new Vector2(6, 6);
        bodyRt.offsetMax = new Vector2(-6, -6);

        var title = CreateTmp(body.transform, "Title", "Inventory",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -22), new Vector2(400, 40),
            TextAlignmentOptions.Center);
        title.fontSize = 36;
        title.fontStyle = FontStyles.Bold;
        title.color = Color.white;
        title.outlineWidth = 0.2f;
        title.outlineColor = new Color32(80, 40, 10, 255);
        title.raycastTarget = false;

        var content = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup));
        content.transform.SetParent(body.transform, false);
        var contentRt = content.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0, 0);
        contentRt.anchorMax = new Vector2(1, 1);
        contentRt.offsetMin = new Vector2(20, 18);
        contentRt.offsetMax = new Vector2(-20, -56);
        var grid = content.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(72, 72);
        grid.spacing = new Vector2(8, 8);
        grid.padding = new RectOffset(4, 4, 4, 4);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 8;
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;

        var empty = CreateTmp(body.transform, "EmptyLabel", "Kho trong",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -10), new Vector2(360, 40),
            TextAlignmentOptions.Center);
        empty.fontSize = 26;
        empty.color = new Color(0.35f, 0.25f, 0.15f, 0.85f);
        empty.raycastTarget = false;
        empty.gameObject.SetActive(false);

        panel.SetActive(false);

        var invButton = CreateButton(canvas.transform, "InventoryButton", "Kho (I)",
            new Vector2(1, 0.5f), new Vector2(1, 0.5f),
            new Vector2(-160, -160), new Vector2(280, 64));

        var toggle = invButton.AddComponent<PanelToggle>();
        var tSo = new SerializedObject(toggle);
        tSo.FindProperty("panel").objectReferenceValue = panel;
        tSo.FindProperty("key").intValue = (int)KeyCode.I;
        tSo.ApplyModifiedPropertiesWithoutUndo();
        UnityEventTools.AddPersistentListener(invButton.GetComponent<Button>().onClick, toggle.Toggle);

        var uiRoot = new GameObject("InventoryUIRoot");
        var invUi = uiRoot.AddComponent<InventoryUI>();
        var uiSo = new SerializedObject(invUi);
        uiSo.FindProperty("content").objectReferenceValue = content.transform;
        uiSo.FindProperty("slotPrefab").objectReferenceValue = slotPrefab;
        uiSo.FindProperty("emptyLabel").objectReferenceValue = empty;
        uiSo.FindProperty("slotCount").intValue = 24;
        uiSo.ApplyModifiedPropertiesWithoutUndo();

        var orders = canvas.transform.Find("OrdersPanel");
        if (orders != null)
            panel.transform.SetSiblingIndex(orders.GetSiblingIndex() + 1);
        else
            panel.transform.SetAsLastSibling();

        // Keep seed buttons / SellAll above inventory visually when both open:
        // Inventory sits at bottom; seed row is also bottom — nudge inventory a bit if needed.
        // SellButton stays outside panel (existing).

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, MainScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[FarmSim] Inventory style-2 wired → " + MainScenePath);
    }

    static InventorySlot EnsureSlotPrefab()
    {
        EnsureFolders();

        var root = new GameObject("InventorySlot", typeof(RectTransform), typeof(Image), typeof(Button));
        var rootImg = root.GetComponent<Image>();
        rootImg.color = SlotEmpty;

        // Soft border via outline image child behind? Use darker parent already; add inset
        var inset = new GameObject("Inset", typeof(RectTransform), typeof(Image));
        inset.transform.SetParent(root.transform, false);
        var insetRt = inset.GetComponent<RectTransform>();
        insetRt.anchorMin = Vector2.zero;
        insetRt.anchorMax = Vector2.one;
        insetRt.offsetMin = new Vector2(3, 3);
        insetRt.offsetMax = new Vector2(-3, -3);
        var insetImg = inset.GetComponent<Image>();
        insetImg.color = SlotFilled;
        insetImg.raycastTarget = false;

        var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(root.transform, false);
        var iconRt = iconGo.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.5f, 0.5f);
        iconRt.anchorMax = new Vector2(0.5f, 0.5f);
        iconRt.pivot = new Vector2(0.5f, 0.5f);
        iconRt.anchoredPosition = new Vector2(0, 4);
        iconRt.sizeDelta = new Vector2(48, 48);
        var icon = iconGo.GetComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        icon.enabled = false;

        var countTmp = CreateTmp(root.transform, "Count", "0",
            new Vector2(1, 0), new Vector2(1, 0), new Vector2(-6, 6), new Vector2(40, 24),
            TextAlignmentOptions.BottomRight);
        countTmp.fontSize = 18;
        countTmp.fontStyle = FontStyles.Bold;
        countTmp.color = Color.white;
        countTmp.outlineWidth = 0.25f;
        countTmp.outlineColor = new Color32(0, 0, 0, 255);
        countTmp.raycastTarget = false;
        countTmp.gameObject.SetActive(false);

        var sellBtn = CreateButton(root.transform, "SellOne", "$",
            new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(14, -14), new Vector2(26, 26));
        sellBtn.GetComponent<Image>().color = SellBtn;
        var sellTmp = sellBtn.GetComponentInChildren<TMP_Text>();
        if (sellTmp != null)
        {
            sellTmp.fontSize = 16;
            sellTmp.fontStyle = FontStyles.Bold;
        }
        sellBtn.SetActive(false);

        var slotComp = root.AddComponent<InventorySlot>();
        var so = new SerializedObject(slotComp);
        so.FindProperty("background").objectReferenceValue = rootImg;
        so.FindProperty("icon").objectReferenceValue = icon;
        so.FindProperty("countText").objectReferenceValue = countTmp;
        so.FindProperty("selectButton").objectReferenceValue = root.GetComponent<Button>();
        so.FindProperty("sellOneButton").objectReferenceValue = sellBtn.GetComponent<Button>();
        so.FindProperty("emptyColor").colorValue = SlotEmpty;
        so.FindProperty("filledColor").colorValue = SlotFilled;
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
        text.fontSize = 30;
        return go;
    }
}
#endif
