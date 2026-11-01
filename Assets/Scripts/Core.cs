using System;
using System.Collections.Generic;

public enum Season { Spring, Summer, Autumn, Winter }
public enum Weather { Sunny, Cloudy, Rain }

// Thứ người chơi bấm E vào được: ô đất, gà...
public interface IInteractable { void Interact(); }

// Thứ có đồng hồ tăng trưởng: cây, gà
public interface IGrowable { void AddGrowth(float seconds); }

public static class Growables
{
    public static readonly List<IGrowable> All = new();

    // seconds > 0: lớn nhanh hơn, seconds < 0: chậm lại
    public static void Shift(float seconds)
    {
        for (int i = 0; i < All.Count; i++) All[i].AddGrowth(seconds);
    }
}

[Serializable]
public class Order
{
    public string cropName;
    public int amount;
    public int reward;
}
