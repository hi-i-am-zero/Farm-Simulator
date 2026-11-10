#if UNITY_EDITOR
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tao ToolsToolbar (cuoc / tuoi / hat / tay) duoi Inventory.
/// Menu: FarmSim → Tao Tools Toolbar
/// </summary>
public static class FarmSimToolsToolbarSetup
{
    const string MainScenePath = "Assets/Scenes/Main.unity";
    const string AutoFlag = "Assets/Editor/RUN_TOOLS_TOOLBAR_SETUP.flag";
    const string SlotPrefabPath = "Assets/Prefabs/ToolSlot.prefab";
    const string ToolsFolder = "Assets/Data/Tools";
    const string PlotPrefabPath = "Assets/Prefabs/Plot.prefab";

    static readonly Color WoodOuter = new(0.55f, 0.32f, 0.14f, 0.98f);
    static readonly Color WoodInner = new(0.78f, 0.55f, 0.28f, 1f);
    static readonly Color Cream = new(0.93f, 0.86f, 0.70f, 1f);
    static readonly Color SlotEmpty = new(0.86f, 0.78f, 0.62f, 1f);
    static readonly Color SlotSelected = new(1f, 0.95f, 0.55f, 1f);

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
        if (!File.Exists(AutoFlag)) return;

        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(AutoFlag)) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            try { File.Delete(AutoFlag); } catch { /* ignore */ }
            Debug.Log("[FarmSim] Auto tools toolbar setup...");
            try
            {
                Execute();
                EditorUtility.DisplayDialog("FarmSim", "Da tao Tools Toolbar (phim 1-4).", "OK");
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[FarmSim] Tools toolbar setup loi: " + ex);
                EditorUtility.DisplayDialog("FarmSim loi", ex.Message, "OK");
            }
        };
    }

    [MenuItem("FarmSim/Tao Tools Toolbar", false, 21)]
    public static void ExecuteFromMenu()
    {
        Execute();
        EditorUtility.DisplayDialog("Xong", "Da tao Tools Toolbar duoi Inventory tren Main.", "OK");
    }

    public static void Execute()
    {
        EnsureFolders();
        WirePlotPrefab();

        var hoe = EnsureTool("Hoe", "Cuốc", ToolType.Hoe, "tile_0088", 15);
        var water = EnsureTool("WateringCan", "Bình tưới", ToolType.WateringCan, "tile_0084", 20);
        var seed = EnsureTool("Seed", "Hạt giống", ToolType.Seed, "tile_0089", 10);
        var hand = EnsureTool("Hand", "Tay", ToolType.Hand, null, 10);
        var tools = new[] { hoe, water, seed, hand };

        var slotPrefab = EnsureSlotPrefab();
        var scene = EditorSceneManager.OpenScene(MainScenePath);

        var canvas = GameObject.Find("Canvas");
        if (canvas == null) throw new System.Exception("Khong tim thay Canvas trong Main.");

        DestroyNamed(canvas.transform, "ToolsToolbar");

        var bar = new GameObject("ToolsToolbar", typeof(RectTransform), typeof(Image));
        bar.transform.SetParent(canvas.transform, false);
        bar.GetComponent<Image>().color = WoodOuter;
        var barRt = bar.GetComponent<RectTransform>();
        barRt.anchorMin = new Vector2(0.5f, 0f);
        barRt.anchorMax = new Vector2(0.5f, 0f);
        barRt.pivot = new Vector2(0.5f, 0f);
        barRt.anchoredPosition = new Vector2(0, 16);
        barRt.sizeDelta = new Vector2(380, 96);

        var mid = new GameObject("WoodRim", typeof(RectTransform), typeof(Image));
        mid.transform.SetParent(bar.transform, false);
        mid.GetComponent<Image>().color = WoodInner;
        var midRt = mid.GetComponent<RectTransform>();
        midRt.anchorMin = Vector2.zero;
        midRt.anchorMax = Vector2.one;
        midRt.offsetMin = new Vector2(6, 6);
        midRt.offsetMax = new Vector2(-6, -6);

        var body = new GameObject("Body", typeof(RectTransform), typeof(Image));
        body.transform.SetParent(mid.transform, false);
        body.GetComponent<Image>().color = Cream;
        var bodyRt = body.GetComponent<RectTransform>();
        bodyRt.anchorMin = Vector2.zero;
        bodyRt.anchorMax = Vector2.one;
        bodyRt.offsetMin = new Vector2(4, 4);
        bodyRt.offsetMax = new Vector2(-4, -4);

        var content = new GameObject("Content", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        content.transform.SetParent(body.transform, false);
        var contentRt = content.GetComponent<RectTransform>();
        contentRt.anchorMin = Vector2.zero;
        contentRt.anchorMax = Vector2.one;
        contentRt.offsetMin = new Vector2(10, 8);
        contentRt.offsetMax = new Vector2(-10, -8);
        var layout = content.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 10;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var toolbar = bar.AddComponent<ToolsToolbar>();
        var tbSo = new SerializedObject(toolbar);
        tbSo.FindProperty("content").objectReferenceValue = content.transform;
        tbSo.FindProperty("slotPrefab").objectReferenceValue = slotPrefab;
        var toolsProp = tbSo.FindProperty("tools");
        toolsProp.arraySize = tools.Length;
        for (int i = 0; i < tools.Length; i++)
            toolsProp.GetArrayElementAtIndex(i).objectReferenceValue = tools[i];
        tbSo.ApplyModifiedPropertiesWithoutUndo();

        // Inventory panel sits above hotbar
        var invPanel = canvas.transform.Find("InventoryPanel") as RectTransform;
        if (invPanel != null)
            invPanel.anchoredPosition = new Vector2(0, 120);

        // Seed buttons above hotbar so they are not covered
        NudgeSeedButton("SeedButton_Carrot", new Vector2(-220, 200));
        NudgeSeedButton("SeedButton_Tomato", new Vector2(0, 200));
        NudgeSeedButton("SeedButton_Pumpkin", new Vector2(220, 200));

        // Keep toolbar above most HUD but under pause/orders if present
        var orders = canvas.transform.Find("OrdersPanel");
        if (orders != null)
            bar.transform.SetSiblingIndex(orders.GetSiblingIndex());
        else
            bar.transform.SetAsLastSibling();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, MainScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[FarmSim] Tools toolbar wired → " + MainScenePath);
    }

    static void WirePlotPrefab()
    {
        var prefabRoot = PrefabUtility.LoadPrefabContents(PlotPrefabPath);
        try
        {
            var plot = prefabRoot.GetComponent<Plot>();
            var soil = prefabRoot.GetComponent<SpriteRenderer>();
            if (plot != null && soil != null)
            {
                var so = new SerializedObject(plot);
                so.FindProperty("soilRenderer").objectReferenceValue = soil;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PlotPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    static ToolData EnsureTool(string fileName, string displayName, ToolType type, string tileName, int price)
    {
        string path = $"{ToolsFolder}/{fileName}.asset";
        var tool = AssetDatabase.LoadAssetAtPath<ToolData>(path);
        if (tool == null)
        {
            tool = ScriptableObject.CreateInstance<ToolData>();
            AssetDatabase.CreateAsset(tool, path);
        }

        tool.displayName = displayName;
        tool.type = type;
        tool.price = price;
        tool.icon = string.IsNullOrEmpty(tileName) ? null : LoadTileSprite(tileName);
        EditorUtility.SetDirty(tool);
        return tool;
    }

    static Sprite LoadTileSprite(string tileName)
    {
        string path = $"Assets/Art/TinyFarm/Tiles/{tileName}.png";
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault()
               ?? AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static ToolSlot EnsureSlotPrefab()
    {
        EnsureFolders();

        if (AssetDatabase.LoadAssetAtPath<GameObject>(SlotPrefabPath) != null)
            AssetDatabase.DeleteAsset(SlotPrefabPath);

        var root = new GameObject("ToolSlot", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        var rootImg = root.GetComponent<Image>();
        rootImg.color = SlotEmpty;
        var le = root.GetComponent<LayoutElement>();
        le.preferredWidth = 72;
        le.preferredHeight = 72;
        le.minWidth = 72;
        le.minHeight = 72;

        var rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(72, 72);

        var inset = new GameObject("Inset", typeof(RectTransform), typeof(Image));
        inset.transform.SetParent(root.transform, false);
        var insetRt = inset.GetComponent<RectTransform>();
        insetRt.anchorMin = Vector2.zero;
        insetRt.anchorMax = Vector2.one;
        insetRt.offsetMin = new Vector2(3, 3);
        insetRt.offsetMax = new Vector2(-3, -3);
        var insetImg = inset.GetComponent<Image>();
        insetImg.color = Cream;
        insetImg.raycastTarget = false;

        var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(root.transform, false);
        var iconRt = iconGo.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.5f, 0.5f);
        iconRt.anchorMax = new Vector2(0.5f, 0.5f);
        iconRt.pivot = new Vector2(0.5f, 0.5f);
        iconRt.anchoredPosition = new Vector2(0, 2);
        iconRt.sizeDelta = new Vector2(48, 48);
        var icon = iconGo.GetComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        icon.enabled = false;

        var keyGo = new GameObject("Key", typeof(RectTransform));
        keyGo.transform.SetParent(root.transform, false);
        var keyRt = keyGo.GetComponent<RectTransform>();
        keyRt.anchorMin = new Vector2(0, 1);
        keyRt.anchorMax = new Vector2(0, 1);
        keyRt.pivot = new Vector2(0, 1);
        keyRt.anchoredPosition = new Vector2(4, -2);
        keyRt.sizeDelta = new Vector2(24, 20);
        var keyTmp = keyGo.AddComponent<TextMeshProUGUI>();
        keyTmp.text = "1";
        keyTmp.fontSize = 16;
        keyTmp.fontStyle = FontStyles.Bold;
        keyTmp.alignment = TextAlignmentOptions.TopLeft;
        keyTmp.color = new Color(0.25f, 0.15f, 0.08f, 0.9f);
        keyTmp.raycastTarget = false;

        var slot = root.AddComponent<ToolSlot>();
        var so = new SerializedObject(slot);
        so.FindProperty("background").objectReferenceValue = rootImg;
        so.FindProperty("icon").objectReferenceValue = icon;
        so.FindProperty("keyLabel").objectReferenceValue = keyTmp;
        so.FindProperty("selectButton").objectReferenceValue = root.GetComponent<Button>();
        so.FindProperty("normalColor").colorValue = SlotEmpty;
        so.FindProperty("selectedColor").colorValue = SlotSelected;
        so.ApplyModifiedPropertiesWithoutUndo();

        root.SetActive(false);
        PrefabUtility.SaveAsPrefabAsset(root, SlotPrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        var loaded = AssetDatabase.LoadAssetAtPath<GameObject>(SlotPrefabPath);
        return loaded.GetComponent<ToolSlot>();
    }

    static void NudgeSeedButton(string name, Vector2 anchoredPos)
    {
        var go = GameObject.Find(name);
        if (go == null) return;
        var rt = go.GetComponent<RectTransform>();
        if (rt != null)
            rt.anchoredPosition = anchoredPos;
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
        if (!AssetDatabase.IsValidFolder("Assets/Data"))
            AssetDatabase.CreateFolder("Assets", "Data");
        if (!AssetDatabase.IsValidFolder(ToolsFolder))
            AssetDatabase.CreateFolder("Assets/Data", "Tools");
        if (!AssetDatabase.IsValidFolder("Assets/Editor"))
            AssetDatabase.CreateFolder("Assets", "Editor");
    }
}
#endif
