#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FarmSimAutoSetup : EditorWindow
{
    [MenuItem("FarmSim/Tạo Player và Động vật (1-Click Setup)", false, 1)]
    public static void ShowWindow()
    {
        var window = GetWindow<FarmSimAutoSetup>("FarmSim Setup");
        window.minSize = new Vector2(380, 220);
        window.Show();
    }

    void OnGUI()
    {
        GUILayout.Space(15);
        GUILayout.Label("CÔNG CỤ TẠO PLAYER VÀ CÁC CON VẬT", EditorStyles.boldLabel);
        GUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "Bấm nút bên dưới để tự động tạo:\n" +
            "1. Prefab Player (kèm điều khiển, animation chạy/đứng)\n" +
            "2. Prefab Gà (Chicken), Bò, Heo, Chó (kèm di chuyển tự nhiên)\n" +
            "3. Dựng sẵn toàn bộ vào scene Dev_B và gắn CameraFollow\n" +
            "4. Bấm Play là chơi thử được ngay!",
            MessageType.Info);

        GUILayout.Space(15);
        GUI.backgroundColor = new Color(0.3f, 0.85f, 0.4f);
        if (GUILayout.Button(" BẮT ĐẦU TẠO TOÀN BỘ NGAY ", GUILayout.Height(45)))
        {
            ExecuteSetup();
        }
    }

    [MenuItem("FarmSim/Tự động chạy tạo ngay", false, 2)]
    public static void ExecuteSetup()
    {
        // 1. Ensure directories exist
        if (!Directory.Exists("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!Directory.Exists("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");

        // 2. Open or create Dev_B scene
        string devBPath = "Assets/Scenes/Dev_B.unity";
        Scene scene;
        if (File.Exists(devBPath))
        {
            scene = EditorSceneManager.OpenScene(devBPath);
        }
        else
        {
            scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, devBPath);
        }

        // 3. Load Player sprites
        Sprite[] idleSprites = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Player/Player_Idle.png")
            .OfType<Sprite>().OrderBy(s => s.name).ToArray();
        Sprite[] runSprites = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Player/Player_Run.png")
            .OfType<Sprite>().OrderBy(s => s.name).ToArray();

        Sprite playerDefaultSprite = (idleSprites != null && idleSprites.Length > 0) ? idleSprites[0] : null;

        // 4. Load Animal sprites from TinyFarm Tiles
        Sprite chickenSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/TinyFarm/Tiles/tile_0121.png");
        Sprite eggSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/TinyFarm/Tiles/tile_0125.png");
        Sprite cowSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/TinyFarm/Tiles/tile_0120.png");
        Sprite dogSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/TinyFarm/Tiles/tile_0123.png");
        Sprite pigSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/TinyFarm/Tiles/tile_0124.png");

        // 5. Create or configure Player
        GameObject player = GameObject.Find("Player");
        if (player == null) player = new GameObject("Player");
        player.tag = "Player";
        player.transform.position = Vector3.zero;

        // Rigidbody2D
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb == null) rb = player.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // Capsule Collider 2D
        var col = player.GetComponent<CapsuleCollider2D>();
        if (col == null) col = player.AddComponent<CapsuleCollider2D>();
        col.size = new Vector2(0.5f, 0.5f);
        col.offset = new Vector2(0, -0.2f);

        // Child Sprite
        Transform spriteChild = player.transform.Find("Sprite");
        GameObject spriteObj;
        if (spriteChild == null)
        {
            spriteObj = new GameObject("Sprite");
            spriteObj.transform.SetParent(player.transform);
            spriteObj.transform.localPosition = Vector3.zero;
        }
        else
        {
            spriteObj = spriteChild.gameObject;
        }

        var sr = spriteObj.GetComponent<SpriteRenderer>();
        if (sr == null) sr = spriteObj.AddComponent<SpriteRenderer>();
        if (playerDefaultSprite != null) sr.sprite = playerDefaultSprite;
        sr.sortingLayerName = "Player";

        // PlayerController
        var ctrl = player.GetComponent<PlayerController>();
        if (ctrl == null) ctrl = player.AddComponent<PlayerController>();

        // PlayerVisual
        var vis = player.GetComponent<PlayerVisual>();
        if (vis == null) vis = player.AddComponent<PlayerVisual>();
        var serializedVis = new SerializedObject(vis);
        serializedVis.FindProperty("body").objectReferenceValue = rb;
        serializedVis.FindProperty("sprite").objectReferenceValue = sr;
        serializedVis.FindProperty("bobSpeed").floatValue = 14f;
        serializedVis.FindProperty("bobAmount").floatValue = 0.05f;

        // Assign sprite animations if available
        if (idleSprites != null && idleSprites.Length > 0)
        {
            var idleProp = serializedVis.FindProperty("idleSprites");
            idleProp.arraySize = idleSprites.Length;
            for (int i = 0; i < idleSprites.Length; i++)
                idleProp.GetArrayElementAtIndex(i).objectReferenceValue = idleSprites[i];
        }
        if (runSprites != null && runSprites.Length > 0)
        {
            var runProp = serializedVis.FindProperty("runSprites");
            runProp.arraySize = runSprites.Length;
            for (int i = 0; i < runSprites.Length; i++)
                runProp.GetArrayElementAtIndex(i).objectReferenceValue = runSprites[i];
        }
        serializedVis.ApplyModifiedProperties();

        // Save Player Prefab
        PrefabUtility.SaveAsPrefabAsset(player, "Assets/Prefabs/Player.prefab");

        // 6. Create Chicken (Gà)
        GameObject chicken = GameObject.Find("Chicken");
        if (chicken == null) chicken = new GameObject("Chicken");
        chicken.transform.position = new Vector3(2.5f, 0.5f, 0);

        var chickenSr = chicken.GetComponent<SpriteRenderer>();
        if (chickenSr == null) chickenSr = chicken.AddComponent<SpriteRenderer>();
        if (chickenSprite != null) chickenSr.sprite = chickenSprite;
        chickenSr.sortingLayerName = "Crops";

        var chickenCol = chicken.GetComponent<BoxCollider2D>();
        if (chickenCol == null) chickenCol = chicken.AddComponent<BoxCollider2D>();
        chickenCol.isTrigger = true;
        chickenCol.size = new Vector2(0.8f, 0.8f);

        // Child EggIcon
        Transform eggChild = chicken.transform.Find("EggIcon");
        GameObject eggObj;
        if (eggChild == null)
        {
            eggObj = new GameObject("EggIcon");
            eggObj.transform.SetParent(chicken.transform);
            eggObj.transform.localPosition = new Vector3(0, 0.6f, 0);
        }
        else
        {
            eggObj = eggChild.gameObject;
        }
        var eggSr = eggObj.GetComponent<SpriteRenderer>();
        if (eggSr == null) eggSr = eggObj.AddComponent<SpriteRenderer>();
        if (eggSprite != null) eggSr.sprite = eggSprite;
        eggSr.sortingLayerName = "Crops";
        eggSr.sortingOrder = 5;

        // Chicken component
        var chickComp = chicken.GetComponent<Chicken>();
        if (chickComp == null) chickComp = chicken.AddComponent<Chicken>();
        var chickSo = new SerializedObject(chickComp);
        chickSo.FindProperty("body").objectReferenceValue = chickenSr;
        chickSo.FindProperty("eggIcon").objectReferenceValue = eggObj;
        chickSo.ApplyModifiedProperties();

        // Animal wander for chicken
        var chickWander = chicken.GetComponent<AnimalWander>();
        if (chickWander == null) chickWander = chicken.AddComponent<AnimalWander>();
        var chickWanderSo = new SerializedObject(chickWander);
        chickWanderSo.FindProperty("moveSpeed").floatValue = 0.8f;
        chickWanderSo.FindProperty("roamRadius").floatValue = 2f;
        chickWanderSo.FindProperty("bobSpeed").floatValue = 12f;
        chickWanderSo.ApplyModifiedProperties();

        // Save Chicken Prefab
        PrefabUtility.SaveAsPrefabAsset(chicken, "Assets/Prefabs/Chicken.prefab");

        // 7. Create Other Animals (Cow, Pig, Dog)
        CreateAnimalPrefab("Cow", new Vector3(-3f, 1.5f, 0), cowSprite, 0.6f, 3f, 8f, 0.03f);
        CreateAnimalPrefab("Pig", new Vector3(-2.5f, -1.8f, 0), pigSprite, 0.7f, 2.5f, 10f, 0.04f);
        CreateAnimalPrefab("Dog", new Vector3(3f, -1.5f, 0), dogSprite, 1.2f, 4f, 12f, 0.05f);

        // 8. Main Camera setup
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.orthographic = true;
            mainCam.orthographicSize = 5.6f;
            mainCam.transform.position = new Vector3(0, 0, -10);

            var camFollow = mainCam.GetComponent<CameraFollow>();
            if (camFollow == null) camFollow = mainCam.gameObject.AddComponent<CameraFollow>();
            var camSo = new SerializedObject(camFollow);
            camSo.FindProperty("target").objectReferenceValue = player.transform;
            camSo.FindProperty("smooth").floatValue = 8f;
            camSo.ApplyModifiedProperties();
        }

        // 9. Save Dev_B Scene
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, devBPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("🎉 [FarmSim] TỰ ĐỘNG THÀNH CÔNG! Đã tạo Player.prefab, Chicken.prefab, Cow.prefab, Pig.prefab, Dog.prefab và setup toàn bộ trong Scenes/Dev_B.unity! Hãy bấm Play để chơi thử!");
        EditorUtility.DisplayDialog("FarmSim Setup Hoàn Tất", "Đã tạo xong Player và các con vật (Gà, Bò, Heo, Chó) cùng toàn bộ Prefabs và chuyển động trong scene Dev_B!\n\nBây giờ bạn chỉ cần bấm nút Play (▶) để chơi thử!", "Tuyệt vời!");
    }

    static void CreateAnimalPrefab(string name, Vector3 pos, Sprite sprite, float speed, float radius, float bobSpeed, float bobAmount)
    {
        GameObject go = GameObject.Find(name);
        if (go == null) go = new GameObject(name);
        go.transform.position = pos;

        var sr = go.GetComponent<SpriteRenderer>();
        if (sr == null) sr = go.AddComponent<SpriteRenderer>();
        if (sprite != null) sr.sprite = sprite;
        sr.sortingLayerName = "Crops";

        var col = go.GetComponent<BoxCollider2D>();
        if (col == null) col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.8f, 0.8f);

        var wander = go.GetComponent<AnimalWander>();
        if (wander == null) wander = go.AddComponent<AnimalWander>();
        var so = new SerializedObject(wander);
        so.FindProperty("moveSpeed").floatValue = speed;
        so.FindProperty("roamRadius").floatValue = radius;
        so.FindProperty("bobSpeed").floatValue = bobSpeed;
        so.FindProperty("bobAmount").floatValue = bobAmount;
        so.ApplyModifiedProperties();

        PrefabUtility.SaveAsPrefabAsset(go, $"Assets/Prefabs/{name}.prefab");
    }
}
#endif
