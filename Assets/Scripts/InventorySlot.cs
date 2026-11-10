using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour
{
    [SerializeField] Image background;
    [SerializeField] Image icon;
    [SerializeField] TMP_Text countText;
    [SerializeField] Button selectButton;
    [SerializeField] Button sellOneButton;
    [SerializeField] Color emptyColor = new(0.86f, 0.78f, 0.62f, 1f);
    [SerializeField] Color filledColor = new(0.93f, 0.86f, 0.70f, 1f);
    [SerializeField] Color selectedColor = new(1f, 0.95f, 0.55f, 1f);

    CropData crop;

    void Awake()
    {
        if (background == null) background = GetComponent<Image>();
        if (selectButton == null) selectButton = GetComponent<Button>();
        if (selectButton != null)
            selectButton.onClick.AddListener(OnSelect);
        if (sellOneButton != null)
            sellOneButton.onClick.AddListener(OnSellOne);
    }

    public void Bind(CropData cropData, int count, bool selected = false)
    {
        crop = cropData;
        bool hasItem = crop != null && count > 0;

        if (icon != null)
        {
            icon.enabled = hasItem && crop.icon != null;
            icon.sprite = hasItem ? crop.icon : null;
            icon.preserveAspect = true;
        }

        if (countText != null)
        {
            countText.gameObject.SetActive(hasItem);
            countText.text = hasItem ? count.ToString() : "";
        }

        if (sellOneButton != null)
            sellOneButton.gameObject.SetActive(hasItem);

        if (background != null)
        {
            if (selected && hasItem) background.color = selectedColor;
            else background.color = hasItem ? filledColor : emptyColor;
        }

        if (selectButton != null)
            selectButton.interactable = hasItem;
    }

    void OnSelect()
    {
        if (crop == null || GameManager.Instance == null) return;
        GameManager.Instance.SelectCrop(crop);
        AudioManager.Instance?.Play("click");
    }

    void OnSellOne()
    {
        if (crop == null || GameManager.Instance == null) return;
        GameManager.Instance.SellOne(crop);
    }
}
