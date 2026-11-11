using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] Transform content;
    [SerializeField] InventorySlot slotPrefab;
    [SerializeField] TMP_Text emptyLabel;
    [SerializeField] int slotCount = 24;

    readonly List<InventorySlot> spawned = new();

    void Start()
    {
        if (content != null && content.TryGetComponent(out GridLayoutGroup grid) && grid.cellSize.x < 92f)
            grid.cellSize = new Vector2(96f, 82f);

        if (GameManager.Instance == null) return;
        GameManager.Instance.OnInventoryChanged += Refresh;
        GameManager.Instance.OnCropSelected += OnCropSelected;
        Refresh();
    }

    void OnDestroy()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.OnInventoryChanged -= Refresh;
        GameManager.Instance.OnCropSelected -= OnCropSelected;
    }

    void OnCropSelected(CropData _) => Refresh();

    public void Refresh()
    {
        if (content == null || slotPrefab == null || GameManager.Instance == null) return;

        foreach (var s in spawned)
            if (s != null) Destroy(s.gameObject);
        spawned.Clear();

        var items = GameManager.Instance.Harvested
            .Where(kv => kv.Key != null && kv.Value > 0)
            .ToList();

        if (emptyLabel != null)
            emptyLabel.gameObject.SetActive(false);

        var selected = GameManager.Instance.SelectedCrop;
        int total = Mathf.Max(slotCount, items.Count);

        for (int i = 0; i < total; i++)
        {
            var slot = Instantiate(slotPrefab, content);
            slot.gameObject.SetActive(true);
            if (i < items.Count)
            {
                var kv = items[i];
                slot.Bind(kv.Key, kv.Value, selected == kv.Key);
            }
            else
            {
                slot.Bind(null, 0);
            }
            spawned.Add(slot);
        }
    }
}
