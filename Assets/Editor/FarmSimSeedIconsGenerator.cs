#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class FarmSimSeedIconsGenerator
{
    const string SeedsFolder = "Assets/Art/Seeds";

    [InitializeOnLoadMethod]
    static void AutoRunOnLoad()
    {
        string checkFile = $"{SeedsFolder}/Seed_Carrot.png";
        if (!File.Exists(checkFile))
        {
            GenerateAllSeedIcons();
        }
    }

    [MenuItem("FarmSim/Tao Icon Hat Giong Rieng", false, 30)]
    public static void GenerateAllSeedIcons()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Art"))
            AssetDatabase.CreateFolder("Assets", "Art");
        if (!AssetDatabase.IsValidFolder(SeedsFolder))
            AssetDatabase.CreateFolder("Assets/Art", "Seeds");

        // Nạp sprite túi hạt giống gốc (tile_0089)
        string basePath = "Assets/Art/TinyFarm/Tiles/tile_0089.png";
        var baseTex = LoadReadableTexture(basePath);
        if (baseTex == null)
        {
            Debug.LogError("[FarmSim] Không tìm thấy " + basePath);
            return;
        }

        // Định nghĩa màu sắc biểu tượng cho từng loại hạt giống
        CreateSeedIcon(baseTex, "Carrot", new Color(0.96f, 0.45f, 0.12f, 1f));   // Cam tươi cà rốt
        CreateSeedIcon(baseTex, "Tomato", new Color(0.92f, 0.20f, 0.20f, 1f));   // Đỏ cà chua
        CreateSeedIcon(baseTex, "Corn", new Color(0.95f, 0.78f, 0.12f, 1f));     // Vàng bắp ngô
        CreateSeedIcon(baseTex, "Pumpkin", new Color(0.85f, 0.40f, 0.08f, 1f));  // Cam đậm bí ngô
        CreateSeedIcon(baseTex, "Rice", new Color(0.55f, 0.75f, 0.25f, 1f));     // Xanh mạ lúa non

        AssetDatabase.Refresh();

        // Gán vào từng CropData asset
        AssignSeedIconToCrop("Carrot", $"{SeedsFolder}/Seed_Carrot.png");
        AssignSeedIconToCrop("Tomato", $"{SeedsFolder}/Seed_Tomato.png");
        AssignSeedIconToCrop("Corn", $"{SeedsFolder}/Seed_Corn.png");
        AssignSeedIconToCrop("Pumpkin", $"{SeedsFolder}/Seed_Pumpkin.png");
        AssignSeedIconToCrop("Rice", $"{SeedsFolder}/Seed_Rice.png");

        // Đảm bảo Rice có icon sản phẩm lúa chín (tile_0062) nếu chưa có
        var riceCrop = AssetDatabase.LoadAssetAtPath<CropData>("Assets/Data/Crops/Rice.asset");
        if (riceCrop != null && riceCrop.icon == null)
        {
            riceCrop.icon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/TinyFarm/Tiles/tile_0062.png");
            EditorUtility.SetDirty(riceCrop);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[FarmSim] Đã tạo và gán toàn bộ seedIcon riêng cho các loại cây trồng!");
    }

    static Texture2D LoadReadableTexture(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(bytes);
        return tex;
    }

    static void CreateSeedIcon(Texture2D baseTex, string cropName, Color badgeColor)
    {
        int w = baseTex.width;
        int h = baseTex.height;
        var newTex = new Texture2D(w, h, TextureFormat.RGBA32, false);

        // Copy pixel từ túi hạt gốc
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                Color c = baseTex.GetPixel(x, y);
                newTex.SetPixel(x, y, c);
            }
        }

        // Vẽ huy hiệu màu sắc đặc trưng của hạt giống lên mặt trước túi (khoảng giữa x: 5..10, y: 3..7)
        int cx = w / 2;
        int cy = h / 2 - 1;

        // Vẽ chấm tròn / kim cương màu 4x4 ở giữa túi
        for (int dy = -2; dy <= 2; dy++)
        {
            for (int dx = -2; dx <= 2; dx++)
            {
                int px = cx + dx;
                int py = cy + dy;
                if (px < 0 || px >= w || py < 0 || py >= h) continue;

                // Kiểm tra xem pixel nền của túi có phải trong suốt không
                Color bg = baseTex.GetPixel(px, py);
                if (bg.a < 0.5f) continue;

                if (Mathf.Abs(dx) + Mathf.Abs(dy) <= 2)
                {
                    // Viền tối nhẹ cho mầm/hạt
                    if (Mathf.Abs(dx) + Mathf.Abs(dy) == 2)
                        newTex.SetPixel(px, py, Color.Lerp(badgeColor, Color.black, 0.35f));
                    else
                        newTex.SetPixel(px, py, badgeColor);
                }
            }
        }

        // Điểm sáng highlight ở góc trên trái của hạt
        newTex.SetPixel(cx - 1, cy + 1, Color.Lerp(badgeColor, Color.white, 0.6f));

        newTex.Apply();

        byte[] png = newTex.EncodeToPNG();
        string outPath = $"{SeedsFolder}/Seed_{cropName}.png";
        File.WriteAllBytes(outPath, png);

        AssetDatabase.ImportAsset(outPath, ImportAssetOptions.ForceUpdate);

        // Cấu hình TextureImporter cho pixel art sắc nét
        var importer = AssetImporter.GetAtPath(outPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spritePixelsPerUnit = 16f;
            importer.SaveAndReimport();
        }
    }

    static void AssignSeedIconToCrop(string cropName, string seedSpritePath)
    {
        string cropPath = $"Assets/Data/Crops/{cropName}.asset";
        var crop = AssetDatabase.LoadAssetAtPath<CropData>(cropPath);
        if (crop == null) return;

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(seedSpritePath);
        if (sprite != null)
        {
            crop.seedIcon = sprite;
            EditorUtility.SetDirty(crop);
        }
    }
}
#endif
