using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUD : MonoBehaviour
{
    [SerializeField] TMP_Text moneyText;
    [SerializeField] TMP_Text inventoryText;
    [SerializeField] TMP_Text selectedLabel;
    [SerializeField] Image selectedIcon;

    void Start()
    {
        var gm = GameManager.Instance;
        gm.OnMoneyChanged += RefreshMoney;
        gm.OnInventoryChanged += RefreshInventory;
        gm.OnCropSelected += RefreshSelected;

        RefreshMoney(gm.Money);
        RefreshInventory();
        RefreshSelected(gm.SelectedCrop);
    }

    void OnDestroy()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.OnMoneyChanged -= RefreshMoney;
        GameManager.Instance.OnInventoryChanged -= RefreshInventory;
        GameManager.Instance.OnCropSelected -= RefreshSelected;
    }

    void RefreshMoney(int money) => moneyText.text = $"Tiền: {money}$";

    void RefreshInventory()
    {
        var sb = new StringBuilder();
        foreach (var kv in GameManager.Instance.Harvested)
            sb.AppendLine($"{kv.Key.cropName} x{kv.Value}");
        inventoryText.text = sb.Length == 0 ? "Kho trống" : sb.ToString();
    }

    void RefreshSelected(CropData crop)
    {
        if (crop == null)
        {
            selectedIcon.enabled = false;
            selectedLabel.text = "Chưa chọn";
            return;
        }
        selectedIcon.enabled = crop.icon != null;
        selectedIcon.sprite = crop.icon;
        selectedLabel.text = $"{crop.cropName} ({crop.seedPrice}$)";
    }
}
