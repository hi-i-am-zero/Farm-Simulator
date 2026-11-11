#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

/// <summary>
/// Thiết lập tự động 4 NPC Shop: Gà, Bò, Cừu, Hạt giống & Nông sản.
/// - Đầy đủ 10 con Gà (20$, +20$), 10 con Bò (100$, +50$), 10 con Cừu (50$, +30$).
/// - 4 Cây trồng: Lúa (5$), Đậu (10$), Ngô (15$), Cà chua (20$). Giá bán = Mua + 50%.
/// - Trứng (10$), Sữa bò (20$), Lông cừu (20$).
/// - Giao diện chữ to rõ nét, có thanh cuộn mượt mà.
/// Menu: FarmSim → Tao NPC Shop
/// </summary>
public static class FarmSimNpcShopSetup
{
    const string MainScenePath = "Assets/Scenes/Main.unity";
    const string AutoFlag = "Assets/Editor/RUN_NPC_SHOP_SETUP.flag";

    static readonly Color ColorWood = new(0.35f, 0.22f, 0.12f, 0.98f);
    static readonly Color ColorFace = new(0.92f, 0.88f, 0.80f, 1f);
    static readonly Color ColorBtnGreen = new(0.13f, 0.58f, 0.28f, 1f);
    static readonly Color ColorBtnInactiveTab = new(0.50f, 0.42f, 0.35f, 0.95f);

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
                Debug.Log("[FarmSim] NPC shop setup hoàn tất thành công.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[FarmSim] NPC shop setup lỗi: " + ex);
            }
        };
    }

    [MenuItem("FarmSim/Tao NPC Shop", false, 23)]
    public static void ExecuteFromMenu()
    {
        Execute();
        EditorUtility.DisplayDialog("Xong", "Đã thiết lập hệ thống Cửa hàng cho 4 NPC với 10 con gia súc mỗi loại và giao diện chữ sắc nét!", "OK");
    }

    public static void EnsureConfiguredIfNeeded()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        var sheepShop = GameObject.Find("NpcShop_Sheep");
        var sheepSamples = GameObject.Find("SheepSampleRoot");
        var firstSampleSheep = GameObject.Find("SampleSheep_0");
        var legacyTileShop = GameObject.Find("NpcShop_TileSheep");
        var sheepsRoot = GameObject.Find("SheepsRoot");
        var chickensRoot = GameObject.Find("ChickensRoot");
        var sampleCol = sheepSamples != null ? sheepSamples.GetComponentInChildren<Collider2D>(true) : null;
        bool sampleNotTileCentered = firstSampleSheep == null ||
            Mathf.Abs(Mathf.Abs(firstSampleSheep.transform.position.x % 1f) - 0.5f) > 0.1f;
        bool missingAnimalProduce = sheepsRoot == null || sheepsRoot.GetComponentInChildren<AnimalProduce>(true) == null;
        bool hasLegacyChicken = chickensRoot != null && chickensRoot.GetComponentInChildren<Chicken>(true) != null;
        if (sheepShop == null || sheepShop.transform.position.y > 0f ||
            sheepShop.GetComponent<SpriteRenderer>() != null ||
            sheepShop.transform.Find("Badge") == null ||
            sheepShop.GetComponent<BoxCollider2D>() == null ||
            sheepSamples == null || sheepSamples.transform.childCount < 4 ||
            (sampleCol != null && !sampleCol.enabled) ||
            sampleNotTileCentered ||
            missingAnimalProduce ||
            hasLegacyChicken ||
            legacyTileShop != null)
        {
            Execute();
        }
    }

    public static void Execute()
    {
        var scene = EditorSceneManager.OpenScene(MainScenePath);
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) throw new System.Exception("Không tìm thấy GameObject Canvas trong Scene.");

        // 1. Nạp 4 nông sản theo yêu cầu
        var rice = AssetDatabase.LoadAssetAtPath<CropData>("Assets/Data/Crops/Rice.asset");       // Lúa: 5$ -> 8$
        var bean = AssetDatabase.LoadAssetAtPath<CropData>("Assets/Data/Crops/Carrot.asset");     // Đậu: 10$ -> 15$
        var corn = AssetDatabase.LoadAssetAtPath<CropData>("Assets/Data/Crops/Corn.asset");       // Ngô: 15$ -> 23$
        var tomato = AssetDatabase.LoadAssetAtPath<CropData>("Assets/Data/Crops/Tomato.asset");   // Cà chua: 20$ -> 30$

        // 2. Nạp sản phẩm chăn nuôi
        var egg = AssetDatabase.LoadAssetAtPath<CropData>("Assets/Data/Crops/Egg.asset");         // Trứng gà: 10$
        var milk = AssetDatabase.LoadAssetAtPath<CropData>("Assets/Data/Crops/Milk.asset");       // Sữa bò: 20$
        var wool = AssetDatabase.LoadAssetAtPath<CropData>("Assets/Data/Crops/Wool.asset");       // Lông cừu: 20$

        // Xóa trigger tạm NpcShop_Tile* (nếu có) để chỉ dùng đúng 3 NPC sẵn có ở phía dưới
        DestroyImmediateIfExists("NpcShop_TileSheep");
        DestroyImmediateIfExists("NpcShop_TileChick");
        DestroyImmediateIfExists("NpcShop_TileCow");

        // 3. Chuẩn bị đủ 10 con mỗi loại đặt chính xác vào tâm ô trong 3 chuồng của người chơi (phía trên):
        var sheep = EnsureAnimalList("Sheep", "Assets/Prefabs/Sheep.prefab", 10,
            new Vector2(-19.5f, 5.5f), new Vector2(-15.5f, 7.5f), wool);

        var chickens = EnsureAnimalList("Chicken", "Assets/Prefabs/Chicken.prefab", 10,
            new Vector2(-6.5f, 13.5f), new Vector2(-3.5f, 14.5f), egg);

        var cows = EnsureAnimalList("Cow", "Assets/Prefabs/Cow.prefab", 10,
            new Vector2(4.5f, 13.5f), new Vector2(9.5f, 14.5f), milk);

        // 3b. Đặt 4 con vật làm mẫu (không thể tương tác) đúng tâm ô .5f trong chuồng 3 NPC phía dưới
        EnsureSampleAnimals("Sheep", "Assets/Prefabs/Sheep.prefab", new[]
        {
            new Vector3(5.5f, -23.5f, 0f), new Vector3(7.5f, -23.5f, 0f),
            new Vector3(6.5f, -22.5f, 0f), new Vector3(8.5f, -22.5f, 0f)
        });
        EnsureSampleAnimals("Chicken", "Assets/Prefabs/Chicken.prefab", new[]
        {
            new Vector3(8.5f, -14.5f, 0f), new Vector3(10.5f, -14.5f, 0f),
            new Vector3(9.5f, -13.5f, 0f), new Vector3(11.5f, -13.5f, 0f)
        });
        EnsureSampleAnimals("Cow", "Assets/Prefabs/Cow.prefab", new[]
        {
            new Vector3(-15.5f, -25.5f, 0f), new Vector3(-13.5f, -25.5f, 0f),
            new Vector3(-14.5f, -24.5f, 0f), new Vector3(-12.5f, -24.5f, 0f)
        });

        // 4. Thiết lập PlotUnlocker cho từng loại (Gà: 20$ tăng 20$, Bò: 100$ tăng 50$, Cừu: 50$ tăng 30$)
        var sheepShop = EnsureUnlocker("SheepShop", sheep, 50, 30, "Cừu");
        var chickenShop = EnsureUnlocker("ChickenShop", chickens, 20, 20, "Gà");
        var cowShop = EnsureUnlocker("CowShop", cows, 100, 50, "Bò");

        WireSaveManager(chickenShop, cowShop, sheepShop);

        // 5. Chỉ giữ 3 NPC bán gia súc sẵn có ở phía dưới (gắn thẳng vào vị trí NpcSheep, NpcChick, NpcCow)
        CreateNpcTrigger("NpcShop_Sheep",
            FindNpcWorldPos("NpcSheep", new Vector3(1.5f, -23.5f, 0f)),
            "Trang Trại Cừu",
            "Chào bạn! Tôi bán cừu lấy lông (tối đa 10 con) và thu mua lông cừu.",
            new[] { AnimalOffer(sheepShop) },
            new[] { wool },
            null
        );

        CreateNpcTrigger("NpcShop_Chick",
            FindNpcWorldPos("NpcChick", new Vector3(6.5f, -14.5f, 0f)),
            "Trang Trại Gà",
            "Chào bạn! Tôi bán gà giống (tối đa 10 con) và thu mua trứng gà tươi.",
            new[] { AnimalOffer(chickenShop) },
            new[] { egg },
            null
        );

        CreateNpcTrigger("NpcShop_Cow",
            FindNpcWorldPos("NpcCow", new Vector3(-6.5f, -25.5f, 0f)),
            "Trang Trại Bò",
            "Chào bạn! Tôi cung cấp bò sữa (tối đa 10 con) và thu mua sữa bò.",
            new[] { AnimalOffer(cowShop) },
            new[] { milk },
            null
        );

        // 8. NPC CÂY TRỒNG (Góc dưới phải vườn cây mẫu 4 luống): Bán 4 hạt giống, mua 4 nông sản
        var seedBuyOffers = new[]
        {
            SeedOffer(rice),
            SeedOffer(bean),
            SeedOffer(corn),
            SeedOffer(tomato)
        };
        var cropsToSell = new[] { rice, bean, corn, tomato };

        CreateNpcTrigger("NpcShop_Seed",
            FindNpcWorldPos("NPCcrop", new Vector3(-12.5f, -18.5f, 0f)),
            "Cửa Hàng Hạt Giống & Nông Sản",
            "Hạt giống tươi tốt và thu mua nông sản giá cao nhất vùng!",
            seedBuyOffers,
            cropsToSell,
            null
        );

        // 9. Xây dựng giao diện ShopUI với ScrollView cuộn mượt mà
        BuildShopUi(canvas.transform);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, MainScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// Đảm bảo luôn có đủ count con vật trong cảnh, dọn sạch con cũ và đặt vào đúng chuồng
    /// </summary>
    static GameObject[] EnsureAnimalList(string typeName, string prefabPath, int count, Vector2 minPos, Vector2 maxPos, CropData productItem)
    {
        // Dọn dẹp sạch sẽ toàn bộ động vật cũ hoặc phế tích còn lại trên Scene
        CleanOldAnimals(typeName);

        string rootName = typeName + "sRoot";
        var root = new GameObject(rootName);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        var list = new List<GameObject>();

        for (int i = 0; i < count; i++)
        {
            GameObject newGo;
            if (prefab != null)
            {
                newGo = UnityEngine.Object.Instantiate(prefab);
            }
            else
            {
                newGo = new GameObject($"{typeName}_{i}");
            }

            newGo.name = $"{typeName}_{i}";
            newGo.transform.SetParent(root.transform, false);

            // Rải đều 10 con vào bên trong chuồng
            int col = i % 4;
            int row = i / 4;
            float px = Mathf.Lerp(minPos.x, maxPos.x, col / 3f);
            float py = Mathf.Lerp(minPos.y, maxPos.y, row / 2.5f);
            newGo.transform.position = new Vector3(px, py, 0f);

            NpcShopBootstrap.SetupBoughtAnimalProduce(newGo, productItem, 30f);
            var produce = newGo.GetComponent<AnimalProduce>();
            if (produce != null)
            {
                var pSo = new SerializedObject(produce);
                pSo.FindProperty("productItem").objectReferenceValue = productItem;
                pSo.FindProperty("produceInterval").floatValue = 30f;
                pSo.ApplyModifiedPropertiesWithoutUndo();
            }

            // Ban đầu tắt (khi người chơi mua con nào thì con đó sẽ lập tức xuất hiện trong chuồng)
            newGo.SetActive(false);
            list.Add(newGo);
        }

        return list.ToArray();
    }

    static void DestroyImmediateIfExists(string name)
    {
        var go = GameObject.Find(name);
        if (go != null) Object.DestroyImmediate(go);
    }

    static void EnsureSampleAnimals(string typeName, string prefabPath, Vector3[] positions)
    {
        string rootName = typeName + "SampleRoot";
        DestroyImmediateIfExists(rootName);

        var root = new GameObject(rootName);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        for (int i = 0; i < positions.Length; i++)
        {
            GameObject sampleGo = prefab != null
                ? Object.Instantiate(prefab)
                : new GameObject($"Sample{typeName}_{i}");

            sampleGo.name = $"Sample{typeName}_{i}";
            sampleGo.transform.SetParent(root.transform, false);
            sampleGo.transform.position = positions[i];
            sampleGo.SetActive(true);

            NpcShopBootstrap.MakeSampleAnimalNonInteractable(sampleGo);
            var wander = sampleGo.GetComponent<AnimalWander>();
            if (wander != null)
            {
                var wSo = new SerializedObject(wander);
                var prop = wSo.FindProperty("maxStepFromHome");
                if (prop != null)
                {
                    prop.intValue = 2;
                    wSo.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            EditorUtility.SetDirty(sampleGo);
        }
    }

    static void CleanOldAnimals(string typeName)
    {
        var root1 = GameObject.Find(typeName + "sRoot");
        if (root1 != null) Object.DestroyImmediate(root1);
        var root2 = GameObject.Find(typeName + "Root");
        if (root2 != null) Object.DestroyImmediate(root2);

        var allGos = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
        foreach (var go in allGos)
        {
            if (go == null) continue;
            if (go.name.Contains("Shop") || go.name.Contains("Trigger") || go.name.Contains("Npc") || go.name.Contains("Sample"))
                continue;

            bool isAnimalComponent = go.GetComponent<AnimalWander>() != null || go.GetComponent<Chicken>() != null;
            bool nameMatch = go.name.StartsWith(typeName, System.StringComparison.OrdinalIgnoreCase);

            if (nameMatch && (isAnimalComponent || go.name.Contains("(") || go.name.Contains("_") || go.name.Equals(typeName, System.StringComparison.OrdinalIgnoreCase)))
            {
                if (PrefabUtility.IsPartOfPrefabInstance(go))
                {
                    var pRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(go);
                    if (pRoot != null && !pRoot.name.Contains("Shop"))
                    {
                        Object.DestroyImmediate(pRoot);
                        continue;
                    }
                }
                Object.DestroyImmediate(go);
            }
        }
    }

    static PlotUnlocker EnsureUnlocker(string name, GameObject[] locked, int baseCost, int stepCost, string itemName)
    {
        var go = GameObject.Find(name);
        if (go == null) go = new GameObject(name);

        var strayCol = go.GetComponent<Collider2D>();
        if (strayCol != null) Object.DestroyImmediate(strayCol);

        var unlocker = go.GetComponent<PlotUnlocker>();
        if (unlocker == null) unlocker = go.AddComponent<PlotUnlocker>();
        var so = new SerializedObject(unlocker);
        var arr = so.FindProperty("lockedPlots");
        arr.arraySize = locked.Length;
        for (int i = 0; i < locked.Length; i++)
            arr.GetArrayElementAtIndex(i).objectReferenceValue = locked[i];
        so.FindProperty("baseCost").intValue = baseCost;
        so.FindProperty("stepCost").intValue = stepCost;
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
            label = crop != null ? crop.cropName : "Hạt"
        };
    }

    static void CreateNpcTrigger(string name, Vector3 worldPos, string title, string greeting,
        ShopOffer[] buyOffers, CropData[] sellCrops, Sprite npcSprite = null)
    {
        var existing = GameObject.Find(name);
        if (existing != null) Object.DestroyImmediate(existing);

        var go = new GameObject(name);
        go.transform.position = worldPos;

        if (npcSprite != null)
        {
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = npcSprite;
            sr.sortingLayerName = "Player";
            sr.sortingOrder = 0;
        }

        var box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(2.8f, 2.8f);

        // Biển tên nổi [E] phía trên đầu để người chơi luôn thấy rõ
        var badgeGo = new GameObject("Badge");
        badgeGo.transform.SetParent(go.transform, false);
        badgeGo.transform.localPosition = new Vector3(0, 0.9f, 0);

        var tmp = badgeGo.AddComponent<TextMeshPro>();
        tmp.text = $"<color=#FFD700><b>[E]</b></color>\n<size=80%>{title}</size>";
        tmp.fontSize = 2.2f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.sortingLayerID = SortingLayer.NameToID("Player");
        tmp.sortingOrder = 10;

        var shop = go.AddComponent<NpcShop>();
        var so = new SerializedObject(shop);
        so.FindProperty("shopTitle").stringValue = title;
        so.FindProperty("greeting").stringValue = greeting;

        var arrOffers = so.FindProperty("offers");
        var arrBuyOffers = so.FindProperty("buyOffers");
        arrOffers.arraySize = buyOffers.Length;
        arrBuyOffers.arraySize = buyOffers.Length;

        for (int i = 0; i < buyOffers.Length; i++)
        {
            void SetOffer(SerializedProperty arr)
            {
                var el = arr.GetArrayElementAtIndex(i);
                el.FindPropertyRelative("label").stringValue = buyOffers[i].label;
                el.FindPropertyRelative("kind").enumValueIndex = (int)buyOffers[i].kind;
                el.FindPropertyRelative("animalShop").objectReferenceValue = buyOffers[i].animalShop;
                el.FindPropertyRelative("seedCrop").objectReferenceValue = buyOffers[i].seedCrop;
            }
            SetOffer(arrOffers);
            SetOffer(arrBuyOffers);
        }

        var arrSell = so.FindProperty("sellCrops");
        arrSell.arraySize = sellCrops.Length;
        for (int i = 0; i < sellCrops.Length; i++)
        {
            arrSell.GetArrayElementAtIndex(i).objectReferenceValue = sellCrops[i];
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void BuildShopUi(Transform canvas)
    {
        DestroyNamed(canvas, "ShopPanel");
        DestroyNamed(canvas, "ShopUIRoot");

        // 1. Panel mờ phủ toàn màn hình
        var panel = new GameObject("ShopPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvas, false);
        panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);
        var panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;

        // 2. Khung viền gỗ lớn (620 x 530)
        var box = new GameObject("Box", typeof(RectTransform), typeof(Image));
        box.transform.SetParent(panel.transform, false);
        box.GetComponent<Image>().color = ColorWood;
        var boxRt = box.GetComponent<RectTransform>();
        boxRt.anchorMin = boxRt.anchorMax = new Vector2(0.5f, 0.5f);
        boxRt.sizeDelta = new Vector2(620f, 530f);

        // 3. Mặt nền Face sáng
        var face = new GameObject("Face", typeof(RectTransform), typeof(Image));
        face.transform.SetParent(box.transform, false);
        face.GetComponent<Image>().color = ColorFace;
        var faceRt = face.GetComponent<RectTransform>();
        faceRt.anchorMin = Vector2.zero;
        faceRt.anchorMax = Vector2.one;
        faceRt.offsetMin = new Vector2(10, 10);
        faceRt.offsetMax = new Vector2(-10, -10);

        // Tiêu đề & Lời chào chữ đậm rõ ràng
        var title = CreateTmp(face.transform, "Title", "Cửa hàng",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -26), new Vector2(560, 38), 32, true);
        title.color = new Color(0.20f, 0.12f, 0.05f, 1f);

        var greet = CreateTmp(face.transform, "Greeting", "Bạn muốn mua hay bán gì hôm nay?",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -60), new Vector2(560, 26), 19, false);
        greet.color = new Color(0.35f, 0.25f, 0.15f, 1f);

        var money = CreateTmp(face.transform, "MoneyText", "Tiền túi: 0$",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -88), new Vector2(560, 28), 22, true);
        money.color = new Color(0.10f, 0.45f, 0.15f, 1f);

        // Thanh Tab Bar [ MUA HÀNG ] [ BÁN SẢN LƯỢNG ]
        var tabBar = new GameObject("TabBar", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        tabBar.transform.SetParent(face.transform, false);
        var tabRt = tabBar.GetComponent<RectTransform>();
        tabRt.anchorMin = tabRt.anchorMax = new Vector2(0.5f, 1f);
        tabRt.pivot = new Vector2(0.5f, 1f);
        tabRt.anchoredPosition = new Vector2(0, -116);
        tabRt.sizeDelta = new Vector2(540, 40);

        var tabHlg = tabBar.GetComponent<HorizontalLayoutGroup>();
        tabHlg.spacing = 16;
        tabHlg.childAlignment = TextAnchor.MiddleCenter;
        tabHlg.childControlHeight = true;
        tabHlg.childControlWidth = true;
        tabHlg.childForceExpandHeight = true;
        tabHlg.childForceExpandWidth = true;

        var buyTabBtn = CreateTabButton(tabBar.transform, "TabBuyBtn", "MUA HÀNG", ColorBtnGreen);
        var sellTabBtn = CreateTabButton(tabBar.transform, "TabSellBtn", "BÁN SẢN LƯỢNG", ColorBtnInactiveTab);

        // 4. Khu vực ScrollView cuộn danh sách (ScrollRect) - nằm gọn dưới TabBar, không chồng lấn
        var scrollView = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
        scrollView.transform.SetParent(face.transform, false);
        scrollView.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.08f);
        var scrollRt = scrollView.GetComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0.5f, 0.5f);
        scrollRt.anchorMax = new Vector2(0.5f, 0.5f);
        scrollRt.anchoredPosition = new Vector2(0, -48);
        scrollRt.sizeDelta = new Vector2(550, 274);

        var scroll = scrollView.GetComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.scrollSensitivity = 25f;

        // Viewport giới hạn hiển thị
        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewport.transform.SetParent(scrollView.transform, false);
        var viewRt = viewport.GetComponent<RectTransform>();
        viewRt.anchorMin = Vector2.zero;
        viewRt.anchorMax = Vector2.one;
        viewRt.offsetMin = new Vector2(4, 4);
        viewRt.offsetMax = new Vector2(-4, -4);
        scroll.viewport = viewRt;

        // Content chứa danh sách các món
        var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        var contentRt = content.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = new Vector2(0, 0);

        var csf = content.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var vlg = content.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 8;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlHeight = false;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.padding = new RectOffset(4, 4, 6, 6);

        scroll.content = contentRt;

        // 5. Nút Đóng
        var closeBtn = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
        closeBtn.transform.SetParent(face.transform, false);
        closeBtn.GetComponent<Image>().color = new Color(0.70f, 0.20f, 0.15f, 1f);
        var closeRt = closeBtn.GetComponent<RectTransform>();
        closeRt.anchorMin = closeRt.anchorMax = new Vector2(0.5f, 0f);
        closeRt.pivot = new Vector2(0.5f, 0f);
        closeRt.anchoredPosition = new Vector2(0, 14);
        closeRt.sizeDelta = new Vector2(180, 42);
        var closeTmp = CreateTmp(closeBtn.transform, "Text", "Đóng (Esc)",
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 20, true);
        closeTmp.rectTransform.offsetMin = Vector2.zero;
        closeTmp.rectTransform.offsetMax = Vector2.zero;

        // 6. ShopUIRoot & kết nối SerializedObject
        var uiRoot = new GameObject("ShopUIRoot");
        var shopUi = uiRoot.AddComponent<ShopUI>();
        var so = new SerializedObject(shopUi);
        so.FindProperty("panel").objectReferenceValue = panel;
        so.FindProperty("titleText").objectReferenceValue = title;
        so.FindProperty("greetingText").objectReferenceValue = greet;
        so.FindProperty("moneyText").objectReferenceValue = money;
        so.FindProperty("contentRoot").objectReferenceValue = content.transform;
        so.FindProperty("scrollRect").objectReferenceValue = scroll;
        so.FindProperty("buyTabButton").objectReferenceValue = buyTabBtn.GetComponent<Button>();
        so.FindProperty("sellTabButton").objectReferenceValue = sellTabBtn.GetComponent<Button>();
        so.FindProperty("buyTabText").objectReferenceValue = buyTabBtn.GetComponentInChildren<TMP_Text>();
        so.FindProperty("sellTabText").objectReferenceValue = sellTabBtn.GetComponentInChildren<TMP_Text>();
        so.FindProperty("closeButton").objectReferenceValue = closeBtn.GetComponent<Button>();
        so.ApplyModifiedPropertiesWithoutUndo();

        panel.SetActive(false);
        panel.transform.SetAsLastSibling();
    }

    static GameObject CreateTabButton(Transform parent, string name, string text, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = color;

        var tmp = CreateTmp(go.transform, "Text", text,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 20, true);
        tmp.rectTransform.offsetMin = Vector2.zero;
        tmp.rectTransform.offsetMax = Vector2.zero;
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
