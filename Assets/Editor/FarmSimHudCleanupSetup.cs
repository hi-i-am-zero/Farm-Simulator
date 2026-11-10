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
/// Gon HUD: xoa nut hat / mua o / mua ga / don hang;
/// Kho = icon; Ban het chi hien khi mo inventory.
/// Menu: FarmSim → Gon HUD Buttons
/// </summary>
public static class FarmSimHudCleanupSetup
{
    const string MainScenePath = "Assets/Scenes/Main.unity";
    const string AutoFlag = "Assets/Editor/RUN_HUD_CLEANUP.flag";

    static readonly Color Wood = new(0.55f, 0.32f, 0.14f, 0.98f);
    static readonly Color Cream = new(0.93f, 0.86f, 0.70f, 1f);

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
            try
            {
                Execute();
                Debug.Log("[FarmSim] HUD cleanup done.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[FarmSim] HUD cleanup loi: " + ex);
            }
        };
    }

    [MenuItem("FarmSim/Gon HUD Buttons", false, 22)]
    public static void ExecuteFromMenu()
    {
        Execute();
        EditorUtility.DisplayDialog("Xong", "Da gon HUD: xoa nut thua, Kho = icon, Ban het theo Inventory.", "OK");
    }

    public static void Execute()
    {
        var scene = EditorSceneManager.OpenScene(MainScenePath);
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) throw new System.Exception("Khong tim thay Canvas.");

        string[] remove =
        {
            "SeedButton_Carrot", "SeedButton_Tomato", "SeedButton_Pumpkin",
            "BuyPlotButton", "BuyChickenButton", "OrdersButton"
        };
        foreach (var name in remove)
            DestroyNamed(canvas.transform, name);

        // Orders van mo bang phim O
        EnsureOrdersHotkey(canvas.transform);

        var invPanel = canvas.transform.Find("InventoryPanel")?.gameObject;
        if (invPanel == null) throw new System.Exception("Khong tim thay InventoryPanel.");

        RebuildInventoryIconButton(canvas.transform, invPanel);
        WireSellButtonToInventory(canvas.transform, invPanel);

        // An chu HUD "Ca rot (10$)" khoi day man hinh neu con SelectedLabel/SelectedIcon
        // (giu nhung dua len goc tren-trai gan MoneyText de khong de len map)
        NudgeSelectedHud(canvas.transform);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, MainScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    static void EnsureOrdersHotkey(Transform canvas)
    {
        var ordersPanel = canvas.Find("OrdersPanel")?.gameObject;
        if (ordersPanel == null) return;

        DestroyNamed(canvas, "OrdersHotkey");
        var go = new GameObject("OrdersHotkey");
        go.transform.SetParent(canvas, false);
        var toggle = go.AddComponent<PanelToggle>();
        var so = new SerializedObject(toggle);
        so.FindProperty("panel").objectReferenceValue = ordersPanel;
        so.FindProperty("key").intValue = (int)KeyCode.O;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void RebuildInventoryIconButton(Transform canvas, GameObject invPanel)
    {
        DestroyNamed(canvas, "InventoryButton");

        var sprite = LoadTileSprite("tile_0075") ?? LoadTileSprite("tile_0010");

        var go = new GameObject("InventoryButton", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(canvas, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-20f, 130f);
        rt.sizeDelta = new Vector2(72f, 72f);

        var bg = go.GetComponent<Image>();
        bg.color = Wood;

        var inset = new GameObject("Inset", typeof(RectTransform), typeof(Image));
        inset.transform.SetParent(go.transform, false);
        var insetRt = inset.GetComponent<RectTransform>();
        insetRt.anchorMin = Vector2.zero;
        insetRt.anchorMax = Vector2.one;
        insetRt.offsetMin = new Vector2(4, 4);
        insetRt.offsetMax = new Vector2(-4, -4);
        var insetImg = inset.GetComponent<Image>();
        insetImg.color = Cream;
        insetImg.raycastTarget = false;

        var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(go.transform, false);
        var iconRt = iconGo.GetComponent<RectTransform>();
        iconRt.anchorMin = iconRt.anchorMax = new Vector2(0.5f, 0.5f);
        iconRt.pivot = new Vector2(0.5f, 0.5f);
        iconRt.sizeDelta = new Vector2(48f, 48f);
        var icon = iconGo.GetComponent<Image>();
        icon.sprite = sprite;
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        icon.enabled = sprite != null;

        var toggle = go.AddComponent<PanelToggle>();
        var tSo = new SerializedObject(toggle);
        tSo.FindProperty("panel").objectReferenceValue = invPanel;
        tSo.FindProperty("key").intValue = (int)KeyCode.I;
        tSo.ApplyModifiedPropertiesWithoutUndo();
        UnityEventTools.AddPersistentListener(go.GetComponent<Button>().onClick, toggle.Toggle);

        // Inventory text HUD: bo "(I)" neu con
        var invText = GameObject.Find("InventoryText")?.GetComponent<TMP_Text>();
        if (invText != null && invText.text.Contains("(I)"))
            invText.text = invText.text.Replace(" (I)", "").Replace("(I)", "");
    }

    static void WireSellButtonToInventory(Transform canvas, GameObject invPanel)
    {
        var sell = canvas.Find("SellButton")?.gameObject;
        if (sell == null)
            sell = GameObject.Find("SellButton");
        if (sell == null) return;

        // Dat trong panel inventory (an theo panel; activeSelf=true de hien khi mo Kho)
        sell.transform.SetParent(invPanel.transform, false);
        var rt = sell.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-18f, 18f);
            rt.sizeDelta = new Vector2(160f, 52f);
        }
        sell.SetActive(true);
    }

    static void NudgeSelectedHud(Transform canvas)
    {
        var icon = canvas.Find("SelectedIcon") as RectTransform;
        if (icon != null)
        {
            icon.anchorMin = icon.anchorMax = new Vector2(0f, 1f);
            icon.pivot = new Vector2(0f, 1f);
            icon.anchoredPosition = new Vector2(16f, -70f);
            icon.sizeDelta = new Vector2(56f, 56f);
        }

        var label = canvas.Find("SelectedLabel") as RectTransform;
        if (label != null)
        {
            label.anchorMin = label.anchorMax = new Vector2(0f, 1f);
            label.pivot = new Vector2(0f, 1f);
            label.anchoredPosition = new Vector2(80f, -78f);
            label.sizeDelta = new Vector2(320f, 40f);
        }

        var labelTmp = label != null ? label.GetComponent<TMP_Text>() : null;
        if (labelTmp != null)
        {
            labelTmp.fontSize = 26;
            labelTmp.raycastTarget = false;
        }
    }

    static Sprite LoadTileSprite(string tileName)
    {
        string path = $"Assets/Art/TinyFarm/Tiles/{tileName}.png";
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault()
               ?? AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static void DestroyNamed(Transform parent, string name)
    {
        var t = parent.Find(name);
        if (t != null) Object.DestroyImmediate(t.gameObject);
        else
        {
            var go = GameObject.Find(name);
            if (go != null) Object.DestroyImmediate(go);
        }
    }
}
#endif
