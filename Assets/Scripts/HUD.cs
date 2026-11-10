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
        if (inventoryText == null) return;
        int total = 0;
        foreach (var kv in GameManager.Instance.Harvested)
            total += kv.Value;
        inventoryText.text = total == 0 ? "Kho: 0 (I)" : $"Kho: {total} mon (I)";
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
