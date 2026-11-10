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
        // Kho dung icon button; an chu dem o goc (tranh so "0" can Pause)
        if (inventoryText != null)
            inventoryText.gameObject.SetActive(false);

        var gm = GameManager.Instance;
        gm.OnMoneyChanged += RefreshMoney;
        gm.OnInventoryChanged += RefreshInventory;
        gm.OnCropSelected += OnCropSelected;
        gm.OnToolSelected += OnToolSelected;

        RefreshMoney(gm.Money);
        RefreshInventory();
        RefreshSelected();
    }

    void OnDestroy()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.OnMoneyChanged -= RefreshMoney;
        GameManager.Instance.OnInventoryChanged -= RefreshInventory;
        GameManager.Instance.OnCropSelected -= OnCropSelected;
        GameManager.Instance.OnToolSelected -= OnToolSelected;
    }

    void OnCropSelected(CropData _) => RefreshSelected();
    void OnToolSelected(ToolData _) => RefreshSelected();

    void RefreshMoney(int money)
    {
        if (moneyText != null)
            moneyText.text = $"Tiền: {money}$";
    }

    void RefreshInventory()
    {
        // Giữ logic phòng khi bật lại inventoryText sau này
        if (inventoryText == null || !inventoryText.gameObject.activeSelf) return;
        int total = 0;
        foreach (var kv in GameManager.Instance.Harvested)
            total += kv.Value;
        inventoryText.text = total == 0 ? "Kho: 0" : $"Kho: {total} món";
    }

    void RefreshSelected()
    {
        if (selectedLabel == null || selectedIcon == null) return;

        var gm = GameManager.Instance;
        var tool = gm.SelectedTool;

        if (tool == null)
        {
            selectedIcon.enabled = false;
            selectedLabel.text = "";
            selectedLabel.enabled = false;
            return;
        }

        selectedLabel.enabled = true;

        if (tool.type == ToolType.Seed)
        {
            var crop = gm.SelectedCrop;
            if (crop == null)
            {
                selectedIcon.enabled = false;
                selectedLabel.text = "";
                selectedLabel.enabled = false;
                return;
            }

            selectedIcon.enabled = crop.icon != null;
            selectedIcon.sprite = crop.icon;
            selectedLabel.text = $"Hạt: {crop.cropName} ({crop.seedPrice}$)";
            return;
        }

        selectedIcon.enabled = tool.icon != null;
        selectedIcon.sprite = tool.icon;
        selectedLabel.text = tool.displayName;
    }
}
