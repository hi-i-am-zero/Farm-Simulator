using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ToolSlot : MonoBehaviour
{
    [SerializeField] Image background;
    [SerializeField] Image icon;
    [SerializeField] TMP_Text keyLabel;
    [SerializeField] TMP_Text countLabel;
    [SerializeField] Button selectButton;
    [SerializeField] Color normalColor = new(0.86f, 0.78f, 0.62f, 1f);
    [SerializeField] Color selectedColor = new(1f, 0.95f, 0.55f, 1f);
    [SerializeField] Color emptyColor = new(0.70f, 0.65f, 0.55f, 0.5f);

    int slotIndex = 0;
    InventoryItem currentItem;

    void Awake()
    {
        if (background == null) background = GetComponent<Image>();
        if (selectButton == null) selectButton = GetComponent<Button>();
        if (selectButton != null)
            selectButton.onClick.AddListener(OnSelect);
    }

    TMP_Text EnsureCountText()
    {
        if (countLabel != null) return countLabel;

        var existing = transform.Find("Count");
        if (existing != null)
        {
            countLabel = existing.GetComponent<TMP_Text>();
            if (countLabel != null) return countLabel;
        }

        var go = new GameObject("Count", typeof(RectTransform));
        go.transform.SetParent(transform, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1, 0);
        rt.anchorMax = new Vector2(1, 0);
        rt.pivot = new Vector2(1, 0);
        rt.anchoredPosition = new Vector2(-4, 3);
        rt.sizeDelta = new Vector2(40, 20);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        if (keyLabel != null && keyLabel.font != null)
            tmp.font = keyLabel.font;
        tmp.fontSize = 14f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.BottomRight;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.raycastTarget = false;

        countLabel = tmp;
        return countLabel;
    }

    public void Bind(InventoryItem item, int hotkeyIndex, bool isSelected)
    {
        slotIndex = hotkeyIndex - 1;
        currentItem = item;

        // Phím tắt 1..6 (KHÔNG BAO GIỜ HIỆN GIÁ TIỀN!)
        if (keyLabel != null)
        {
            keyLabel.textWrappingMode = TextWrappingModes.NoWrap;
            keyLabel.overflowMode = TextOverflowModes.Overflow;
            keyLabel.text = hotkeyIndex > 0 ? hotkeyIndex.ToString() : "";
        }

        // Icon vật phẩm
        if (icon != null)
        {
            Sprite sprite = (item != null && !item.IsEmpty) ? item.Icon : null;
            icon.enabled = sprite != null;
            icon.sprite = sprite;
            icon.preserveAspect = true;
            icon.color = Color.white;
        }

        // Số lượng (chỉ hiện khi > 1 hoặc là hạt giống)
        var count = EnsureCountText();
        if (count != null)
        {
            if (item != null && !item.IsEmpty && (item.count > 1 || item.kind == ItemKind.Seed))
            {
                count.gameObject.SetActive(true);
                count.text = $"x{item.count}";
                count.color = item.count > 0 ? new Color(0.12f, 0.28f, 0.12f, 1f) : new Color(0.85f, 0.2f, 0.2f, 1f);
            }
            else
            {
                count.gameObject.SetActive(false);
            }
        }

        // Màu viền và nền
        if (background != null)
        {
            if (isSelected)
                background.color = selectedColor;
            else if (item != null && !item.IsEmpty)
                background.color = normalColor;
            else
                background.color = emptyColor;
        }
    }

    // Tương thích ngược nếu còn script cũ gọi
    public void Bind(ToolData toolData, int hotkeyIndex, bool selected, bool isOwned)
    {
        Bind(InventoryItem.FromTool(toolData), hotkeyIndex, selected);
    }

    public void RefreshIcon()
    {
        if (icon == null) return;
        Sprite sprite = (currentItem != null && !currentItem.IsEmpty) ? currentItem.Icon : null;
        icon.enabled = sprite != null;
        icon.sprite = sprite;
    }

    public void RefreshSeedCount()
    {
        var count = EnsureCountText();
        if (count != null)
        {
            if (currentItem != null && !currentItem.IsEmpty && (currentItem.count > 1 || currentItem.kind == ItemKind.Seed))
            {
                count.gameObject.SetActive(true);
                count.text = $"x{currentItem.count}";
            }
            else
            {
                count.gameObject.SetActive(false);
            }
        }
    }

    void OnSelect()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        bool isInvOpen = InventoryUI.Instance != null && InventoryUI.Instance.IsPanelOpen;

        // CHỈ KHI NÀO TÚI ĐỒ (INVENTORY) THỰC SỰ ĐANG MỞ:
        if (isInvOpen)
        {
            if (InventoryUI.Instance.SelectedItem != null && !InventoryUI.Instance.SelectedItem.IsEmpty)
            {
                // Gán hoặc hoán đổi món đang chọn từ Inventory vào ô này trên Toolbar!
                var itemToAssign = InventoryUI.Instance.SelectedItem;
                gm.AssignItemToHotbar(slotIndex, itemToAssign);
                Toast.Show($"Đã gán {itemToAssign.Name} vào ô {slotIndex + 1}");
                InventoryUI.Instance.ClearSelection();
                AudioManager.Instance?.Play("click");
                return;
            }
            else if (currentItem != null && !currentItem.IsEmpty)
            {
                // Khi đang mở túi đồ và click vào ô có đồ trên Toolbar: Gỡ món này ra!
                string itemName = currentItem.Name;
                gm.ClearHotbarSlot(slotIndex);
                Toast.Show($"Đã gỡ {itemName} khỏi thanh công cụ");
                AudioManager.Instance?.Play("click");
                return;
            }
        }

        // KHI TÚI ĐỒ ĐÓNG (CHƠI BÌNH THƯỜNG):
        // Chọn ô này làm công cụ/vật phẩm đang cầm — TUYỆT ĐỐI KHÔNG GỠ ĐỒ!
        gm.SelectHotbarSlot(slotIndex);
        AudioManager.Instance?.Play("click");
    }
}
