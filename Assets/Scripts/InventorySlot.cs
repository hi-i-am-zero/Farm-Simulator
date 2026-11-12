using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour
{
    [SerializeField] Image background;
    [SerializeField] Image icon;
    [SerializeField] TMP_Text countText;
    [SerializeField] TMP_Text badgeText;
    [SerializeField] Button selectButton;
    [SerializeField] Button sellOneButton;
    [SerializeField] Color emptyColor = new(0.84f, 0.79f, 0.70f, 0.45f);
    [SerializeField] Color filledColor = new(0.96f, 0.92f, 0.84f, 1f);
    [SerializeField] Color selectedColor = new(1.0f, 0.88f, 0.35f, 1f);

    InventoryItem currentItem;

    void Awake()
    {
        if (background == null) background = GetComponent<Image>();
        if (selectButton == null) selectButton = GetComponent<Button>();
        if (selectButton != null)
            selectButton.onClick.AddListener(OnSelect);
        if (sellOneButton != null)
            sellOneButton.gameObject.SetActive(false);
    }

    public void Bind(InventoryItem item, bool isSelected = false)
    {
        currentItem = item;
        bool hasItem = item != null && !item.IsEmpty;

        // 1. Icon vật phẩm ở chính giữa ô (hoàn toàn không có chữ đè lên!)
        if (icon != null)
        {
            Sprite sprite = hasItem ? item.Icon : null;
            icon.enabled = sprite != null;
            icon.sprite = sprite;
            icon.preserveAspect = true;
            icon.color = Color.white;
        }

        // 2. Số lượng ở góc dưới phải (x{count})
        if (countText != null)
        {
            bool showCount = hasItem && (item.count > 1 || item.kind == ItemKind.Seed);
            var parentGo = countText.transform.parent != null && countText.transform.parent != transform
                ? countText.transform.parent.gameObject
                : countText.gameObject;

            parentGo.SetActive(showCount);
            if (showCount)
            {
                countText.text = $"x{item.count}";
            }
        }

        // 3. Huy hiệu Hotbar ở góc trên trái ([1], [2]...)
        if (badgeText != null)
        {
            int slotIdx = hasItem && GameManager.Instance != null ? GameManager.Instance.GetHotbarSlotOf(item) : -1;
            var parentGo = badgeText.transform.parent != null && badgeText.transform.parent != transform
                ? badgeText.transform.parent.gameObject
                : badgeText.gameObject;

            parentGo.SetActive(slotIdx >= 0);
            if (slotIdx >= 0)
            {
                badgeText.text = $"{slotIdx + 1}";
            }
        }

        // 4. Ẩn nút bán cũ trên slot nếu còn sót lại
        if (sellOneButton != null)
            sellOneButton.gameObject.SetActive(false);

        // Ẩn nhãn tên cũ nếu có sinh ra trước đây
        var oldName = transform.Find("ItemName");
        if (oldName != null) oldName.gameObject.SetActive(false);

        // 5. Màu sắc viền / nền
        if (background != null)
        {
            if (isSelected && hasItem)
                background.color = selectedColor;
            else if (hasItem)
                background.color = filledColor;
            else
                background.color = emptyColor;
        }

        if (selectButton != null)
            selectButton.interactable = hasItem;
    }

    public void Bind(CropData cropData, int count, bool selected = false)
    {
        Bind(InventoryItem.FromHarvested(cropData, count), selected);
    }

    void OnSelect()
    {
        if (currentItem == null || currentItem.IsEmpty || InventoryUI.Instance == null) return;
        InventoryUI.Instance.SelectItem(currentItem);
        AudioManager.Instance?.Play("click");
    }
}
