#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Sua loi con ton: dat lai Plot, go collider ChickenShop origin, tieng Viet NPC shop.
/// Menu: FarmSim → Sua loi Bug/UX
/// </summary>
public static class FarmSimBugfixSetup
{
    const string MainScenePath = "Assets/Scenes/Main.unity";
    const string AutoFlag = "Assets/Editor/RUN_BUGFIX_SETUP.flag";
    const string PlotPrefabPath = "Assets/Prefabs/Plot.prefab";

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
                Debug.Log("[FarmSim] Bugfix setup done.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[FarmSim] Bugfix loi: " + ex);
            }
        };
    }

    [MenuItem("FarmSim/Sua loi Bug/UX", false, 24)]
    public static void ExecuteFromMenu()
    {
        Execute();
        EditorUtility.DisplayDialog("Xong", "Da sua Plot / collider / chu NPC shop.", "OK");
    }

    public static void Execute()
    {
        var scene = EditorSceneManager.OpenScene(MainScenePath);

        EnsurePlots();
        EnsurePlotBootstrap();
        StripChickenShopSolidCollider();
        FixNpcShopVietnamese();
        FixHandTool();
        RaiseToastAboveShop();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, MainScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    static void EnsurePlots()
    {
        var existing = Object.FindObjectsByType<Plot>(FindObjectsInactive.Include);
        if (existing != null && existing.Length > 0)
        {
            Debug.Log($"[FarmSim] Da co {existing.Length} Plot — bo qua dat lai.");
            return;
        }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlotPrefabPath);
        if (prefab == null) throw new System.Exception("Thieu Plot prefab: " + PlotPrefabPath);

        var oldRoot = GameObject.Find("PlotsRoot");
        if (oldRoot != null) Object.DestroyImmediate(oldRoot);

        var root = new GameObject("PlotsRoot");
        Vector2 origin = new Vector2(-2.5f, -1.5f);
        float step = 2f;
        var plots = new GameObject[9];
        int i = 0;
        for (int y = 0; y < 3; y++)
        for (int x = 0; x < 3; x++)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = $"Plot_{i}";
            go.transform.SetParent(root.transform, true);
            go.transform.position = new Vector3(origin.x + x * step, origin.y + y * step, 0f);
            if (i >= 6) go.SetActive(false);
            plots[i++] = go;
        }

        var upgrades = GameObject.Find("Upgrades");
        if (upgrades == null) upgrades = new GameObject("Upgrades");
        var unlocker = upgrades.GetComponent<PlotUnlocker>() ?? upgrades.AddComponent<PlotUnlocker>();
        var so = new SerializedObject(unlocker);
        var arr = so.FindProperty("lockedPlots");
        arr.arraySize = 3;
        for (int k = 0; k < 3; k++)
            arr.GetArrayElementAtIndex(k).objectReferenceValue = plots[6 + k];
        so.FindProperty("baseCost").intValue = 100;
        so.FindProperty("itemName").stringValue = "ô đất";
        so.ApplyModifiedPropertiesWithoutUndo();

        var save = Object.FindAnyObjectByType<SaveManager>();
        if (save != null)
        {
            var sSo = new SerializedObject(save);
            sSo.FindProperty("unlocker").objectReferenceValue = unlocker;
            sSo.ApplyModifiedPropertiesWithoutUndo();
        }

        Debug.Log("[FarmSim] Da dat 9 Plot (6 mo, 3 khoa).");
    }

    static void EnsurePlotBootstrap()
    {
        var gm = Object.FindAnyObjectByType<GameManager>();
        if (gm == null) return;
        var boot = gm.GetComponent<PlotFieldBootstrap>() ?? gm.gameObject.AddComponent<PlotFieldBootstrap>();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlotPrefabPath);
        var so = new SerializedObject(boot);
        so.FindProperty("plotPrefab").objectReferenceValue = prefab;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void StripChickenShopSolidCollider()
    {
        var go = GameObject.Find("ChickenShop");
        if (go == null) return;
        foreach (var col in go.GetComponents<Collider2D>())
        {
            if (col != null && !col.isTrigger)
                Object.DestroyImmediate(col);
        }
    }

    static void FixNpcShopVietnamese()
    {
        SetNpcText("NpcShop_Chick", "Chuồng gà", "Muốn mua gà không?");
        SetNpcText("NpcShop_Cow", "Chuồng bò", "Bò sữa đây!");
        SetNpcText("NpcShop_Sheep", "Chuồng cừu", "Lên mua cừu nào!");
        SetNpcText("NpcShop_Seed", "Thương lai hạt", "Hạt giống tươi ngon!");

        SetUnlockerName("ChickenShop", "gà");
        SetUnlockerName("CowShop", "bò");
        SetUnlockerName("SheepShop", "cừu");
        SetUnlockerName("Upgrades", "ô đất");

        var close = GameObject.Find("CloseButton");
        var closeTmp = close != null ? close.GetComponentInChildren<TMPro.TMP_Text>() : null;
        if (closeTmp != null) closeTmp.text = "Đóng (Esc)";

        var title = GameObject.Find("ShopPanel")?.transform.Find("Box/Face/Title")
            ?.GetComponent<TMPro.TMP_Text>();
        if (title != null) title.text = "Cửa hàng";
        var greet = GameObject.Find("ShopPanel")?.transform.Find("Box/Face/Greeting")
            ?.GetComponent<TMPro.TMP_Text>();
        if (greet != null) greet.text = "Bạn muốn mua gì?";
    }

    static void SetNpcText(string goName, string title, string greeting)
    {
        var go = GameObject.Find(goName);
        if (go == null) return;
        var shop = go.GetComponent<NpcShop>();
        if (shop == null) return;
        var so = new SerializedObject(shop);
        so.FindProperty("shopTitle").stringValue = title;
        so.FindProperty("greeting").stringValue = greeting;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetUnlockerName(string goName, string itemName)
    {
        var go = GameObject.Find(goName);
        if (go == null) return;
        var u = go.GetComponent<PlotUnlocker>();
        if (u == null) return;
        var so = new SerializedObject(u);
        so.FindProperty("itemName").stringValue = itemName;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void FixHandTool()
    {
        var hand = AssetDatabase.LoadAssetAtPath<ToolData>("Assets/Data/Tools/Hand.asset");
        if (hand == null) return;
        hand.price = 0;
        hand.displayName = "Tay";
        // Icon: sack/crate fallback
        var sprite = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/TinyFarm/Tiles/tile_0074.png")
            .OfType<Sprite>().FirstOrDefault();
        if (sprite != null) hand.icon = sprite;
        EditorUtility.SetDirty(hand);
    }

    static void RaiseToastAboveShop()
    {
        var toast = GameObject.Find("ToastRoot");
        var shop = GameObject.Find("ShopPanel");
        if (toast == null) return;
        toast.transform.SetAsLastSibling();
        if (shop != null)
        {
            shop.transform.SetAsLastSibling();
            toast.transform.SetAsLastSibling();
        }
    }
}
#endif
