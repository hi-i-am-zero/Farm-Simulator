using System;
using System.Collections.Generic;
using UnityEngine;

public enum Season { Spring, Summer, Autumn, Winter }
public enum Weather { Sunny, Cloudy, Rain }

// Thứ người chơi bấm E vào được: ô đất, gà...
public interface IInteractable { void Interact(); }

// Đối tượng tương tác có điều kiện (ví dụ: động vật chỉ thu hoạch khi sẵn sàng, bỏ qua khi chưa tới lượt)
public interface IConditionalInteractable : IInteractable { bool CanInteract { get; } }

// Thứ có đồng hồ tăng trưởng: cây, gà
public interface IGrowable { void AddGrowth(float seconds); }

public static class Growables
{
    public static readonly List<IGrowable> All = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => All.Clear();

    // seconds > 0: lớn nhanh hơn, seconds < 0: chậm lại
    public static void Shift(float seconds)
    {
        for (int i = All.Count - 1; i >= 0; i--)
        {
            var g = All[i];
            if (g == null || (g is UnityEngine.Object uo && uo == null))
            {
                All.RemoveAt(i);
                continue;
            }
            g.AddGrowth(seconds);
        }
    }
}

[Serializable]
public class Order
{
    public string cropName;
    public int amount;
    public int reward;
}
