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
    [SerializeField] TMP_Text nameText;
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
        if (countText != null)
            countText.textWrappingMode = TextWrappingModes.NoWrap;
    }

    TMP_Text EnsureNameText()
    {
        if (nameText != null) return nameText;

        var existing = transform.Find("ItemName");
        if (existing != null)
        {
            nameText = existing.GetComponent<TMP_Text>();
            if (nameText != null) return nameText;
        }

        var go = new GameObject("ItemName", typeof(RectTransform));
        go.transform.SetParent(transform, false);
        go.transform.SetSiblingIndex(Mathf.Min(2, Mathf.Max(0, transform.childCount - 1)));

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(4f, 18f);
        rt.offsetMax = new Vector2(-4f, -20f);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        if (countText != null && countText.font != null)
            tmp.font = countText.font;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 10f;
        tmp.fontSizeMax = 15f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.color = new Color(0.10f, 0.13f, 0.17f, 1f);
        tmp.raycastTarget = false;

        nameText = tmp;
        return nameText;
    }

    public void Bind(CropData cropData, int count, bool selected = false)
    {
        crop = cropData;
        bool hasItem = crop != null && count > 0;

        if (icon != null)
        {
            icon.enabled = false;
            icon.sprite = null;
        }

        var label = EnsureNameText();
        if (label != null)
        {
            label.gameObject.SetActive(hasItem);
            label.text = hasItem ? crop.cropName : "";
        }

        if (countText != null)
        {
            countText.gameObject.SetActive(hasItem);
            countText.text = hasItem ? $"x{count}" : "";
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
