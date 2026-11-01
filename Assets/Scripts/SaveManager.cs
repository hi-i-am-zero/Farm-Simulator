using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
    [SerializeField] CropDatabase cropDb;
    [SerializeField] PlotUnlocker unlocker;         // mở ô đất
    [SerializeField] PlotUnlocker chickenUnlocker;  // mở gà
    [SerializeField] OrderManager orderManager;
    [SerializeField] WorldEnvironment environment;
    [SerializeField] float autoSaveInterval = 30f;

    Plot[] plots;
    Chicken[] chickens;
    bool resetting;

    void Start()
    {
        // Gồm cả đối tượng đang tắt; sắp theo vị trí để thứ tự luôn cố định
        plots = FindObjectsByType<Plot>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .OrderBy(p => p.transform.position.x)
            .ThenBy(p => p.transform.position.y)
            .ToArray();

        chickens = FindObjectsByType<Chicken>(FindObjectsInactive.Include, FindObjectsSortMode.None)
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
        var data = new SaveData
        {
            money = gm.Money,
            day = DayCycle.Instance.Day,
            timeOfDay = DayCycle.Instance.TimeOfDay,
            weather = environment != null ? environment.WeatherIndex : 0,
            unlockedPlots = unlocker.UnlockedCount,
            unlockedChickens = chickenUnlocker != null ? chickenUnlocker.UnlockedCount : 0
        };

        foreach (var p in plots)
            data.plots.Add(new PlotSave
            {
                cropName = p.Crop != null ? p.Crop.cropName : "",
                plantedTicks = p.PlantedTicks
            });

        foreach (var c in chickens) data.chickenFedTicks.Add(c.FedTicks);

        foreach (var kv in gm.Harvested)
            data.harvest.Add(new HarvestSave { cropName = kv.Key.cropName, count = kv.Value });

        if (orderManager != null) data.orders.AddRange(orderManager.Orders);

        SaveSystem.Save(data);
    }

    void Load()
    {
        var data = SaveSystem.Load();
        if (data == null) return;

        // Save cũ có thể thiếu các trường mới
        data.plots ??= new();
        data.harvest ??= new();
        data.chickenFedTicks ??= new();
        data.orders ??= new();

        var gm = GameManager.Instance;
        gm.Money = data.money;

        DayCycle.Instance.Day = Mathf.Max(1, data.day);
        DayCycle.Instance.TimeOfDay = data.timeOfDay;

        // Bật ô và gà trước khi nạp trạng thái
        unlocker.ApplyUnlocked(data.unlockedPlots);
        if (chickenUnlocker != null) chickenUnlocker.ApplyUnlocked(data.unlockedChickens);

        for (int i = 0; i < plots.Length && i < data.plots.Count; i++)
        {
            var s = data.plots[i];
            var crop = string.IsNullOrEmpty(s.cropName) ? null : cropDb.Find(s.cropName);
            plots[i].SetCrop(crop, s.plantedTicks);
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
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void OnApplicationQuit() => Save();
    void OnApplicationPause(bool pause) { if (pause) Save(); }
}
