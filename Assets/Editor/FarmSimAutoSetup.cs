#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class FarmSimAutoRunner
{
    static FarmSimAutoRunner()
    {
        EditorApplication.delayCall += RunAutoSetup;
    }

    static void RunAutoSetup()
    {
        // Khong chay khi dang Play (MarkSceneDirty se nem InvalidOperationException)
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        FarmSimAutoSetup.ExecuteSetup(silent: true);
        FarmSimAutoSetup.ExecuteColliderSetup(silent: true);
    }
}

public class FarmSimAutoSetup : EditorWindow
{
    [MenuItem("FarmSim/Cập nhật Động vật (Gà, Bò, Cừu) - Bước 1 Ô & Thở", false, 1)]
    public static void ShowWindow()
    {
        var window = GetWindow<FarmSimAutoSetup>("Cập nhật Động Vật");
        window.minSize = new Vector2(400, 240);
        window.Show();
    }

    void OnGUI()
    {
        GUILayout.Space(12);
        GUILayout.Label("CẬP NHẬT GÀ, BÒ, CỪU: ĐI BƯỚC 1 Ô & KHÔNG NHẢY", EditorStyles.boldLabel);
        GUILayout.Space(8);
        EditorGUILayout.HelpBox(
            "Đặc điểm chuyển động mới:\n" +
            "1. KHÔNG NHẢY: Con vật bước đi phẳng trên mặt đất, trượt êm 1 ô.\n" +
            "2. Đi 1 ô phẳng (Lên/Xuống/Trái/Phải), tự lật hướng.\n" +
            "3. Lắc nhẹ thân mình khi bước chân (waddle) và thở nhẹ khi đứng yên.\n" +
            "4. Tự động spawn Bò và Cừu vào chuồng.",
            MessageType.Info);

        GUILayout.Space(12);
        GUI.backgroundColor = new Color(0.2f, 0.8f, 0.4f);
        if (GUILayout.Button(" CẬP NHẬT VÀ LƯU VÀO GAME NGAY ", GUILayout.Height(45)))
        {
            ExecuteSetup(silent: false);
        }
    }

    [MenuItem("FarmSim/Chạy Cập nhật ngay", false, 2)]
    public static void MenuExecute()
    {
        ExecuteSetup(silent: false);
    }

    public static void ExecuteSetup(bool silent = false)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            if (!silent)
                EditorUtility.DisplayDialog("FarmSim", "Hay thoat Play Mode truoc khi chay cap nhat.", "OK");
            return;
        }

        if (!Directory.Exists("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!Directory.Exists("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");

        Sprite chickenSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/TinyFarm/Tiles/tile_0122.png");
        Sprite cowSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/TinyFarm/Tiles/tile_0121.png");
        Sprite sheepSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/TinyFarm/Tiles/tile_0120.png");
        Sprite eggSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/TinyFarm/Tiles/tile_0125.png");

        // 1. Tạo/Cập nhật Prefab Bò (Cow) - bước 0.45s
        GameObject cowPrefab = CreateOrUpdateAnimalPrefab("Cow", cowSprite, 0.45f, AnimalWander.AnimalType.Cow);
        // 2. Tạo/Cập nhật Prefab Cừu (Sheep) - bước 0.40s
        GameObject sheepPrefab = CreateOrUpdateAnimalPrefab("Sheep", sheepSprite, 0.40f, AnimalWander.AnimalType.Sheep);
        // 3. Tạo/Cập nhật Prefab Gà (Chicken) - bước 0.35s
        GameObject chickenPrefab = CreateOrUpdateChickenPrefab(chickenSprite, eggSprite);

        // 4. Thêm Bò và Cừu vào Scene đang mở nếu chưa có
        AddAnimalsToCurrentScene(cowPrefab, sheepPrefab);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("🎉 [FarmSim] ĐÃ CẬP NHẬT: Gà, Bò, Cừu di chuyển phẳng 1 ô (không nhảy) và thở nhẹ nhàng!");
        if (!silent)
        {
            EditorUtility.DisplayDialog("FarmSim",
                "Đã cập nhật xong!\n\n" +
                "- Các con vật (Gà, Bò, Cừu) bây giờ bước đi phẳng mượt mà 1 ô (KHÔNG CÒN NHẢY).\n" +
                "- Khi đi có hiệu ứng lắc thân nhẹ (waddle), khi đứng yên thở phập phồng nhẹ.\n\n" +
                "Bấm nút Play (▶) để xem các con vật bước đi nhé!", "Tuyệt vời!");
        }
    }

    static GameObject CreateOrUpdateAnimalPrefab(string name, Sprite sprite, float stepDuration, AnimalWander.AnimalType type)
    {
        string prefabPath = $"Assets/Prefabs/{name}.prefab";
        GameObject go = new GameObject(name);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = "Crops";

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.8f, 0.8f);

        var wander = go.AddComponent<AnimalWander>();
        var so = new SerializedObject(wander);
        so.FindProperty("animalType").enumValueIndex = (int)type;
        so.FindProperty("stepDistance").floatValue = 1.0f;
        so.FindProperty("stepDuration").floatValue = stepDuration;
        so.FindProperty("minWaitTime").floatValue = 2.0f;
        so.FindProperty("maxWaitTime").floatValue = 4.5f;
        so.FindProperty("maxStepFromHome").intValue = 4;
        so.FindProperty("enableIdleBreathing").boolValue = true;
        so.FindProperty("enableWalkWaddle").boolValue = true;
        so.FindProperty("spriteRenderer").objectReferenceValue = sr;
        so.ApplyModifiedProperties();

        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        DestroyImmediate(go);
        return savedPrefab;
    }

    static GameObject CreateOrUpdateChickenPrefab(Sprite chickenSprite, Sprite eggSprite)
    {
        string prefabPath = "Assets/Prefabs/Chicken.prefab";
        GameObject chicken = new GameObject("Chicken");
        var sr = chicken.AddComponent<SpriteRenderer>();
        sr.sprite = chickenSprite;
        sr.sortingLayerName = "Crops";

        var col = chicken.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.8f, 0.8f);

        GameObject eggObj = new GameObject("EggIcon");
        eggObj.transform.SetParent(chicken.transform);
        eggObj.transform.localPosition = new Vector3(0, 0.6f, 0);
        var eggSr = eggObj.AddComponent<SpriteRenderer>();
        eggSr.sprite = eggSprite;
        eggSr.sortingLayerName = "Crops";
        eggSr.sortingOrder = 5;

        var chickComp = chicken.AddComponent<Chicken>();
        var chickSo = new SerializedObject(chickComp);
        chickSo.FindProperty("body").objectReferenceValue = sr;
        chickSo.FindProperty("eggIcon").objectReferenceValue = eggObj;
        var eggAsset = AssetDatabase.LoadAssetAtPath<CropData>("Assets/Data/Crops/Egg.asset");
        if (eggAsset != null) chickSo.FindProperty("eggItem").objectReferenceValue = eggAsset;
        chickSo.ApplyModifiedProperties();

        var wander = chicken.AddComponent<AnimalWander>();
        var so = new SerializedObject(wander);
        so.FindProperty("animalType").enumValueIndex = (int)AnimalWander.AnimalType.Chicken;
        so.FindProperty("stepDistance").floatValue = 1.0f;
        so.FindProperty("stepDuration").floatValue = 0.35f;
        so.FindProperty("minWaitTime").floatValue = 1.5f;
        so.FindProperty("maxWaitTime").floatValue = 4.0f;
        so.FindProperty("maxStepFromHome").intValue = 3;
        so.FindProperty("enableIdleBreathing").boolValue = true;
        so.FindProperty("enableWalkWaddle").boolValue = true;
        so.FindProperty("spriteRenderer").objectReferenceValue = sr;
        so.ApplyModifiedProperties();

        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(chicken, prefabPath);
        DestroyImmediate(chicken);
        return savedPrefab;
    }

    static void AddAnimalsToCurrentScene(GameObject cowPrefab, GameObject sheepPrefab)
    {
        Scene currentScene = SceneManager.GetActiveScene();
        if (!currentScene.isLoaded) return;

        // Chi spawn vao scene Main — tranh de Cow/Sheep len MainMenu
        if (currentScene.name != "Main")
        {
            Debug.Log($"[FarmSim] Bo qua spawn dong vat tren scene '{currentScene.name}' (chi Main).");
            return;
        }

        bool changed = false;

        // Kiểm tra xem đã có Bò chưa
        if (GameObject.Find("Cow") == null && cowPrefab != null)
        {
            GameObject cow = (GameObject)PrefabUtility.InstantiatePrefab(cowPrefab);
            cow.name = "Cow";
            cow.transform.position = new Vector3(3.5f, 3.5f, 0f);
            Undo.RegisterCreatedObjectUndo(cow, "Add Cow");
            changed = true;
        }

        // Kiểm tra xem đã có Cừu chưa
        if (GameObject.Find("Sheep") == null && sheepPrefab != null)
        {
            GameObject sheep = (GameObject)PrefabUtility.InstantiatePrefab(sheepPrefab);
            sheep.name = "Sheep";
            sheep.transform.position = new Vector3(5.0f, 3.0f, 0f);
            Undo.RegisterCreatedObjectUndo(sheep, "Add Sheep");
            changed = true;
        }

        // Cập nhật các con gà trong ChickensRoot
        GameObject chickensRoot = GameObject.Find("ChickensRoot");
        if (chickensRoot != null)
        {
            foreach (Transform chickTransform in chickensRoot.transform)
            {
                var wander = chickTransform.GetComponent<AnimalWander>();
                if (wander == null)
                    wander = chickTransform.gameObject.AddComponent<AnimalWander>();

                var so = new SerializedObject(wander);
                so.FindProperty("stepDistance").floatValue = 1.0f;
                so.FindProperty("stepDuration").floatValue = 0.35f;
                so.FindProperty("enableIdleBreathing").boolValue = true;
                so.FindProperty("enableWalkWaddle").boolValue = true;
                so.ApplyModifiedProperties();
                changed = true;
            }
        }

        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(currentScene);
            EditorSceneManager.SaveScene(currentScene);
        }
    }
    [MenuItem("FarmSim/Cài đặt va chạm (Chuồng, Máng nước, Decor & NPC)", false, 2)]
    public static void MenuSetupColliders()
    {
        ExecuteColliderSetup(silent: false);
    }

    public static void ExecuteColliderSetup(bool silent = true)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        // 1. CÃ i Ä‘áº·t ColliderType:
        // - Chá»‰ duy nháº¥t cÃ¢y trá»“ng máº«u cá»§a NPC (tilemap_12, tilemap_24, tilemap_36) cÃ³ ColliderType = None (Ä‘i xuyÃªn qua)
        // - ToÃ n bá»™ cÃ¡c Tile khÃ¡c (MÃ¡ng nÆ°á»›c, mÃ¡ng Äƒn, cÃ¢y cá»‘i, Ä‘Ã¡, hÃ ng rÃ o, nhÃ  cá»­a...) Ä‘á»u cÃ³ ColliderType = Grid (cháº·n cá»©ng 100%)
        string[] passableTileNames = new string[] {
            "tilemap_12", "tilemap_24", "tilemap_36"
        };
        foreach (var tName in passableTileNames)
        {
            var tile = AssetDatabase.LoadAssetAtPath<UnityEngine.Tilemaps.Tile>($"Assets/TileMap/{tName}.asset");
            if (tile != null && tile.colliderType != UnityEngine.Tilemaps.Tile.ColliderType.None)
            {
                tile.colliderType = UnityEngine.Tilemaps.Tile.ColliderType.None;
                EditorUtility.SetDirty(tile);
            }
        }

        // Äáº£m báº£o táº¥t cáº£ cÃ¡c Tile khÃ¡c trong Assets/TileMap Ä‘á»u lÃ  Grid (cháº·n 100%)
        string[] allTileGuids = AssetDatabase.FindAssets("t:Tile", new[] { "Assets/TileMap" });
        foreach (var guid in allTileGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var tile = AssetDatabase.LoadAssetAtPath<UnityEngine.Tilemaps.Tile>(path);
            if (tile == null) continue;

            bool isPassable = false;
            foreach (var p in passableTileNames)
            {
                if (tile.name.Equals(p, System.StringComparison.OrdinalIgnoreCase))
                {
                    isPassable = true;
                    break;
                }
            }

            if (!isPassable && tile.colliderType != UnityEngine.Tilemaps.Tile.ColliderType.Grid)
            {
                tile.colliderType = UnityEngine.Tilemaps.Tile.ColliderType.Grid;
                EditorUtility.SetDirty(tile);
            }
        }
        AssetDatabase.SaveAssets();

        // 2. Danh sach Tilemap va GameObject can CHAN CUNG (Chuong, Mang nuoc, Mang an, Hang rao, Nha, Cay coi, NPC)
        string[] solidTargetNames = new string[] {
            "Decor",
            "Fences",
            "House",
            "Roof",
            "NpcChick",
            "NpcCow",
            "NpcSheep",
            "ChickenShop",
            "Buildings",
            "Structures"
        };

        // Danh sach can CHO PHEP DI QUA (Ruong trong cay, ruong mau, mat dat)
        string[] passableTargetNames = new string[] {
            "Field",
            "TreeToSell",
            "NPCcrop",
            "Ground"
        };

        // 3. Xu ly triet de cho tat ca cac Tilemap chuong, mang nuoc, hang rao, nha cua, cay coi
        string[] obstacleTilemapNames = new string[] { "Decor", "Fences", "House", "Roof" };
        int solidCount = 0;

        foreach (var tmName in obstacleTilemapNames)
        {
            var go = FindGameObjectAnywhere(tmName);
            if (go == null) continue;
            var tm = go.GetComponent<UnityEngine.Tilemaps.Tilemap>();
            if (tm == null) continue;

            BoundsInt bounds = tm.cellBounds;
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    var pos = new Vector3Int(x, y, 0);
                    if (!tm.HasTile(pos)) continue;

                    var tile = tm.GetTile(pos);
                    string tName = tile != null ? tile.name : "";

                    // Neu la cay trong mau NPC tren Decor thi cho di xuyen qua (None)
                    if (tmName.Equals("Decor", System.StringComparison.OrdinalIgnoreCase) &&
                        (tName.Equals("tilemap_12", System.StringComparison.OrdinalIgnoreCase) ||
                         tName.Equals("tilemap_24", System.StringComparison.OrdinalIgnoreCase) ||
                         tName.Equals("tilemap_36", System.StringComparison.OrdinalIgnoreCase)))
                    {
                        tm.SetColliderType(pos, UnityEngine.Tilemaps.Tile.ColliderType.None);
                    }
                    else
                    {
                        // Tat ca cac o khac (Mang nuoc, mang an, hang rao, cay coi, nha cua...) deu chan 100%
                        tm.SetColliderType(pos, UnityEngine.Tilemaps.Tile.ColliderType.Grid);
                    }
                }
            }
            tm.RefreshAllTiles();
            AddTilemapColliders(go);
            solidCount++;
        }

        // Cai dat BoxCollider2D cho cac NPC
        string[] npcNames = new string[] { "NpcChick", "NpcCow", "NpcSheep", "ChickenShop" };
        foreach (var name in npcNames)
        {
            var go = FindGameObjectAnywhere(name);
            if (go != null)
            {
                if (go.GetComponent<Collider2D>() == null)
                {
                    var box = Undo.AddComponent<BoxCollider2D>(go);
                    box.size = new Vector2(1f, 1f);
                    box.isTrigger = false;
                }
                solidCount++;
            }
        }

        // Quet tat ca cac Grid trong Scene de xu ly moi Tilemap con
        var allTilemaps = Object.FindObjectsByType<UnityEngine.Tilemaps.Tilemap>();
        foreach (var tm in allTilemaps)
        {
            string tmName = tm.name;
            bool isPassable = false;
            foreach (var p in passableTargetNames)
            {
                if (string.Equals(tmName, p, System.StringComparison.OrdinalIgnoreCase))
                {
                    isPassable = true;
                    break;
                }
            }

            if (isPassable)
            {
                RemoveColliders(tm.gameObject);
                BoundsInt bounds = tm.cellBounds;
                for (int x = bounds.xMin; x < bounds.xMax; x++)
                {
                    for (int y = bounds.yMin; y < bounds.yMax; y++)
                    {
                        var pos = new Vector3Int(x, y, 0);
                        if (tm.HasTile(pos))
                        {
                            tm.SetColliderType(pos, UnityEngine.Tilemaps.Tile.ColliderType.None);
                        }
                    }
                }
                tm.RefreshAllTiles();
            }
        }

        // 4. Go bo va cham cho Ruong Field, TreeToSell, NPCcrop
        int passableCount = 0;
        foreach (var name in passableTargetNames)
        {
            var go = FindGameObjectAnywhere(name);
            if (go != null)
            {
                RemoveColliders(go);
                passableCount++;
            }
        }

        var scene = SceneManager.GetActiveScene();
        if (scene.isLoaded)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        if (!silent)
        {
            EditorUtility.DisplayDialog("FarmSim",
                "DA CAI DAT VA CHAM THANH CONG!\n\n" +
                "1. CHAN CUNG KHONG THE DI XUYEN QUA:\n" +
                "   - Mang nuoc & Mang thuc an\n" +
                "   - Chuong & Hang rao (Fences)\n" +
                "   - Toan bo Decor (Cay thong vien, cay coi, da, thung...)\n" +
                "   - Nha cua & Mai nha (House, Roof)\n" +
                "   - Cac NPC (NpcChick, NpcCow, NpcSheep, ChickenShop)\n\n" +
                "2. CHO PHEP DI XUYEN QUA TU DO:\n" +
                "   - Ruong trong cay cua nguoi choi (Field, TreeToSell)\n" +
                "   - Cay trong mau cua NPC (tilemap_12, 24, 36)\n\n" +
                "Hay bam Play de thu nghiem nhe!",
                "Tuyet voi!");
        }
    }

    public static void AddTilemapColliders(GameObject go)
    {
        var tm = go.GetComponent<UnityEngine.Tilemaps.Tilemap>();
        if (tm != null)
        {
            tm.RefreshAllTiles();
        }

        var tilemapCol = go.GetComponent<UnityEngine.Tilemaps.TilemapCollider2D>();
        if (tilemapCol == null) tilemapCol = Undo.AddComponent<UnityEngine.Tilemaps.TilemapCollider2D>(go);
        tilemapCol.compositeOperation = Collider2D.CompositeOperation.Merge;
        tilemapCol.isTrigger = false;

        var compCol = go.GetComponent<CompositeCollider2D>();
        if (compCol == null) compCol = Undo.AddComponent<CompositeCollider2D>(go);
        compCol.geometryType = CompositeCollider2D.GeometryType.Polygons;
        compCol.generationType = CompositeCollider2D.GenerationType.Synchronous;

        var rb = go.GetComponent<Rigidbody2D>();
        if (rb == null) rb = Undo.AddComponent<Rigidbody2D>(go);
        rb.bodyType = RigidbodyType2D.Static;
        rb.simulated = true;

        compCol.GenerateGeometry();
    }

    public static void RemoveColliders(GameObject go)
    {
        var compCol = go.GetComponent<CompositeCollider2D>();
        if (compCol != null) Undo.DestroyObjectImmediate(compCol);

        var rb = go.GetComponent<Rigidbody2D>();
        if (rb != null) Undo.DestroyObjectImmediate(rb);

        var tilemapCol = go.GetComponent<UnityEngine.Tilemaps.TilemapCollider2D>();
        if (tilemapCol != null) Undo.DestroyObjectImmediate(tilemapCol);

        var anyCol = go.GetComponent<Collider2D>();
        if (anyCol != null) Undo.DestroyObjectImmediate(anyCol);
    }

    public static GameObject FindGameObjectAnywhere(string name)
    {
        var go = GameObject.Find(name);
        if (go != null) return go;

        var scene = SceneManager.GetActiveScene();
        if (!scene.isLoaded) return null;

        foreach (var root in scene.GetRootGameObjects())
        {
            var found = FindInChildren(root.transform, name);
            if (found != null) return found.gameObject;
        }
        return null;
    }

    static Transform FindInChildren(Transform parent, string name)
    {
        if (string.Equals(parent.name, name, System.StringComparison.OrdinalIgnoreCase))
            return parent;
        foreach (Transform child in parent)
        {
            var res = FindInChildren(child, name);
            if (res != null) return res;
        }
        return null;
    }

    [MenuItem("FarmSim/Xáº¿p GÃ , BÃ², Cá»«u vÃ o Ä‘Ãºng chuá»“ng", false, 3)]
    public static void PlaceAnimalsIntoPens()
    {
        ExecutePlaceAnimals(silent: false);
    }

    public static void ExecutePlaceAnimals(bool silent = true)
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.isLoaded) return;
        if (scene.name != "Main")
        {
            if (!silent)
                EditorUtility.DisplayDialog("FarmSim", "Chi xep dong vat tren scene Main.", "OK");
            return;
        }

        // Load Prefabs
        GameObject chickenPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Chicken.prefab");
        GameObject cowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Cow.prefab");
        GameObject sheepPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Sheep.prefab");

        // 1. CHUá»’NG GÃ€ (GÃ³c trÃªn bÃªn pháº£i, cáº¡nh NpcChick: X=8.5..12.5, Y=-15..-13)
        GameObject chickensRoot = GameObject.Find("ChickensRoot");
        if (chickensRoot == null)
        {
            chickensRoot = new GameObject("ChickensRoot");
            Undo.RegisterCreatedObjectUndo(chickensRoot, "Create ChickensRoot");
        }

        Vector3[] chickenPositions = new Vector3[] {
            new Vector3(9.5f, -14.0f, 0f),
            new Vector3(11.0f, -13.5f, 0f),
            new Vector3(12.0f, -14.5f, 0f)
        };

        int chickenIdx = 0;
        foreach (Transform child in chickensRoot.transform)
        {
            if (chickenIdx < chickenPositions.Length)
            {
                Undo.RecordObject(child, "Move Chicken");
                child.position = chickenPositions[chickenIdx++];
                ConfigureWander(child.gameObject, 2);
            }
        }
        while (chickenIdx < chickenPositions.Length && chickenPrefab != null)
        {
            GameObject chick = (GameObject)PrefabUtility.InstantiatePrefab(chickenPrefab, chickensRoot.transform);
            chick.name = $"Chicken_{chickenIdx + 1}";
            chick.transform.position = chickenPositions[chickenIdx++];
            ConfigureWander(chick, 2);
            Undo.RegisterCreatedObjectUndo(chick, "Spawn Chicken");
        }

        // 2. CHUá»’NG BÃ’ (GÃ³c dÆ°á»›i bÃªn trÃ¡i, dÆ°á»›i nhÃ  giá»¯a cÃ³ mÃ¡ng Äƒn: X=-15.5..-12.5, Y=-26..-24)
        GameObject cowsRoot = GameObject.Find("CowsRoot");
        if (cowsRoot == null)
        {
            cowsRoot = new GameObject("CowsRoot");
            Undo.RegisterCreatedObjectUndo(cowsRoot, "Create CowsRoot");
        }

        Vector3[] cowPositions = new Vector3[] {
            new Vector3(-15.0f, -25.0f, 0f),
            new Vector3(-13.0f, -25.5f, 0f)
        };

        var existingCow = GameObject.Find("Cow");
        if (existingCow != null && existingCow.transform.parent != cowsRoot.transform)
        {
            Undo.SetTransformParent(existingCow.transform, cowsRoot.transform, "Parent Cow");
        }

        int cowIdx = 0;
        foreach (Transform child in cowsRoot.transform)
        {
            if (cowIdx < cowPositions.Length)
            {
                Undo.RecordObject(child, "Move Cow");
                child.position = cowPositions[cowIdx++];
                ConfigureWander(child.gameObject, 2);
            }
        }
        while (cowIdx < cowPositions.Length && cowPrefab != null)
        {
            GameObject cow = (GameObject)PrefabUtility.InstantiatePrefab(cowPrefab, cowsRoot.transform);
            cow.name = $"Cow_{cowIdx + 1}";
            cow.transform.position = cowPositions[cowIdx++];
            ConfigureWander(cow, 2);
            Undo.RegisterCreatedObjectUndo(cow, "Spawn Cow");
        }

        // 3. CHUá»’NG Cá»ªU (GÃ³c dÆ°á»›i bÃªn pháº£i, cáº¡nh NpcSheep: X=5.5..8.5, Y=-24..-22.5)
        GameObject sheepRoot = GameObject.Find("SheepRoot");
        if (sheepRoot == null)
        {
            sheepRoot = new GameObject("SheepRoot");
            Undo.RegisterCreatedObjectUndo(sheepRoot, "Create SheepRoot");
        }

        Vector3[] sheepPositions = new Vector3[] {
            new Vector3(6.5f, -23.0f, 0f),
            new Vector3(8.0f, -23.5f, 0f)
        };

        var existingSheep = GameObject.Find("Sheep");
        if (existingSheep != null && existingSheep.transform.parent != sheepRoot.transform)
        {
            Undo.SetTransformParent(existingSheep.transform, sheepRoot.transform, "Parent Sheep");
        }

        int sheepIdx = 0;
        foreach (Transform child in sheepRoot.transform)
        {
            if (sheepIdx < sheepPositions.Length)
            {
                Undo.RecordObject(child, "Move Sheep");
                child.position = sheepPositions[sheepIdx++];
                ConfigureWander(child.gameObject, 2);
            }
        }
        while (sheepIdx < sheepPositions.Length && sheepPrefab != null)
        {
            GameObject sheep = (GameObject)PrefabUtility.InstantiatePrefab(sheepPrefab, sheepRoot.transform);
            sheep.name = $"Sheep_{sheepIdx + 1}";
            sheep.transform.position = sheepPositions[sheepIdx++];
            ConfigureWander(sheep, 2);
            Undo.RegisterCreatedObjectUndo(sheep, "Spawn Sheep");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (!silent)
        {
            EditorUtility.DisplayDialog("FarmSim",
                "ÄÃƒ Xáº¾P GÃ€, BÃ’, Cá»ªU VÃ€O ÄÃšNG Tá»ªNG CHUá»’NG!\n\n" +
                "- 3 GÃ : ÄÃ£ xáº¿p vÃ o Chuá»“ng GÃ  (gÃ³c trÃªn bÃªn pháº£i, cáº¡nh NpcChick)\n" +
                "- 2 BÃ²: ÄÃ£ xáº¿p vÃ o Chuá»“ng BÃ² (gÃ³c dÆ°á»›i bÃªn trÃ¡i, dÆ°á»›i nhÃ  giá»¯a cÃ³ mÃ¡ng Äƒn)\n" +
                "- 2 Cá»«u: ÄÃ£ xáº¿p vÃ o Chuá»“ng Cá»«u (gÃ³c dÆ°á»›i bÃªn pháº£i, cáº¡nh NpcSheep)\n\n" +
                "Táº¥t cáº£ con váº­t Ä‘Ã£ Ä‘Æ°á»£c cÃ i Ä‘áº·t Ä‘i pháº³ng 1 Ã´, láº¯c thÃ¢n nháº¹, thá»Ÿ nháº¹ vÃ  á»Ÿ yÃªn trong chuá»“ng!",
                "Tuyá»‡t vá»i");
        }
    }

    static void ConfigureWander(GameObject go, int maxSteps)
    {
        var wander = go.GetComponent<AnimalWander>();
        if (wander == null) wander = Undo.AddComponent<AnimalWander>(go);

        var so = new SerializedObject(wander);
        so.FindProperty("stepDistance").floatValue = 1.0f;
        so.FindProperty("stepDuration").floatValue = 0.40f;
        so.FindProperty("maxStepFromHome").intValue = maxSteps;
        so.FindProperty("enableIdleBreathing").boolValue = true;
        so.FindProperty("enableWalkWaddle").boolValue = true;
        so.ApplyModifiedProperties();
    }
}
#endif
