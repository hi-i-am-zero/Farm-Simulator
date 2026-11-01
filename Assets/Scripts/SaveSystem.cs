using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable] public class PlotSave { public string cropName; public long plantedTicks; }
[Serializable] public class HarvestSave { public string cropName; public int count; }

[Serializable]
public class SaveData
{
    public int money;
    public int day;
    public float timeOfDay;
    public int weather;
    public int unlockedPlots;
    public int unlockedChickens;
    public List<PlotSave> plots = new();
    public List<HarvestSave> harvest = new();
    public List<long> chickenFedTicks = new();
    public List<Order> orders = new();
}

public static class SaveSystem
{
    static string FilePath => Path.Combine(Application.persistentDataPath, "save.json");
    public static bool Exists => File.Exists(FilePath);

    public static void Save(SaveData data) =>
        File.WriteAllText(FilePath, JsonUtility.ToJson(data, true));

    public static SaveData Load()
    {
        if (!File.Exists(FilePath)) return null;
        try { return JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath)); }
        catch { return null; } // file hỏng thì coi như chưa có save
    }

    public static void Delete()
    {
        if (File.Exists(FilePath)) File.Delete(FilePath);
    }
}
