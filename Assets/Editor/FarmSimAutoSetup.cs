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
        FarmSimAutoSetup.ExecuteSetup(silent: true);
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
}
#endif
