using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Tự động khởi tạo và đồng bộ hệ thống NPC Shop, đàn vật nuôi, mẫu trưng bày và cổng chắn chuồng khi vào game.
/// - 3 chuồng người chơi phía trên: Cừu (10 con), Gà (10 con), Bò (10 con) kèm cổng chắn Trigger và phạm vi chuồng.
/// - 3 NPC bán gia súc ở phía dưới (kèm 4 con mẫu biết di chuyển nhưng không tương tác) + 1 NPC hạt giống.
/// </summary>
[DefaultExecutionOrder(-45)]
public class NpcShopBootstrap : MonoBehaviour
{
    readonly struct AnimalPenSpec
    {
        public readonly string TypeName;
        public readonly string ItemName;
        public readonly string ShopName;
        public readonly string LegacyTriggerName;
        public readonly int BaseCost;
        public readonly int StepCost;
        public readonly Vector2 SpawnMin;
        public readonly Vector2 SpawnMax;
        public readonly Vector2 BoundsMax;
        public readonly Vector3 GatePos;
        public readonly string NpcTilemapName;
        public readonly string NpcTriggerName;
        public readonly Vector3 NpcFallbackPos;
        public readonly string NpcTitle;
        public readonly string NpcGreeting;
        public readonly string NpcBadge;
        public readonly string[] ProductAliases;
        public readonly Vector3[] SamplePositions;
        public readonly Vector2 SampleMin;
        public readonly Vector2 SampleMax;

        public AnimalPenSpec(
            string typeName, string itemName, string shopName, string legacyTriggerName,
            int baseCost, int stepCost,
            Vector2 spawnMin, Vector2 spawnMax, Vector2 boundsMax, Vector3 gatePos,
            string npcTilemapName, string npcTriggerName, Vector3 npcFallbackPos,
            string npcTitle, string npcGreeting, string npcBadge,
            string[] productAliases,
            Vector3[] samplePositions, Vector2 sampleMin, Vector2 sampleMax)
        {
            TypeName = typeName;
            ItemName = itemName;
            ShopName = shopName;
            LegacyTriggerName = legacyTriggerName;
            BaseCost = baseCost;
            StepCost = stepCost;
            SpawnMin = spawnMin;
            SpawnMax = spawnMax;
            BoundsMax = boundsMax;
            GatePos = gatePos;
            NpcTilemapName = npcTilemapName;
            NpcTriggerName = npcTriggerName;
            NpcFallbackPos = npcFallbackPos;
            NpcTitle = npcTitle;
            NpcGreeting = npcGreeting;
            NpcBadge = npcBadge;
            ProductAliases = productAliases;
            SamplePositions = samplePositions;
            SampleMin = sampleMin;
            SampleMax = sampleMax;
        }
    }

    static readonly AnimalPenSpec[] AnimalPens =
    {
        new AnimalPenSpec(
            typeName: "Sheep", itemName: "Cừu", shopName: "SheepShop", legacyTriggerName: "NpcShop_TileSheep",
            baseCost: 50, stepCost: 30,
            spawnMin: new Vector2(-19.5f, 5.5f), spawnMax: new Vector2(-15.5f, 7.5f), boundsMax: new Vector2(-15.5f, 7.5f),
            gatePos: new Vector3(-17.5f, 4.5f, 0f),
            npcTilemapName: "NpcSheep", npcTriggerName: "NpcShop_Sheep", npcFallbackPos: new Vector3(1.5f, -23.5f, 0f),
            npcTitle: "Trang Trại Cừu",
            npcGreeting: "Chào bạn! Tôi bán cừu lấy lông (tối đa 10 con) và thu mua lông cừu tươi.",
            npcBadge: "[E] Cửa Hàng Cừu",
            productAliases: new[] { "Lông cừu" },
            samplePositions: new[]
            {
                new Vector3(5.5f, -23.5f, 0f), new Vector3(7.5f, -23.5f, 0f),
                new Vector3(6.5f, -22.5f, 0f), new Vector3(8.5f, -22.5f, 0f)
            },
            sampleMin: new Vector2(5.5f, -23.5f), sampleMax: new Vector2(9.5f, -22.5f)
        ),
        new AnimalPenSpec(
            typeName: "Chicken", itemName: "Gà", shopName: "ChickenShop", legacyTriggerName: "NpcShop_TileChick",
            baseCost: 20, stepCost: 20,
            spawnMin: new Vector2(-6.5f, 13.5f), spawnMax: new Vector2(-3.5f, 14.5f), boundsMax: new Vector2(-3.5f, 14.5f),
            gatePos: new Vector3(-5.5f, 12.5f, 0f),
            npcTilemapName: "NpcChick", npcTriggerName: "NpcShop_Chick", npcFallbackPos: new Vector3(6.5f, -14.5f, 0f),
            npcTitle: "Trang Trại Gà",
            npcGreeting: "Chào bạn! Tôi cung cấp gà giống (tối đa 10 con) và thu mua trứng gà tươi.",
            npcBadge: "[E] Cửa Hàng Gà",
            productAliases: new[] { "Trứng gà", "Trứng" },
            samplePositions: new[]
            {
                new Vector3(8.5f, -14.5f, 0f), new Vector3(10.5f, -14.5f, 0f),
                new Vector3(9.5f, -13.5f, 0f), new Vector3(11.5f, -13.5f, 0f)
            },
            sampleMin: new Vector2(8.5f, -14.5f), sampleMax: new Vector2(13.5f, -12.5f)
        ),
        new AnimalPenSpec(
            typeName: "Cow", itemName: "Bò", shopName: "CowShop", legacyTriggerName: "NpcShop_TileCow",
            baseCost: 100, stepCost: 50,
            spawnMin: new Vector2(4.5f, 13.5f), spawnMax: new Vector2(9.5f, 14.5f), boundsMax: new Vector2(9.5f, 15.5f),
            gatePos: new Vector3(5.5f, 12.5f, 0f),
            npcTilemapName: "NpcCow", npcTriggerName: "NpcShop_Cow", npcFallbackPos: new Vector3(-6.5f, -25.5f, 0f),
            npcTitle: "Trang Trại Bò",
            npcGreeting: "Chào bạn! Tôi cung cấp bò sữa (tối đa 10 con) và thu mua sữa bò nguyên chất.",
            npcBadge: "[E] Cửa Hàng Bò",
            productAliases: new[] { "Sữa bò" },
            samplePositions: new[]
            {
                new Vector3(-15.5f, -25.5f, 0f), new Vector3(-13.5f, -25.5f, 0f),
                new Vector3(-14.5f, -24.5f, 0f), new Vector3(-12.5f, -24.5f, 0f)
            },
            sampleMin: new Vector2(-15.5f, -25.5f), sampleMax: new Vector2(-11.5f, -24.5f)
        )
    };

    void Awake()
    {
        try
        {
            EnsureAllNpcShopsAndAnimals();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex, this);
        }
    }

    public static void EnsureAllNpcShopsAndAnimals()
    {
        var save = FindAnyObjectByType<SaveManager>();
        var cropDb = save != null ? save.CropDb : null;
        var gateRoot = FindOrCreateRoot("PenGateBlockers");

        // 1. Thiết lập đồng bộ cả 3 loại vật nuôi: chuồng nuôi, Shop/PlotUnlocker, con mẫu dưới & cổng chắn
        foreach (var pen in AnimalPens)
        {
            CleanLooseAnimals(pen.TypeName);
            DestroyIfExists(pen.LegacyTriggerName);

            var prefab = Resources.Load<GameObject>(pen.TypeName);
            var product = FindCrop(cropDb, pen.ProductAliases);

            var animals = EnsureAnimalsInPen(pen, prefab, 10, product);
            var unlocker = EnsureUnlocker(pen.ShopName, animals, pen.BaseCost, pen.StepCost, pen.ItemName);

            var sellCrops = product != null ? new[] { product } : new CropData[0];
            AttachOrCreateTilemapNpc(
                pen.NpcTilemapName, pen.NpcTriggerName, pen.NpcFallbackPos,
                pen.NpcTitle, pen.NpcGreeting, pen.NpcBadge,
                new[] { AnimalOffer(unlocker) }, sellCrops
            );

            EnsureSampleAnimalsInPen(pen, prefab);
            EnsureSingleGateBlocker(gateRoot.transform, $"GateBlocker_{pen.TypeName}", pen.GatePos);
        }

        // 2. Thiết lập NPC Hạt Giống & Nông Sản
        SetupSeedNpc(cropDb);

        // 3. Đảm bảo ShopUI luôn sẵn sàng
        EnsureShopUi();
    }

    static void SetupSeedNpc(CropDatabase cropDb)
    {
        var buyOffers = new List<ShopOffer>();

        // 1. Công cụ làm nông (Cuốc, Bình tưới) - Bán tại NPC hạt giống, chỉ mua 1 lần!
        var hoe = FindTool(ToolType.Hoe);
        var water = FindTool(ToolType.WateringCan);
        if (hoe != null) buyOffers.Add(ToolOffer(hoe));
        if (water != null) buyOffers.Add(ToolOffer(water));

        // 2. Hạt giống các loại cây trồng
        CropData[] crops =
        {
            FindCrop(cropDb, "Lúa"),
            FindCrop(cropDb, "Đậu", "Carrot"),
            FindCrop(cropDb, "Ngô"),
            FindCrop(cropDb, "Cà chua")
        };

        var sellCrops = new List<CropData>();
        foreach (var crop in crops)
        {
            if (crop == null) continue;
            buyOffers.Add(SeedOffer(crop));
            sellCrops.Add(crop);
        }

        CreateNpcShopObject(
            "NpcShop_Seed",
            new Vector3(-12.5f, -18.5f, 0f),
            "Cửa Hàng Hạt Giống & Công Cụ",
            "Hạt giống tươi tốt, công cụ làm nông và thu mua nông sản giá cao nhất vùng!",
            "[E] Cửa Hàng Nông Sản",
            buyOffers.ToArray(),
            sellCrops.ToArray(),
            null
        );
    }

    static ToolData FindTool(ToolType type)
    {
        if (GameManager.Instance != null)
        {
            var t = GameManager.Instance.GetToolByType(type);
            if (t != null) return t;
        }

        var tb = FindAnyObjectByType<ToolsToolbar>(FindObjectsInactive.Include);
        if (tb != null && tb.Tools != null)
        {
            foreach (var t in tb.Tools)
            {
                if (t != null && t.type == type) return t;
            }
        }

#if UNITY_EDITOR
        string fileName = type == ToolType.Hoe ? "Hoe" : type == ToolType.WateringCan ? "WateringCan" : "";
        if (!string.IsNullOrEmpty(fileName))
        {
            return UnityEditor.AssetDatabase.LoadAssetAtPath<ToolData>($"Assets/Data/Tools/{fileName}.asset");
        }
#endif
        return null;
    }

    static ShopOffer ToolOffer(ToolData tool) => new()
    {
        kind = ShopOfferKind.Tool,
        toolData = tool,
        label = tool != null ? tool.displayName : "Công cụ"
    };

    static GameObject[] EnsureAnimalsInPen(AnimalPenSpec pen, GameObject prefab, int count, CropData productItem)
    {
        var root = FindOrCreateRoot(pen.TypeName + "sRoot");
        var list = new List<GameObject>(count);

        for (int i = 0; i < count; i++)
        {
            var animalGo = GetOrCreateAnimalChild(root.transform, $"{pen.TypeName}_{i}", prefab);

            int col = i % 4;
            int row = i / 4;
            float px = Mathf.Lerp(pen.SpawnMin.x, pen.SpawnMax.x, col / 3f);
            float py = Mathf.Lerp(pen.SpawnMin.y, pen.SpawnMax.y, row / 2.5f);
            animalGo.transform.position = new Vector3(px, py, 0f);

            // Tắt trước khi gắn AnimalProduce để chờ mua trong Shop (tránh khởi tạo TMP khi đang tắt)
            animalGo.SetActive(false);
            ApplyPenBounds(animalGo, pen.SpawnMin, pen.BoundsMax);
            SetupBoughtAnimalProduce(animalGo, productItem, 30f);
            list.Add(animalGo);
        }

        return list.ToArray();
    }

    static void EnsureSampleAnimalsInPen(AnimalPenSpec pen, GameObject prefab)
    {
        var root = FindOrCreateRoot(pen.TypeName + "SampleRoot");

        for (int i = 0; i < pen.SamplePositions.Length; i++)
        {
            var animalGo = GetOrCreateAnimalChild(root.transform, $"Sample{pen.TypeName}_{i}", prefab);
            animalGo.transform.position = pen.SamplePositions[i];
            animalGo.SetActive(true);
            ApplyPenBounds(animalGo, pen.SampleMin, pen.SampleMax);
            MakeSampleAnimalNonInteractable(animalGo);
        }
    }

    static GameObject FindOrCreateRoot(string rootName)
    {
        var root = GameObject.Find(rootName);
        return root != null ? root : new GameObject(rootName);
    }

    static GameObject GetOrCreateAnimalChild(Transform root, string childName, GameObject prefab)
    {
        var existing = root.Find(childName);
        if (existing != null) return existing.gameObject;

        GameObject go = prefab != null ? Instantiate(prefab, root) : new GameObject(childName);
        if (prefab == null) go.transform.SetParent(root, false);
        go.name = childName;
        return go;
    }

    static PlotUnlocker EnsureUnlocker(string name, GameObject[] locked, int baseCost, int stepCost, string itemName)
    {
        var go = FindOrCreateRoot(name);
        var unlocker = go.GetComponent<PlotUnlocker>();
        if (unlocker == null) unlocker = go.AddComponent<PlotUnlocker>();
        unlocker.Configure(locked, baseCost, stepCost, itemName);
        return unlocker;
    }

    static void EnsureSingleGateBlocker(Transform root, string gateName, Vector3 worldPos)
    {
        var existing = root.Find(gateName);
        GameObject gateGo = existing != null ? existing.gameObject : new GameObject(gateName);
        if (existing == null)
            gateGo.transform.SetParent(root, false);

        gateGo.transform.position = worldPos;

        var box = gateGo.GetComponent<BoxCollider2D>();
        if (box == null) box = gateGo.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(0.9f, 0.9f);

        if (gateGo.GetComponent<AnimalGateBlocker>() == null)
            gateGo.AddComponent<AnimalGateBlocker>();
    }

    static void ApplyPenBounds(GameObject go, Vector2 minPos, Vector2 maxPos)
    {
        if (go == null) return;
        var wander = go.GetComponent<AnimalWander>();
        if (wander != null)
            wander.SetPenBounds(minPos, maxPos);
    }

    static void SetTriggerCollidersEnabled(GameObject go)
    {
        foreach (var col in go.GetComponentsInChildren<Collider2D>(true))
        {
            col.isTrigger = true;
            col.enabled = true;
        }
    }

    static void DestroySafely(UnityEngine.Object obj)
    {
        if (obj == null) return;
        if (Application.isPlaying) Destroy(obj);
        else DestroyImmediate(obj);
    }

    static void DestroyIfExists(string name)
    {
        var go = GameObject.Find(name);
        if (go != null) Destroy(go);
    }

    static void StripLegacyChickenBehavior(GameObject go)
    {
        DestroySafely(go.GetComponent<Chicken>());
        var eggIcon = go.transform.Find("EggIcon");
        if (eggIcon != null) DestroySafely(eggIcon.gameObject);
    }

    /// <summary>
    /// Cấu hình vật nuôi của người chơi để sinh sản lượng theo chu kỳ 30 giây (không dùng cơ chế cho ăn cũ).
    /// </summary>
    public static void SetupBoughtAnimalProduce(GameObject go, CropData productItem, float produceInterval = 30f)
    {
        if (go == null) return;

        StripLegacyChickenBehavior(go);
        SetTriggerCollidersEnabled(go);

        var produce = go.GetComponent<AnimalProduce>();
        if (produce == null) produce = go.AddComponent<AnimalProduce>();
        CropData resolvedItem = productItem != null ? productItem : produce.ProductItem;
        produce.Configure(resolvedItem, produceInterval);
    }

    /// <summary>
    /// Biến con vật thành vật mẫu trưng bày: vẫn di chuyển trong chuồng NPC nhưng không thể tương tác.
    /// </summary>
    public static void MakeSampleAnimalNonInteractable(GameObject go)
    {
        if (go == null) return;

        StripLegacyChickenBehavior(go);
        DestroySafely(go.GetComponent<AnimalProduce>());
        SetTriggerCollidersEnabled(go);

        var wander = go.GetComponent<AnimalWander>();
        if (wander != null)
            wander.SetMaxStepFromHome(2);
    }

    static void AttachOrCreateTilemapNpc(string tilemapName, string triggerName, Vector3 fallbackPos,
        string title, string greeting, string badgeText, ShopOffer[] buyOffers, CropData[] sellCrops)
    {
        Vector3 worldPos = fallbackPos;
        var tmGo = GameObject.Find(tilemapName);
        if (tmGo != null && tmGo.TryGetComponent(out Tilemap tm))
        {
            foreach (var cell in tm.cellBounds.allPositionsWithin)
            {
                if (!tm.HasTile(cell)) continue;
                worldPos = tm.GetCellCenterWorld(cell);
                break;
            }
        }
        CreateNpcShopObject(triggerName, worldPos, title, greeting, badgeText, buyOffers, sellCrops, null);
    }

    static void CreateNpcShopObject(string name, Vector3 pos, string title, string greeting,
        string badgeText, ShopOffer[] buyOffers, CropData[] sellCrops, Sprite sprite)
    {
        var go = FindOrCreateRoot(name);
        go.transform.position = pos;

        // 1. Sprite nhân vật: xóa sprite dư nếu dùng NPC vẽ sẵn trên Tilemap (sprite == null)
        var sr = go.GetComponent<SpriteRenderer>();
        if (sprite != null)
        {
            if (sr == null) sr = go.AddComponent<SpriteRenderer>();
            if (sr.sprite == null)
            {
                sr.sprite = sprite;
                sr.sortingLayerName = "Player";
                sr.sortingOrder = 0;
            }
        }
        else if (sr != null)
        {
            DestroySafely(sr);
        }

        // 2. Vùng Trigger tương tác quanh NPC
        var box = go.GetComponent<BoxCollider2D>();
        if (box == null) box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(2.8f, 2.8f);

        // 3. Cấu hình dữ liệu cửa hàng trực tiếp (không cần Reflection)
        var shop = go.GetComponent<NpcShop>();
        if (shop == null) shop = go.AddComponent<NpcShop>();
        shop.Configure(title, greeting, buyOffers, sellCrops);

        // 4. Biển tên nổi [E] phía trên đầu
        if (go.transform.Find("Badge") == null)
        {
            var badgeGo = new GameObject("Badge");
            badgeGo.transform.SetParent(go.transform, false);
            badgeGo.transform.localPosition = new Vector3(0f, 0.9f, 0f);

            var tmp = badgeGo.AddComponent<TextMeshPro>();
            tmp.text = $"<color=#FFD700><b>{badgeText}</b></color>";
            tmp.fontSize = 2.4f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.sortingLayerID = SortingLayer.NameToID("Player");
            tmp.sortingOrder = 10;
        }
    }

    static void CleanLooseAnimals(string typeName)
    {
        var allGos = FindObjectsByType<GameObject>(FindObjectsInactive.Include);
        foreach (var go in allGos)
        {
            if (go == null) continue;
            if (go.name.Contains("Shop") || go.name.Contains("Trigger") || go.name.Contains("Root") ||
                go.name.Contains("Npc") || go.name.Contains("Sample") || go.name.Contains("GateBlocker"))
                continue;

            if (go.name.StartsWith(typeName, StringComparison.OrdinalIgnoreCase) &&
                (go.name.Contains("(") || go.name.Equals(typeName, StringComparison.OrdinalIgnoreCase)))
            {
                Destroy(go);
            }
        }
    }

    static CropData FindCrop(CropDatabase db, params string[] names)
    {
        if (db == null || names == null) return null;
        foreach (var n in names)
        {
            var found = db.Find(n);
            if (found != null) return found;
        }
        return null;
    }

    static ShopOffer AnimalOffer(PlotUnlocker shop) => new()
    {
        kind = ShopOfferKind.Animal,
        animalShop = shop,
        label = "Mua"
    };

    static ShopOffer SeedOffer(CropData crop) => new()
    {
        kind = ShopOfferKind.Seed,
        seedCrop = crop,
        label = crop != null ? crop.cropName : "Hạt"
    };

    static void EnsureShopUi()
    {
        if (ShopUI.Instance != null) return;
        if (FindAnyObjectByType<ShopUI>(FindObjectsInactive.Include) != null) return;
        if (FindAnyObjectByType<Canvas>() == null) return;

        var uiRoot = new GameObject("ShopUIRoot");
        uiRoot.AddComponent<ShopUI>();
    }
}
