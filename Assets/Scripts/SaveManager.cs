using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
    [SerializeField] CropDatabase cropDb;
    public CropDatabase CropDb => cropDb;
    [SerializeField] PlotUnlocker unlocker;         // mở ô đất
    [SerializeField] PlotUnlocker chickenUnlocker;  // mở gà
    [SerializeField] PlotUnlocker cowUnlocker;
    [SerializeField] PlotUnlocker sheepUnlocker;
    [SerializeField] OrderManager orderManager;
    [SerializeField] WorldEnvironment environment;
    [SerializeField] float autoSaveInterval = 30f;

    Plot[] plots;
    Chicken[] chickens;
    bool resetting;

    void Start()
    {
        // Gồm cả đối tượng đang tắt; sắp theo vị trí để thứ tự luôn cố định
        plots = FindObjectsByType<Plot>(FindObjectsInactive.Include)
            .OrderBy(p => p.transform.position.x)
            .ThenBy(p => p.transform.position.y)
            .ToArray();

        chickens = FindObjectsByType<Chicken>(FindObjectsInactive.Include)
            .OrderBy(c => c.transform.position.x)
            .ThenBy(c => c.transform.position.y)
            .ToArray();

        Load();
        InvokeRepeating(nameof(Save), autoSaveInterval, autoSaveInterval);
    }

    public void Save()
    {
        if (resetting) return;

        var gm = GameManager.Instance;
        if (gm == null || DayCycle.Instance == null) return;

        var data = new SaveData
        {
            money = gm.Money,
            day = DayCycle.Instance.Day,
            timeOfDay = DayCycle.Instance.TimeOfDay,
            weather = environment != null ? environment.WeatherIndex : 0,
            unlockedPlots = unlocker != null ? unlocker.UnlockedCount : 0,
            unlockedChickens = chickenUnlocker != null ? chickenUnlocker.UnlockedCount : 0,
            unlockedCows = cowUnlocker != null ? cowUnlocker.UnlockedCount : 0,
            unlockedSheep = sheepUnlocker != null ? sheepUnlocker.UnlockedCount : 0
        };

        if (plots != null)
        {
            foreach (var p in plots)
            {
                if (p == null) continue;
                data.plots.Add(new PlotSave
                {
                    cropName = p.Crop != null ? p.Crop.cropName : "",
                    plantedTicks = p.PlantedTicks,
                    isTilled = p.IsTilled,
                    isWatered = p.IsWatered
                });
            }
        }

        if (chickens != null)
        {
            foreach (var c in chickens)
                if (c != null) data.chickenFedTicks.Add(c.FedTicks);
        }

        foreach (var kv in gm.Harvested)
        {
            if (kv.Key == null) continue;
            data.harvest.Add(new HarvestSave { cropName = kv.Key.cropName, count = kv.Value });
        }

        data.seeds.AddRange(gm.GetSeedSaves());

        if (orderManager != null) data.orders.AddRange(orderManager.Orders);
        data.ownedTools.AddRange(gm.GetOwnedToolIds());

        SaveSystem.Save(data);
    }

    void Load()
    {
        var data = SaveSystem.Load();
        if (data == null) return;

        // Save cũ có thể thiếu các trường mới
        data.plots ??= new();
        data.harvest ??= new();
        data.seeds ??= new();
        data.chickenFedTicks ??= new();
        data.orders ??= new();
        data.ownedTools ??= new();

        var gm = GameManager.Instance;
        gm.Money = data.money;
        gm.ApplyOwnedTools(data.ownedTools);
        gm.ApplySeeds(data.seeds, cropDb);

        DayCycle.Instance.Day = Mathf.Max(1, data.day);
        DayCycle.Instance.TimeOfDay = data.timeOfDay;

        // Bật ô và vật nuôi trước khi nạp trạng thái
        if (unlocker != null) unlocker.ApplyUnlocked(data.unlockedPlots);
        if (chickenUnlocker != null) chickenUnlocker.ApplyUnlocked(data.unlockedChickens);
        if (cowUnlocker != null) cowUnlocker.ApplyUnlocked(data.unlockedCows);
        if (sheepUnlocker != null) sheepUnlocker.ApplyUnlocked(data.unlockedSheep);

        for (int i = 0; i < plots.Length && i < data.plots.Count; i++)
        {
            var s = data.plots[i];
            var crop = string.IsNullOrEmpty(s.cropName) ? null : cropDb.Find(s.cropName);
            plots[i].SetState(crop, s.plantedTicks, s.isTilled, s.isWatered);
        }

        for (int i = 0; i < chickens.Length && i < data.chickenFedTicks.Count; i++)
            chickens[i].SetFed(data.chickenFedTicks[i]);

        gm.Harvested.Clear();
        foreach (var h in data.harvest)
        {
            var crop = cropDb.Find(h.cropName);
            if (crop != null) gm.Harvested[crop] = h.count;
        }

        if (environment != null) environment.Restore(data.weather); // sau khi đã nạp ngày
        if (orderManager != null) orderManager.Restore(data.orders);

        gm.RefreshAll();
    }

    // Gắn vào nút "Chơi lại từ đầu"
    public void ResetSave()
    {
        resetting = true;
        CancelInvoke(nameof(Save));
        SaveSystem.Delete();
        Time.timeScale = 1f;
        ControlsGuide.Pending = true;
        var scene = SceneManager.GetActiveScene();
        if (scene.buildIndex >= 0)
            SceneManager.LoadScene(scene.buildIndex);
        else
            SceneManager.LoadScene(scene.name);
    }

    void OnApplicationQuit() => Save();
    void OnApplicationPause(bool pause) { if (pause) Save(); }
}
