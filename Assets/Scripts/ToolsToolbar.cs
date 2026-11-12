using System.Collections.Generic;
using UnityEngine;

public class ToolsToolbar : MonoBehaviour
{
    public static ToolsToolbar Instance { get; private set; }

    [SerializeField] Transform content;
    [SerializeField] ToolSlot slotPrefab;
    [SerializeField] ToolData[] tools;

    public ToolData[] Tools => tools;
    readonly List<ToolSlot> slots = new();

    void Awake()
    {
        Instance = this;

        // Tự động mở rộng thanh Toolbar để đủ chỗ cho 6 ô (phím 1-6)
        var rt = GetComponent<RectTransform>();
        if (rt != null && rt.sizeDelta.x < 500f)
            rt.sizeDelta = new Vector2(510f, 96f);
    }

    void Start()
    {
        var gm = GameManager.Instance;
        if (gm != null)
        {
            gm.InitializeDefaultHotbarAndTools();
            gm.OnHotbarChanged += RefreshSlots;
        }

        BuildSlots();
        RefreshSlots();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnHotbarChanged -= RefreshSlots;
        }
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;

        var gm = GameManager.Instance;
        if (gm == null) return;

        // Phím 1 đến 6 để chọn nhanh ô tương ứng trên thanh công cụ
        for (int i = 0; i < GameManager.HotbarSlotCount; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i))
            {
                gm.SelectHotbarSlot(i);
            }
        }

        // Cuộn chuột giữa để đổi nhanh qua lại giữa các ô
        float scroll = Input.GetAxisRaw("Mouse ScrollWheel");
        if (scroll > 0.05f)
        {
            int prev = (gm.ActiveHotbarIndex - 1 + GameManager.HotbarSlotCount) % GameManager.HotbarSlotCount;
            gm.SelectHotbarSlot(prev);
        }
        else if (scroll < -0.05f)
        {
            int next = (gm.ActiveHotbarIndex + 1) % GameManager.HotbarSlotCount;
            gm.SelectHotbarSlot(next);
        }
    }

    void BuildSlots()
    {
        if (content == null) return;

        if (slotPrefab == null)
            slotPrefab = Resources.Load<ToolSlot>("ToolSlot");

        foreach (var s in slots)
            if (s != null) Destroy(s.gameObject);
        slots.Clear();

        var gm = GameManager.Instance;
        int activeIdx = gm != null ? gm.ActiveHotbarIndex : 0;

        for (int i = 0; i < GameManager.HotbarSlotCount; i++)
        {
            var slot = Instantiate(slotPrefab, content);
            slot.gameObject.SetActive(true);
            var item = gm != null ? gm.Hotbar[i] : null;
            slot.Bind(item, i + 1, activeIdx == i);
            slots.Add(slot);
        }
    }

    public void RefreshSlots()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        // Đảm bảo số lượng slot luôn đủ
        if (slots.Count != GameManager.HotbarSlotCount)
        {
            BuildSlots();
            return;
        }

        for (int i = 0; i < slots.Count && i < GameManager.HotbarSlotCount; i++)
        {
            if (slots[i] == null) continue;
            slots[i].Bind(gm.Hotbar[i], i + 1, gm.ActiveHotbarIndex == i);
        }
    }
}
