using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OrderUI : MonoBehaviour
{
    [Serializable]
    public class Row
    {
        public GameObject root;
        public TMP_Text text;
        public Button button;
    }

    [SerializeField] Row[] rows;

    void Start()
    {
        for (int i = 0; i < rows.Length; i++)
        {
            int index = i; // tránh lỗi biến vòng lặp trong lambda
            rows[i].button.onClick.AddListener(() => OrderManager.Instance.Deliver(index));
        }

        OrderManager.Instance.OnOrdersChanged += Refresh;
        GameManager.Instance.OnInventoryChanged += Refresh;
        Refresh();
    }

    void OnDestroy()
    {
        if (OrderManager.Instance != null) OrderManager.Instance.OnOrdersChanged -= Refresh;
        if (GameManager.Instance != null) GameManager.Instance.OnInventoryChanged -= Refresh;
    }

    void Refresh()
    {
        var om = OrderManager.Instance;
        var gm = GameManager.Instance;

        for (int i = 0; i < rows.Length; i++)
        {
            bool has = i < om.Orders.Count;
            rows[i].root.SetActive(has);
            if (!has) continue;

            var order = om.Orders[i];
            var crop = om.Resolve(order);
            int have = gm.Count(crop);

            rows[i].text.text = crop == null
                ? "Đơn không hợp lệ"
                : $"{order.amount} {crop.cropName} → {order.reward}$ (có {have}/{order.amount})";
            rows[i].button.interactable = crop != null && have >= order.amount;
        }
    }
}
