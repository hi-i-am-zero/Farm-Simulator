#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

/// <summary>
/// Gan cua hang NPC: ga / bo / cuu / hat giong. Bam E de mua.
/// Menu: FarmSim → Tao NPC Shop
/// </summary>
public static class FarmSimNpcShopSetup
{
    const string MainScenePath = "Assets/Scenes/Main.unity";
    const string AutoFlag = "Assets/Editor/RUN_NPC_SHOP_SETUP.flag";

    static readonly Color Wood = new(0.55f, 0.32f, 0.14f, 0.98f);
    static readonly Color Cream = new(0.93f, 0.86f, 0.70f, 1f);
    static readonly Color BtnGreen = new(0.18f, 0.45f, 0.28f, 0.95f);

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
                Debug.Log("[FarmSim] NPC shop setup done.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[FarmSim] NPC shop setup loi: " + ex);
            }
        };
    }

    [MenuItem("FarmSim/Tao NPC Shop", false, 23)]
    public static void ExecuteFromMenu()
    {
        Execute();
        EditorUtility.DisplayDialog("Xong", "Da tao cua hang NPC (E de noi chuyen / mua).", "OK");
    }

    public static void Execute()
    {
        var scene = EditorSceneManager.OpenScene(MainScenePath);
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) throw new System.Exception("Khong tim thay Canvas.");

        var carrot = AssetDatabase.LoadAssetAtPath<CropData>("Assets/Data/Crops/Carrot.asset");
        var tomato = AssetDatabase.LoadAssetAtPath<CropData>("Assets/Data/Crops/Tomato.asset");
        var pumpkin = AssetDatabase.LoadAssetAtPath<CropData>("Assets/Data/Crops/Pumpkin.asset");

        var chickens = FindAnimals("Chicken").Take(3).ToArray();
        var cows = FindAnimals("Cow").Take(4).ToArray();
        var sheep = FindAnimals("Sheep").Take(4).ToArray();

        // Ban dau khoa het (phai mua o NPC)
        foreach (var go in chickens.Concat(cows).Concat(sheep))
            if (go != null) go.SetActive(false);

        var chickenShop = EnsureUnlocker("ChickenShop", chickens, 150, "ga");
        var cowShop = EnsureUnlocker("CowShop", cows, 250, "bo");
        var sheepShop = EnsureUnlocker("SheepShop", sheep, 200, "cuu");

        WireSaveManager(chickenShop, cowShop, sheepShop);

        // Trigger gan NPC (dung ClosestPoint / transform con)
        CreateNpcTrigger("NpcShop_Chick", FindNpcWorldPos("NpcChick", new Vector3(6.5f, -14.5f, 0f)),
            "Chuong ga", "Muon mua ga khong?", AnimalOffer(chickenShop));
        CreateNpcTrigger("NpcShop_Cow", FindNpcWorldPos("NpcCow", new Vector3(-6.5f, -25.5f, 0f)),
            "Chuong bo", "Bo sua day!", AnimalOffer(cowShop));
        CreateNpcTrigger("NpcShop_Sheep", FindNpcWorldPos("NpcSheep", new Vector3(1.5f, -23.5f, 0f)),
            "Chuong cuu", "Len mua cuu nao!", AnimalOffer(sheepShop));

        var seedOffers = new[]
        {
            SeedOffer(carrot), SeedOffer(tomato), SeedOffer(pumpkin)
        };
        CreateNpcTrigger("NpcShop_Seed", FindNpcWorldPos("NPCcrop", new Vector3(0.5f, -8f, 0f)),
            "Thuong lai hat", "Hat giong tuoi ngon!", seedOffers);

        BuildShopUi(canvas.transform);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, MainScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    static GameObject[] FindAnimals(string typeName)
    {
        var list = new List<GameObject>();
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include))
        {
            if (go == null) continue;
            // Prefab instance roots: Chicken, Cow, Sheep, Chicken_0, Cow (1)...
            if (go.GetComponent<AnimalWander>() == null && go.GetComponent<Chicken>() == null)
                continue;
            if (!go.name.StartsWith(typeName)) continue;
            list.Add(go);
        }

        return list
            .OrderBy(g => g.transform.position.x)
            .ThenBy(g => g.transform.position.y)
            .ToArray();
    }

    static PlotUnlocker EnsureUnlocker(string name, GameObject[] locked, int baseCost, string itemName)
    {
        var go = GameObject.Find(name);
        if (go == null) go = new GameObject(name);
        var unlocker = go.GetComponent<PlotUnlocker>() ?? go.AddComponent<PlotUnlocker>();
        var so = new SerializedObject(unlocker);
        var arr = so.FindProperty("lockedPlots");
        arr.arraySize = locked.Length;
        for (int i = 0; i < locked.Length; i++)
            arr.GetArrayElementAtIndex(i).objectReferenceValue = locked[i];
        so.FindProperty("baseCost").intValue = baseCost;
        so.FindProperty("itemName").stringValue = itemName;
        so.FindProperty("buttonLabel").objectReferenceValue = null;
        so.ApplyModifiedPropertiesWithoutUndo();
        return unlocker;
    }

    static void WireSaveManager(PlotUnlocker chicken, PlotUnlocker cow, PlotUnlocker sheep)
    {
        var save = Object.FindAnyObjectByType<SaveManager>();
        if (save == null) return;
        var so = new SerializedObject(save);
        so.FindProperty("chickenUnlocker").objectReferenceValue = chicken;
        var cowProp = so.FindProperty("cowUnlocker");
        var sheepProp = so.FindProperty("sheepUnlocker");
        if (cowProp != null) cowProp.objectReferenceValue = cow;
        if (sheepProp != null) sheepProp.objectReferenceValue = sheep;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static Vector3 FindNpcWorldPos(string npcName, Vector3 fallback)
    {
        var go = GameObject.Find(npcName);
        if (go == null) return fallback;

        var tm = go.GetComponent<Tilemap>();
        if (tm != null)
        {
            foreach (var pos in tm.cellBounds.allPositionsWithin)
            {
                if (!tm.HasTile(pos)) continue;
                return tm.GetCellCenterWorld(pos);
            }
        }

        if (go.transform.position.sqrMagnitude > 0.01f)
            return go.transform.position;
        return fallback;
    }

    static ShopOffer AnimalOffer(PlotUnlocker shop)
    {
        return new ShopOffer
        {
            kind = ShopOfferKind.Animal,
            animalShop = shop,
            label = "Mua"
        };
    }

    static ShopOffer SeedOffer(CropData crop)
    {
        return new ShopOffer
        {
            kind = ShopOfferKind.Seed,
            seedCrop = crop,
            label = crop != null ? crop.cropName : "Hat"
        };
    }

    static void CreateNpcTrigger(string name, Vector3 worldPos, string title, string greeting, params ShopOffer[] offers)
    {
        var existing = GameObject.Find(name);
        if (existing != null) Object.DestroyImmediate(existing);

        var go = new GameObject(name);
        go.transform.position = worldPos;

        var box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(1.6f, 1.6f);

        var shop = go.AddComponent<NpcShop>();
        var so = new SerializedObject(shop);
        so.FindProperty("shopTitle").stringValue = title;
        so.FindProperty("greeting").stringValue = greeting;
        var arr = so.FindProperty("offers");
        arr.arraySize = offers.Length;
        for (int i = 0; i < offers.Length; i++)
        {
            var el = arr.GetArrayElementAtIndex(i);
            el.FindPropertyRelative("label").stringValue = offers[i].label;
            el.FindPropertyRelative("kind").enumValueIndex = (int)offers[i].kind;
            el.FindPropertyRelative("animalShop").objectReferenceValue = offers[i].animalShop;
            el.FindPropertyRelative("seedCrop").objectReferenceValue = offers[i].seedCrop;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void BuildShopUi(Transform canvas)
    {
        DestroyNamed(canvas, "ShopPanel");
        DestroyNamed(canvas, "ShopUIRoot");

        var panel = new GameObject("ShopPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvas, false);
        panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.45f);
        var panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;

        var box = new GameObject("Box", typeof(RectTransform), typeof(Image));
        box.transform.SetParent(panel.transform, false);
        box.GetComponent<Image>().color = Wood;
        var boxRt = box.GetComponent<RectTransform>();
        boxRt.anchorMin = boxRt.anchorMax = new Vector2(0.5f, 0.5f);
        boxRt.sizeDelta = new Vector2(520f, 420f);

        var face = new GameObject("Face", typeof(RectTransform), typeof(Image));
        face.transform.SetParent(box.transform, false);
        face.GetComponent<Image>().color = Cream;
        var faceRt = face.GetComponent<RectTransform>();
        faceRt.anchorMin = Vector2.zero;
        faceRt.anchorMax = Vector2.one;
        faceRt.offsetMin = new Vector2(10, 10);
        faceRt.offsetMax = new Vector2(-10, -10);

        var title = CreateTmp(face.transform, "Title", "Cua hang",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -28), new Vector2(440, 40), 34, true);
        var greet = CreateTmp(face.transform, "Greeting", "Ban muon mua gi?",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -68), new Vector2(440, 36), 22, false);
        greet.color = new Color(0.3f, 0.2f, 0.1f, 1f);

        var offerRoot = new GameObject("Offers", typeof(RectTransform), typeof(VerticalLayoutGroup));
        offerRoot.transform.SetParent(face.transform, false);
        var offerRt = offerRoot.GetComponent<RectTransform>();
        offerRt.anchorMin = new Vector2(0.5f, 0.5f);
        offerRt.anchorMax = new Vector2(0.5f, 0.5f);
        offerRt.anchoredPosition = new Vector2(0, -10);
        offerRt.sizeDelta = new Vector2(440, 240);
        var vlg = offerRoot.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 10;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlHeight = false;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;

        var offerPrefabGo = CreateOfferButton(null, "OfferButtonPrefab");
        offerPrefabGo.SetActive(false);
        // Keep prefab under ShopUIRoot (not destroyed with panel close)
        var uiRoot = new GameObject("ShopUIRoot");
        var offerPrefab = Object.Instantiate(offerPrefabGo);
        offerPrefab.name = "OfferButtonPrefab";
        offerPrefab.transform.SetParent(uiRoot.transform, false);
        offerPrefab.SetActive(false);
        Object.DestroyImmediate(offerPrefabGo);

        var closeBtn = CreateOfferButton(face.transform, "CloseButton");
        closeBtn.GetComponent<Image>().color = new Color(0.55f, 0.22f, 0.18f, 1f);
        var closeRt = closeBtn.GetComponent<RectTransform>();
        closeRt.anchorMin = closeRt.anchorMax = new Vector2(0.5f, 0f);
        closeRt.pivot = new Vector2(0.5f, 0f);
        closeRt.anchoredPosition = new Vector2(0, 18);
        closeRt.sizeDelta = new Vector2(200, 48);
        var closeTmp = closeBtn.GetComponentInChildren<TMP_Text>();
        if (closeTmp != null) closeTmp.text = "Dong (Esc)";

        var shopUi = uiRoot.AddComponent<ShopUI>();
        var so = new SerializedObject(shopUi);
        so.FindProperty("panel").objectReferenceValue = panel;
        so.FindProperty("titleText").objectReferenceValue = title;
        so.FindProperty("greetingText").objectReferenceValue = greet;
        so.FindProperty("offerRoot").objectReferenceValue = offerRoot.transform;
        so.FindProperty("offerButtonPrefab").objectReferenceValue = offerPrefab.GetComponent<Button>();
        so.FindProperty("closeButton").objectReferenceValue = closeBtn.GetComponent<Button>();
        so.ApplyModifiedPropertiesWithoutUndo();

        panel.SetActive(false);
        panel.transform.SetAsLastSibling();
    }

    static GameObject CreateOfferButton(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        if (parent != null) go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = BtnGreen;
        var le = go.GetComponent<LayoutElement>();
        le.preferredHeight = 52;
        le.minHeight = 52;
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(400, 52);

        var tmp = CreateTmp(go.transform, "Text", "Mua",
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 26, true);
        tmp.rectTransform.offsetMin = Vector2.zero;
        tmp.rectTransform.offsetMax = Vector2.zero;
        tmp.alignment = TextAlignmentOptions.Center;
        return go;
    }

    static TMP_Text CreateTmp(Transform parent, string name, string text,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size, float fontSize, bool bold)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        return tmp;
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
