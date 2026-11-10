using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ToolSlot : MonoBehaviour
{
    [SerializeField] Image background;
    [SerializeField] Image icon;
    [SerializeField] TMP_Text keyLabel;
    [SerializeField] Button selectButton;
    [SerializeField] Color normalColor = new(0.86f, 0.78f, 0.62f, 1f);
    [SerializeField] Color selectedColor = new(1f, 0.95f, 0.55f, 1f);
    [SerializeField] Color lockedColor = new(0.62f, 0.58f, 0.50f, 0.85f);

    ToolData tool;
    bool owned;

    void Awake()
    {
        if (background == null) background = GetComponent<Image>();
        if (selectButton == null) selectButton = GetComponent<Button>();
        if (selectButton != null)
            selectButton.onClick.AddListener(OnSelect);
    }

    public void Bind(ToolData toolData, int hotkeyIndex, bool selected, bool isOwned)
    {
        tool = toolData;
        owned = isOwned;

        if (keyLabel != null)
        {
            keyLabel.textWrappingMode = TextWrappingModes.NoWrap;
            keyLabel.overflowMode = TextOverflowModes.Overflow;
            if (!owned && tool != null)
                keyLabel.text = $"{tool.price}$";
            else
                keyLabel.text = hotkeyIndex > 0 ? hotkeyIndex.ToString() : "";
        }

        RefreshIcon();
        SetSelected(selected);
    }

    public void RefreshIcon()
    {
        if (icon == null) return;

        Sprite sprite = null;
        if (tool != null)
        {
            if (owned && tool.type == ToolType.Seed
                && GameManager.Instance != null
                && GameManager.Instance.SelectedCrop != null)
                sprite = GameManager.Instance.SelectedCrop.icon;
            else
                sprite = tool.icon;
        }

        icon.enabled = sprite != null;
        icon.sprite = sprite;
        icon.preserveAspect = true;
        icon.color = owned ? Color.white : new Color(1f, 1f, 1f, 0.28f);
    }

    public void SetSelected(bool selected)
    {
        if (background == null) return;
        if (!owned)
            background.color = lockedColor;
        else
            background.color = selected ? selectedColor : normalColor;
    }

    void OnSelect()
    {
        if (tool == null || GameManager.Instance == null) return;

        if (!GameManager.Instance.OwnsTool(tool))
        {
            GameManager.Instance.TryBuyTool(tool);
            return;
        }

        GameManager.Instance.SelectTool(tool);
        AudioManager.Instance?.Play("click");
    }
}
