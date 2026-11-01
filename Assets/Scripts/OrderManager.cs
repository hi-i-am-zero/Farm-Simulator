using System;
using System.Collections.Generic;
using UnityEngine;

public class OrderManager : MonoBehaviour
{
    public static OrderManager Instance { get; private set; }

    [SerializeField] CropDatabase cropDb;
    [SerializeField] int slots = 3;
    [SerializeField] float rewardMultiplier = 1.5f;
    [SerializeField] Vector2Int amountRange = new Vector2Int(2, 6); // 2..5

    public readonly List<Order> Orders = new();
    public event Action OnOrdersChanged;

    void Awake() => Instance = this;
    void Start() => FillMissing();

    public CropData Resolve(Order order) => cropDb.Find(order.cropName);

    public void Restore(List<Order> saved)
    {
        Orders.Clear();
        if (saved != null)
            foreach (var o in saved)
                if (o != null && cropDb.Find(o.cropName) != null && Orders.Count < slots)
                    Orders.Add(o);
        FillMissing();
    }

    void FillMissing()
    {
        while (Orders.Count < slots) Orders.Add(Generate());
        OnOrdersChanged?.Invoke();
    }

    Order Generate()
    {
        var crop = cropDb.crops[UnityEngine.Random.Range(0, cropDb.crops.Length)];
        int amount = UnityEngine.Random.Range(amountRange.x, amountRange.y);
        int reward = Mathf.RoundToInt(crop.sellPrice * amount * rewardMultiplier);
        return new Order { cropName = crop.cropName, amount = amount, reward = reward };
    }

    public void Deliver(int index)
    {
        if (index < 0 || index >= Orders.Count) return;

        var order = Orders[index];
        var crop = Resolve(order);
        var gm = GameManager.Instance;

        if (crop == null)
        {
            Orders[index] = Generate();
            OnOrdersChanged?.Invoke();
            return;
        }

        if (!gm.TryTake(crop, order.amount))
        {
            Toast.Show($"Chưa đủ {crop.cropName} ({gm.Count(crop)}/{order.amount})");
            return;
        }

        gm.AddMoney(order.reward);
        Toast.Show($"Hoàn thành đơn! +{order.reward}$");
        AudioManager.Instance?.Play("sell");

        Orders[index] = Generate();
        OnOrdersChanged?.Invoke();
    }
}
