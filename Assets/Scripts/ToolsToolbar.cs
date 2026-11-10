using System.Collections.Generic;
using UnityEngine;

public class ToolsToolbar : MonoBehaviour
{
    [SerializeField] Transform content;
    [SerializeField] ToolSlot slotPrefab;
    [SerializeField] ToolData[] tools;

    readonly List<ToolSlot> slots = new();

    void Start()
    {
        BuildSlots();

        var gm = GameManager.Instance;
        if (gm == null) return;

        gm.OnToolSelected += OnToolSelected;
        gm.OnCropSelected += OnCropSelected;
        gm.OnToolsOwnedChanged += OnToolsOwnedChanged;

        // Tool gia 0 (Tay) tu dong so huu
        gm.GrantFreeTools(tools);
        BuildSlots();
        RefreshSelection(gm.SelectedTool);
    }

    void OnDestroy()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.OnToolSelected -= OnToolSelected;
        GameManager.Instance.OnCropSelected -= OnCropSelected;
        GameManager.Instance.OnToolsOwnedChanged -= OnToolsOwnedChanged;
    }

    void Update()
    {
        if (Time.timeScale == 0f || tools == null) return;

        var gm = GameManager.Instance;
        if (gm == null) return;

        for (int i = 0; i < tools.Length && i < 9; i++)
        {
            if (!Input.GetKeyDown(KeyCode.Alpha1 + i) && !Input.GetKeyDown(KeyCode.Keypad1 + i))
                continue;

            var tool = tools[i];
            if (tool == null) continue;

            // Phím tắt chỉ chọn tool đã mua (tránh mua nhầm → soft-lock)
            if (gm.OwnsTool(tool))
                gm.SelectTool(tool);
        }
    }

    void BuildSlots()
    {
        if (content == null || slotPrefab == null || tools == null) return;

        foreach (var s in slots)
            if (s != null) Destroy(s.gameObject);
        slots.Clear();

        var gm = GameManager.Instance;
        var selected = gm != null ? gm.SelectedTool : null;

        for (int i = 0; i < tools.Length; i++)
        {
            var slot = Instantiate(slotPrefab, content);
            slot.gameObject.SetActive(true);
            bool owned = gm != null && gm.OwnsTool(tools[i]);
            slot.Bind(tools[i], i + 1, selected == tools[i] && owned, owned);
            slots.Add(slot);
        }
    }

    void OnToolSelected(ToolData tool) => RefreshSelection(tool);

    void OnCropSelected(CropData _)
    {
        foreach (var slot in slots)
            if (slot != null) slot.RefreshIcon();
    }

    void OnToolsOwnedChanged() => BuildSlots();

    void RefreshSelection(ToolData selected)
    {
        var gm = GameManager.Instance;
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] == null) continue;
            bool owned = gm != null && i < tools.Length && gm.OwnsTool(tools[i]);
            bool isSelected = owned && i < tools.Length && tools[i] == selected;
            slots[i].Bind(tools[i], i + 1, isSelected, owned);
        }
    }
}
