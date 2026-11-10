#if UNITY_EDITOR
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using UnityEngine.InputSystem.UI;

/// <summary>
/// Dựng Phần 1 (asset cây) + Phần 3 (scene Main) theo FarmSim2D_HuongDan.docx
/// Menu: FarmSim → Setup theo hướng dẫn (Phần 1-3)
/// </summary>
public static class FarmSimGuideSetup
{
    const string MainScenePath = "Assets/Scenes/Main.unity";
    const string DataCrops = "Assets/Data/Crops";
    const string DataPath = "Assets/Data";
    const string AutoFlag = "Assets/Editor/RUN_GUIDE_SETUP.flag";

    [InitializeOnLoadMethod]
    static void AutoRunIfFlagged()
    {
        if (!File.Exists(AutoFlag)) return;
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(AutoFlag)) return;
            try { File.Delete(AutoFlag); } catch { /* ignore */ }
            Debug.Log("[FarmSim] Phát hiện cờ auto-setup → chạy Phần 1-3...");
            try
            {
                ExecuteFromGuide();
                EditorUtility.DisplayDialog("FarmSim", "Đã dựng xong Phần 1–3 theo hướng dẫn.\nMở Scenes/Main rồi bấm Play.", "OK");
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[FarmSim] Setup lỗi: " + ex);
                EditorUtility.DisplayDialog("FarmSim lỗi", ex.Message, "OK");
            }
        };
    }

    [MenuItem("FarmSim/Setup theo hướng dẫn (Phần 1-3)", false, 0)]
    public static void ExecuteFromMenu()
    {
        if (!EditorUtility.DisplayDialog(
                "FarmSim Guide Setup",
                "Sẽ tạo Crop assets, Prefab Plot/Chicken và dựng đầy đủ scene Main theo hướng dẫn (Phần 1 bước 5 + Phần 3).\n\nScene Main hiện tại sẽ được cập nhật. Tiếp tục?",
                "Chạy", "Huỷ"))
            return;

        ExecuteFromGuide();
        EditorUtility.DisplayDialog("Xong", "Đã dựng xong Phần 1-3.\nMở Scenes/Main và bấm Play để thử.", "OK");
    }

    // Unity batchmode: -executeMethod FarmSimGuideSetup.ExecuteFromGuide
    public static void ExecuteFromGuide()
    {
        EnsureFolders();
        EnsureSortingLayers();
        var crops = CreateCropAssets();
        var cropDb = CreateCropDatabase(crops);

        var scene = EditorSceneManager.OpenScene(MainScenePath);
        CleanupOldGuideObjects();
        PrepareTilemaps(out var ground, out var decor, out var obstacles);
        FixGlobalLight();

        var gameManager = CreateManagers(crops.carrot);
        var player = EnsurePlayer();
        SetupCamera(player);

        var plotPrefab = CreatePlotPrefab(LoadSprite("tile_0042"));
        var plots = PlacePlots(plotPrefab);
        var upgrades = CreateUnlocker("Upgrades", plots.Skip(6).Take(3).ToArray(), 100, "ô đất");

        var chickenPrefab = CreateChickenPrefab(crops.egg, LoadSprite("tile_0122"), LoadSprite("tile_0125"));
        var chickens = PlaceChickens(chickenPrefab);
        var chickenShop = CreateUnlocker("ChickenShop", chickens, 150, "gà");

        BuildCanvasAndWire(
            crops, cropDb, gameManager, upgrades, chickenShop,
            ground, decor, obstacles);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, MainScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[FarmSim] Setup Phần 1-3 hoàn tất → " + MainScenePath);
    }

    static void EnsureFolders()
    {
        EnsureFolder("Assets", "Art");
        EnsureFolder("Assets", "Audio");
        EnsureFolder("Assets", "Fonts");
        EnsureFolder("Assets", "Scripts");
        EnsureFolder("Assets", "Prefabs");
        EnsureFolder("Assets", "Data");
        EnsureFolder("Assets", "Scenes");
        EnsureFolder("Assets/Data", "Crops");
        EnsureFolder("Assets/Data", "Tiles");
    }

    static void EnsureFolder(string parent, string name)
    {
        string path = parent + "/" + name;
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, name);
    }

    static void EnsureSortingLayers()
    {
        string[] needed = { "Background", "Plots", "Crops", "Player" };
        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tagManager.FindProperty("m_SortingLayers");
        foreach (var name in needed)
        {
            bool found = false;
            for (int i = 0; i < layers.arraySize; i++)
            {
                if (layers.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == name)
                {
                    found = true;
                    break;
                }
            }
            if (found) continue;
            layers.InsertArrayElementAtIndex(layers.arraySize);
            var el = layers.GetArrayElementAtIndex(layers.arraySize - 1);
            el.FindPropertyRelative("name").stringValue = name;
            el.FindPropertyRelative("uniqueID").intValue = unchecked((int)(uint)(1000 + layers.arraySize));
        }
        tagManager.ApplyModifiedProperties();
    }

    struct CropSet
    {
        public CropData carrot, tomato, pumpkin, egg;
    }

    static CropSet CreateCropAssets()
    {
        var set = new CropSet
        {
            carrot = CreateOrUpdateCrop("Carrot", "Cà rốt", 10, 25, 10f, Season.Spring, true,
                new[] { "tile_0093", "tile_0095", "tile_0066" }, "tile_0066"),
            tomato = CreateOrUpdateCrop("Tomato", "Cà chua", 25, 70, 25f, Season.Summer, true,
                new[] { "tile_0129", "tile_0131", "tile_0103" }, "tile_0103"),
            pumpkin = CreateOrUpdateCrop("Pumpkin", "Bí ngô", 60, 190, 60f, Season.Autumn, true,
                new[] { "tile_0096", "tile_0097", "tile_0091" }, "tile_0097"),
            egg = CreateOrUpdateCrop("Egg", "Trứng", 0, 20, 0f, Season.Winter, true,
                null, "tile_0125")
        };
        return set;
    }

    static CropData CreateOrUpdateCrop(
        string fileName, string cropName, int seed, int sell, float grow,
        Season season, bool hasBest, string[] growthTileNames, string iconTile)
    {
        string path = $"{DataCrops}/{fileName}.asset";
        var crop = AssetDatabase.LoadAssetAtPath<CropData>(path);
        if (crop == null)
        {
            crop = ScriptableObject.CreateInstance<CropData>();
            AssetDatabase.CreateAsset(crop, path);
        }

        crop.cropName = cropName;
        crop.seedPrice = seed;
        crop.sellPrice = sell;
        crop.growTime = grow;
        crop.hasBestSeason = hasBest;
        crop.bestSeason = season;
        crop.icon = LoadSprite(iconTile);

        if (growthTileNames == null)
        {
            crop.growthSprites = new Sprite[0];
        }
        else
        {
            crop.growthSprites = growthTileNames.Select(LoadSprite).Where(s => s != null).ToArray();
        }

        EditorUtility.SetDirty(crop);
        return crop;
    }

    static CropDatabase CreateCropDatabase(CropSet set)
    {
        string path = $"{DataPath}/CropDB.asset";
        var db = AssetDatabase.LoadAssetAtPath<CropDatabase>(path);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<CropDatabase>();
            AssetDatabase.CreateAsset(db, path);
        }
        db.crops = new[] { set.carrot, set.tomato, set.pumpkin, set.egg };
        EditorUtility.SetDirty(db);
        return db;
    }

    static Sprite LoadSprite(string tileFile)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/TinyFarm/Tiles/{tileFile}.png");
    }

    static void CleanupOldGuideObjects()
    {
        string[] names =
        {
            "GameManager", "AudioManager", "Music", "Upgrades", "ChickenShop",
            "HUDRoot", "ToastRoot", "DayCycle", "WorldEnvironment",
            "OrderManagerObj", "OrderUIRoot", "SaveManager", "PauseRoot",
            "Canvas", "EventSystem", "PlotsRoot", "ChickensRoot"
        };
        foreach (var n in names)
        {
            var go = GameObject.Find(n);
            if (go != null) Object.DestroyImmediate(go);
        }

        // Xoá các Plot / Chicken rời (không phải prefab asset)
        foreach (var p in Object.FindObjectsByType<Plot>(FindObjectsInactive.Include))
            if (p != null) Object.DestroyImmediate(p.gameObject);
        foreach (var c in Object.FindObjectsByType<Chicken>(FindObjectsInactive.Include))
            if (c != null) Object.DestroyImmediate(c.gameObject);

        var oldPlayer = GameObject.Find("Player");
        if (oldPlayer != null) Object.DestroyImmediate(oldPlayer);
    }

    static void PrepareTilemaps(out Tilemap ground, out Tilemap decor, out Tilemap obstacles)
    {
        var grid = Object.FindAnyObjectByType<Grid>();
        if (grid == null)
        {
            var gridGo = new GameObject("Grid", typeof(Grid));
            grid = gridGo.GetComponent<Grid>();
        }

        ground = FindOrRenameTilemap(grid.transform, new[] { "Ground", "Map1" }, "Ground", 0);
        decor = FindOrRenameTilemap(grid.transform, new[] { "Decor" }, "Decor", 1);
        obstacles = FindOrRenameTilemap(grid.transform, new[] { "Obstacles", "Fences" }, "Obstacles", 2);

        if (decor.GetComponent<TilemapCollider2D>() != null)
            Object.DestroyImmediate(decor.GetComponent<TilemapCollider2D>());

        if (obstacles.GetComponent<TilemapCollider2D>() == null)
            obstacles.gameObject.AddComponent<TilemapCollider2D>();
    }

    static Tilemap FindOrRenameTilemap(Transform grid, string[] aliases, string finalName, int order)
    {
        Tilemap found = null;
        foreach (var a in aliases)
        {
            var t = grid.Find(a);
            if (t != null) { found = t.GetComponent<Tilemap>(); break; }
        }

        if (found == null)
        {
            var go = new GameObject(finalName);
            go.transform.SetParent(grid, false);
            found = go.AddComponent<Tilemap>();
            go.AddComponent<TilemapRenderer>();
        }

        found.gameObject.name = finalName;
        var rend = found.GetComponent<TilemapRenderer>();
        rend.sortingLayerName = "Background";
        rend.sortingOrder = order;
        return found;
    }

    static void FixGlobalLight()
    {
        var light = Object.FindObjectsByType<MonoBehaviour>()
            .FirstOrDefault(m => m.GetType().Name == "Light2D");
        if (light == null) return;

        var so = new SerializedObject(light);
        var prop = so.FindProperty("m_ApplyToSortingLayers");
        if (prop == null || !prop.isArray) return;

        int[] ids = SortingLayer.layers.Select(l => l.id).ToArray();
        prop.arraySize = ids.Length;
        for (int i = 0; i < ids.Length; i++)
            prop.GetArrayElementAtIndex(i).intValue = ids[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static GameManager CreateManagers(CropData defaultCrop)
    {
        var gmGo = new GameObject("GameManager");
        var gm = gmGo.AddComponent<GameManager>();
        var gmSo = new SerializedObject(gm);
        gmSo.FindProperty("startMoney").intValue = 50;
        gmSo.FindProperty("defaultCrop").objectReferenceValue = defaultCrop;
        gmSo.ApplyModifiedPropertiesWithoutUndo();

        var audioGo = new GameObject("AudioManager");
        var audio = audioGo.AddComponent<AudioManager>();
        var src = audioGo.AddComponent<AudioSource>();
        src.playOnAwake = false;
        var audioSo = new SerializedObject(audio);
        audioSo.FindProperty("sfxSource").objectReferenceValue = src;
        var sounds = audioSo.FindProperty("sounds");
        string[] names = { "plant", "harvest", "sell", "click" };
        sounds.arraySize = names.Length;
        for (int i = 0; i < names.Length; i++)
            sounds.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue = names[i];
        audioSo.ApplyModifiedPropertiesWithoutUndo();

        return gm;
    }

    static GameObject EnsurePlayer()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
        GameObject player;
        if (prefab != null)
            player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        else
            player = CreatePlayerFallback();

        player.name = "Player";
        player.tag = "Player";
        player.transform.position = new Vector3(0.5f, 0.5f, 0f);
        return player;
    }

    static GameObject CreatePlayerFallback()
    {
        var player = new GameObject("Player");
        player.tag = "Player";
        var rb = player.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        var col = player.AddComponent<CapsuleCollider2D>();
        col.size = new Vector2(0.5f, 0.5f);
        col.offset = new Vector2(0, -0.2f);

        var spriteObj = new GameObject("Sprite");
        spriteObj.transform.SetParent(player.transform, false);
        var sr = spriteObj.AddComponent<SpriteRenderer>();
        var idle = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Player/Player_Idle.png").OfType<Sprite>().FirstOrDefault();
        if (idle != null) sr.sprite = idle;
        sr.sortingLayerName = "Player";

        player.AddComponent<PlayerController>();
        var vis = player.AddComponent<PlayerVisual>();
        var so = new SerializedObject(vis);
        so.FindProperty("body").objectReferenceValue = rb;
        so.FindProperty("sprite").objectReferenceValue = sr;
        so.ApplyModifiedPropertiesWithoutUndo();
        return player;
    }

    static void SetupCamera(GameObject player)
    {
        var cam = Camera.main;
        if (cam == null) return;
        cam.orthographic = true;
        cam.orthographicSize = 5.6f;
        cam.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, -10f);

        var follow = cam.GetComponent<CameraFollow>();
        if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
        var so = new SerializedObject(follow);
        so.FindProperty("target").objectReferenceValue = player.transform;
        so.FindProperty("smooth").floatValue = 8f;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static GameObject CreatePlotPrefab(Sprite soil)
    {
        var root = new GameObject("Plot");
        var sr = root.AddComponent<SpriteRenderer>();
        sr.sprite = soil;
        sr.sortingLayerName = "Plots";

        var box = root.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = Vector2.one;

        var crop = new GameObject("Crop");
        crop.transform.SetParent(root.transform, false);
        var cropSr = crop.AddComponent<SpriteRenderer>();
        cropSr.sortingLayerName = "Crops";

        var plot = root.AddComponent<Plot>();
        var so = new SerializedObject(plot);
        so.FindProperty("cropRenderer").objectReferenceValue = cropSr;
        so.ApplyModifiedPropertiesWithoutUndo();

        string path = "Assets/Prefabs/Plot.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    static GameObject[] PlacePlots(GameObject plotPrefab)
    {
        var root = new GameObject("PlotsRoot");
        // Lưới 3x3 trong vùng cỏ trống của map hiện tại (map nhỏ hơn ví dụ 40x30 trong guide)
        Vector2 origin = new Vector2(-2.5f, -1.5f);
        float step = 2f;
        var plots = new GameObject[9];
        int i = 0;
        for (int y = 0; y < 3; y++)
        for (int x = 0; x < 3; x++)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(plotPrefab);
            go.name = $"Plot_{i}";
            go.transform.SetParent(root.transform, true);
            go.transform.position = new Vector3(origin.x + x * step, origin.y + y * step, 0f);
            if (i >= 6) go.SetActive(false); // 3 ô cuối khoá
            plots[i++] = go;
        }
        return plots;
    }

    static GameObject CreateChickenPrefab(CropData egg, Sprite chickenSprite, Sprite eggSprite)
    {
        var root = new GameObject("Chicken");
        var sr = root.AddComponent<SpriteRenderer>();
        sr.sprite = chickenSprite;
        sr.sortingLayerName = "Crops";

        var box = root.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(0.8f, 0.8f);

        var eggIcon = new GameObject("EggIcon");
        eggIcon.transform.SetParent(root.transform, false);
        eggIcon.transform.localPosition = new Vector3(0f, 0.6f, 0f);
        var eggSr = eggIcon.AddComponent<SpriteRenderer>();
        eggSr.sprite = eggSprite;
        eggSr.sortingLayerName = "Crops";
        eggSr.sortingOrder = 5;
        eggIcon.SetActive(false);

        var chicken = root.AddComponent<Chicken>();
        var so = new SerializedObject(chicken);
        so.FindProperty("eggItem").objectReferenceValue = egg;
        so.FindProperty("body").objectReferenceValue = sr;
        so.FindProperty("eggIcon").objectReferenceValue = eggIcon;
        so.ApplyModifiedPropertiesWithoutUndo();

        string path = "Assets/Prefabs/Chicken.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    static GameObject[] PlaceChickens(GameObject prefab)
    {
        var root = new GameObject("ChickensRoot");
        Vector3[] positions =
        {
            new Vector3(5.5f, -2.5f, 0f),
            new Vector3(6.5f, -2.5f, 0f),
            new Vector3(7.5f, -2.5f, 0f)
        };
        var chickens = new GameObject[3];
        for (int i = 0; i < 3; i++)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = $"Chicken_{i}";
            go.transform.SetParent(root.transform, true);
            go.transform.position = positions[i];
            go.SetActive(false);
            chickens[i] = go;
        }
        return chickens;
    }

    static PlotUnlocker CreateUnlocker(string name, GameObject[] locked, int baseCost, string itemName)
    {
        var go = new GameObject(name);
        var unlocker = go.AddComponent<PlotUnlocker>();
        var so = new SerializedObject(unlocker);
        var arr = so.FindProperty("lockedPlots");
        arr.arraySize = locked.Length;
        for (int i = 0; i < locked.Length; i++)
            arr.GetArrayElementAtIndex(i).objectReferenceValue = locked[i];
        so.FindProperty("baseCost").intValue = baseCost;
        so.FindProperty("itemName").stringValue = itemName;
        so.ApplyModifiedPropertiesWithoutUndo();
        return unlocker;
    }

    static void BuildCanvasAndWire(
        CropSet crops, CropDatabase cropDb, GameManager gameManager,
        PlotUnlocker upgrades, PlotUnlocker chickenShop,
        Tilemap ground, Tilemap decor, Tilemap obstacles)
    {
        EnsureEventSystem();

        var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // HUD texts / images
        var moneyText = CreateTmp(canvasGo.transform, "MoneyText", "Tiền: 50$", new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(40, -40), new Vector2(420, 60), TextAlignmentOptions.TopLeft);
        var dayText = CreateTmp(canvasGo.transform, "DayText", "Ngày 1", new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -40), new Vector2(360, 60), TextAlignmentOptions.Top);
        var seasonText = CreateTmp(canvasGo.transform, "SeasonText", "Xuân - Nắng\nTốc độ lớn: +20%", new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -100), new Vector2(520, 90), TextAlignmentOptions.Top);
        var inventoryText = CreateTmp(canvasGo.transform, "InventoryText", "Kho trống", new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-40, -40), new Vector2(420, 220), TextAlignmentOptions.TopRight);

        var selectedIconGo = new GameObject("SelectedIcon", typeof(RectTransform), typeof(Image));
        selectedIconGo.transform.SetParent(canvasGo.transform, false);
        var selectedIconRt = selectedIconGo.GetComponent<RectTransform>();
        SetAnchor(selectedIconRt, new Vector2(0, 0), new Vector2(0, 0), new Vector2(40, 40), new Vector2(96, 96));
        var selectedIcon = selectedIconGo.GetComponent<Image>();
        selectedIcon.preserveAspect = true;

        var selectedLabel = CreateTmp(canvasGo.transform, "SelectedLabel", "Cà rốt (10$)", new Vector2(0, 0), new Vector2(0, 0),
            new Vector2(150, 55), new Vector2(360, 60), TextAlignmentOptions.Left);

        var seedCarrot = CreateButton(canvasGo.transform, "SeedButton_Carrot", "Cà rốt", new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(-220, 50), new Vector2(180, 64));
        var seedTomato = CreateButton(canvasGo.transform, "SeedButton_Tomato", "Cà chua", new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(0, 50), new Vector2(180, 64));
        var seedPumpkin = CreateButton(canvasGo.transform, "SeedButton_Pumpkin", "Bí ngô", new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(220, 50), new Vector2(180, 64));
        AttachCropButton(seedCarrot, crops.carrot);
        AttachCropButton(seedTomato, crops.tomato);
        AttachCropButton(seedPumpkin, crops.pumpkin);

        var sellButton = CreateButton(canvasGo.transform, "SellButton", "Bán hết", new Vector2(1, 0), new Vector2(1, 0),
            new Vector2(-120, 50), new Vector2(200, 64));
        UnityEventTools.AddPersistentListener(sellButton.GetComponent<Button>().onClick, gameManager.SellAll);

        var buyPlotButton = CreateButton(canvasGo.transform, "BuyPlotButton", "Mua ô đất (100$)", new Vector2(1, 0.5f), new Vector2(1, 0.5f),
            new Vector2(-160, 80), new Vector2(280, 64));
        var buyChickenButton = CreateButton(canvasGo.transform, "BuyChickenButton", "Mua gà (150$)", new Vector2(1, 0.5f), new Vector2(1, 0.5f),
            new Vector2(-160, 0), new Vector2(280, 64));
        var ordersButton = CreateButton(canvasGo.transform, "OrdersButton", "Đơn hàng (O)", new Vector2(1, 0.5f), new Vector2(1, 0.5f),
            new Vector2(-160, -80), new Vector2(280, 64));
        var pauseButton = CreateButton(canvasGo.transform, "PauseButton", "II", new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-70, -40), new Vector2(70, 64));
        var resetButton = CreateButton(canvasGo.transform, "ResetButton", "Chơi lại", new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-220, -40), new Vector2(160, 64));

        // Orders panel
        var ordersPanel = CreatePanel(canvasGo.transform, "OrdersPanel", new Color(0.12f, 0.14f, 0.18f, 0.92f));
        var ordersRt = ordersPanel.GetComponent<RectTransform>();
        SetAnchor(ordersRt, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-520, 0), new Vector2(480, 360));
        var vlg = ordersPanel.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(16, 16, 16, 16);
        vlg.spacing = 12;
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;
        ordersPanel.SetActive(false);

        var orderRows = new OrderUI.Row[3];
        for (int i = 0; i < 3; i++)
        {
            var row = new GameObject($"Row{i + 1}", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(ordersPanel.transform, false);
            var h = row.GetComponent<HorizontalLayoutGroup>();
            h.spacing = 8;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = true;

            var le = row.AddComponent<LayoutElement>();
            le.minHeight = 70;
            le.preferredHeight = 70;

            var rowText = CreateTmp(row.transform, "Text", $"Đơn {i + 1}", new Vector2(0, 0.5f), new Vector2(1, 0.5f),
                Vector2.zero, new Vector2(0, 60), TextAlignmentOptions.Left);
            rowText.fontSize = 28;
            var deliver = CreateButton(row.transform, "Deliver", "Giao", new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                Vector2.zero, new Vector2(120, 56));
            var deliverLe = deliver.AddComponent<LayoutElement>();
            deliverLe.minWidth = 120;
            deliverLe.preferredWidth = 120;

            orderRows[i] = new OrderUI.Row
            {
                root = row,
                text = rowText,
                button = deliver.GetComponent<Button>()
            };
        }

        // Overlays
        var weatherOverlay = CreateFullscreenImage(canvasGo.transform, "WeatherOverlay", new Color(0, 0, 0, 0));
        weatherOverlay.raycastTarget = false;
        var rainArea = new GameObject("RainArea", typeof(RectTransform));
        rainArea.transform.SetParent(canvasGo.transform, false);
        Stretch(rainArea.GetComponent<RectTransform>());
        rainArea.AddComponent<RainEffect>();

        var nightOverlay = CreateFullscreenImage(canvasGo.transform, "NightOverlay", new Color(0.04f, 0.07f, 0.25f, 0f));
        nightOverlay.raycastTarget = false;

        // Toast
        var toastRoot = new GameObject("ToastRoot", typeof(RectTransform));
        toastRoot.transform.SetParent(canvasGo.transform, false);
        Stretch(toastRoot.GetComponent<RectTransform>());
        var toastText = CreateTmp(toastRoot.transform, "ToastText", "", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, 120), new Vector2(900, 80), TextAlignmentOptions.Center);
        toastText.fontSize = 42;
        toastText.gameObject.SetActive(false);
        var toast = toastRoot.AddComponent<Toast>();
        var toastSo = new SerializedObject(toast);
        toastSo.FindProperty("label").objectReferenceValue = toastText;
        toastSo.ApplyModifiedPropertiesWithoutUndo();

        // Pause panel
        var pausePanel = CreatePanel(canvasGo.transform, "PausePanel", new Color(0f, 0f, 0f, 0.7f));
        Stretch(pausePanel.GetComponent<RectTransform>());
        pausePanel.GetComponent<Image>().raycastTarget = true;
        var pauseTitle = CreateTmp(pausePanel.transform, "Title", "Tạm dừng", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, 160), new Vector2(500, 80), TextAlignmentOptions.Center);
        pauseTitle.fontSize = 56;
        var resumeBtn = CreateButton(pausePanel.transform, "ResumeButton", "Tiếp tục", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, 40), new Vector2(280, 70));
        var menuBtn = CreateButton(pausePanel.transform, "MenuButton", "Về menu", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, -50), new Vector2(280, 70));
        var quitBtn = CreateButton(pausePanel.transform, "QuitButton", "Thoát", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, -140), new Vector2(280, 70));
        pausePanel.SetActive(false);

        // Manager roots (always on)
        var hudRoot = new GameObject("HUDRoot");
        var hud = hudRoot.AddComponent<HUD>();
        var hudSo = new SerializedObject(hud);
        hudSo.FindProperty("moneyText").objectReferenceValue = moneyText;
        hudSo.FindProperty("inventoryText").objectReferenceValue = inventoryText;
        hudSo.FindProperty("selectedLabel").objectReferenceValue = selectedLabel;
        hudSo.FindProperty("selectedIcon").objectReferenceValue = selectedIcon;
        hudSo.ApplyModifiedPropertiesWithoutUndo();

        var dayCycleGo = new GameObject("DayCycle");
        var dayCycle = dayCycleGo.AddComponent<DayCycle>();
        var daySo = new SerializedObject(dayCycle);
        daySo.FindProperty("dayLengthSeconds").floatValue = 120f;
        daySo.FindProperty("nightOverlay").objectReferenceValue = nightOverlay;
        daySo.FindProperty("dayText").objectReferenceValue = dayText;
        daySo.ApplyModifiedPropertiesWithoutUndo();

        var worldGo = new GameObject("WorldEnvironment");
        var world = worldGo.AddComponent<WorldEnvironment>();
        var worldSo = new SerializedObject(world);
        var tinted = worldSo.FindProperty("tintedTilemaps");
        tinted.arraySize = 3;
        tinted.GetArrayElementAtIndex(0).objectReferenceValue = ground;
        tinted.GetArrayElementAtIndex(1).objectReferenceValue = decor;
        tinted.GetArrayElementAtIndex(2).objectReferenceValue = obstacles;
        worldSo.FindProperty("weatherOverlay").objectReferenceValue = weatherOverlay;
        worldSo.FindProperty("rain").objectReferenceValue = rainArea.GetComponent<RainEffect>();
        worldSo.FindProperty("infoText").objectReferenceValue = seasonText;
        worldSo.ApplyModifiedPropertiesWithoutUndo();

        var orderMgrGo = new GameObject("OrderManagerObj");
        var orderMgr = orderMgrGo.AddComponent<OrderManager>();
        var omSo = new SerializedObject(orderMgr);
        omSo.FindProperty("cropDb").objectReferenceValue = cropDb;
        omSo.ApplyModifiedPropertiesWithoutUndo();

        var orderUiGo = new GameObject("OrderUIRoot");
        var orderUi = orderUiGo.AddComponent<OrderUI>();
        var ouiSo = new SerializedObject(orderUi);
        var rowsProp = ouiSo.FindProperty("rows");
        rowsProp.arraySize = 3;
        for (int i = 0; i < 3; i++)
        {
            var row = rowsProp.GetArrayElementAtIndex(i);
            row.FindPropertyRelative("root").objectReferenceValue = orderRows[i].root;
            row.FindPropertyRelative("text").objectReferenceValue = orderRows[i].text;
            row.FindPropertyRelative("button").objectReferenceValue = orderRows[i].button;
        }
        ouiSo.ApplyModifiedPropertiesWithoutUndo();

        var saveGo = new GameObject("SaveManager");
        var save = saveGo.AddComponent<SaveManager>();
        var saveSo = new SerializedObject(save);
        saveSo.FindProperty("cropDb").objectReferenceValue = cropDb;
        saveSo.FindProperty("unlocker").objectReferenceValue = upgrades;
        saveSo.FindProperty("chickenUnlocker").objectReferenceValue = chickenShop;
        saveSo.FindProperty("orderManager").objectReferenceValue = orderMgr;
        saveSo.FindProperty("environment").objectReferenceValue = world;
        saveSo.ApplyModifiedPropertiesWithoutUndo();

        var pauseRoot = new GameObject("PauseRoot");
        var pauseMenu = pauseRoot.AddComponent<PauseMenu>();
        var pauseSo = new SerializedObject(pauseMenu);
        pauseSo.FindProperty("panel").objectReferenceValue = pausePanel;
        pauseSo.FindProperty("saveManager").objectReferenceValue = save;
        pauseSo.ApplyModifiedPropertiesWithoutUndo();

        // Unlocker button labels
        BindUnlockerLabel(upgrades, buyPlotButton);
        BindUnlockerLabel(chickenShop, buyChickenButton);
        UnityEventTools.AddPersistentListener(buyPlotButton.GetComponent<Button>().onClick, upgrades.BuyPlot);
        UnityEventTools.AddPersistentListener(buyChickenButton.GetComponent<Button>().onClick, chickenShop.BuyPlot);

        var ordersToggle = ordersButton.AddComponent<PanelToggle>();
        var otSo = new SerializedObject(ordersToggle);
        otSo.FindProperty("panel").objectReferenceValue = ordersPanel;
        otSo.FindProperty("key").enumValueIndex = (int)KeyCode.O;
        // KeyCode enum index is not reliable via enumValueIndex — set int value
        otSo.FindProperty("key").intValue = (int)KeyCode.O;
        otSo.ApplyModifiedPropertiesWithoutUndo();
        UnityEventTools.AddPersistentListener(ordersButton.GetComponent<Button>().onClick, ordersToggle.Toggle);

        UnityEventTools.AddPersistentListener(pauseButton.GetComponent<Button>().onClick, pauseMenu.Pause);
        UnityEventTools.AddPersistentListener(resetButton.GetComponent<Button>().onClick, save.ResetSave);
        UnityEventTools.AddPersistentListener(resumeBtn.GetComponent<Button>().onClick, pauseMenu.Resume);
        UnityEventTools.AddPersistentListener(menuBtn.GetComponent<Button>().onClick, pauseMenu.BackToMenu);
        // Nut Thoat trong pause da bo — an di, khong wire Quit
        quitBtn.SetActive(false);

        // Hierarchy order: HUD → Orders → Weather → Rain → Night → Toast → Pause
        moneyText.transform.SetSiblingIndex(0);
        ordersPanel.transform.SetAsLastSibling();
        weatherOverlay.transform.SetAsLastSibling();
        rainArea.transform.SetAsLastSibling();
        nightOverlay.transform.SetAsLastSibling();
        toastRoot.transform.SetAsLastSibling();
        pausePanel.transform.SetAsLastSibling();
    }

    static void BindUnlockerLabel(PlotUnlocker unlocker, GameObject button)
    {
        var label = button.GetComponentInChildren<TMP_Text>();
        var so = new SerializedObject(unlocker);
        so.FindProperty("buttonLabel").objectReferenceValue = label;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void AttachCropButton(GameObject button, CropData crop)
    {
        var cb = button.AddComponent<CropButton>();
        var so = new SerializedObject(cb);
        so.FindProperty("crop").objectReferenceValue = crop;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        // Active Input Handling = Both: Input System UI module
        if (es.GetComponent<InputSystemUIInputModule>() == null)
            es.AddComponent<InputSystemUIInputModule>();
    }

    static TMP_Text CreateTmp(Transform parent, string name, string content,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        SetAnchor(rt, anchorMin, anchorMax, anchoredPos, size);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = 36;
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
        SetAnchor(rt, anchorMin, anchorMax, anchoredPos, size);
        var img = go.GetComponent<Image>();
        img.color = new Color(0.18f, 0.45f, 0.28f, 0.95f);

        var text = CreateTmp(go.transform, "Text", label, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
        Stretch(text.rectTransform);
        text.fontSize = 30;
        return go;
    }

    static GameObject CreatePanel(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        return go;
    }

    static Image CreateFullscreenImage(Transform parent, string name, Color color)
    {
        var go = CreatePanel(parent, name, color);
        Stretch(go.GetComponent<RectTransform>());
        return go.GetComponent<Image>();
    }

    static void SetAnchor(RectTransform rt, Vector2 min, Vector2 max, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = min;
        rt.anchorMax = max;
        // Pivot khớp anchor góc để chữ/nút không bị tràn ra ngoài màn hình
        rt.pivot = new Vector2((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f);
        if (min == Vector2.zero && max == Vector2.one && size == Vector2.zero)
        {
            Stretch(rt);
            return;
        }
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
    }
}
#endif
