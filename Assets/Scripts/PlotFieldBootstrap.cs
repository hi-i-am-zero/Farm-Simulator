using UnityEngine;

/// <summary>
/// Dat luoi Plot 3x3 neu scene chua co (load tu Resources/Plot).
/// </summary>
[DefaultExecutionOrder(-50)]
public class PlotFieldBootstrap : MonoBehaviour
{
    [SerializeField] GameObject plotPrefab;
    [SerializeField] Vector2 origin = new(-2.5f, -1.5f);
    [SerializeField] float step = 2f;

    void Awake()
    {
        var existing = FindObjectsByType<Plot>(FindObjectsInactive.Include);
        if (existing != null && existing.Length > 0) return;

        if (plotPrefab == null)
            plotPrefab = Resources.Load<GameObject>("Plot");
        if (plotPrefab == null)
        {
            Debug.LogWarning("[FarmSim] Thieu Resources/Plot prefab — khong dat duoc o dat.");
            return;
        }

        var root = new GameObject("PlotsRoot");
        var locked = new GameObject[3];
        int i = 0;
        for (int y = 0; y < 3; y++)
        for (int x = 0; x < 3; x++)
        {
            var go = Instantiate(plotPrefab, root.transform);
            go.name = $"Plot_{i}";
            go.transform.position = new Vector3(origin.x + x * step, origin.y + y * step, 0f);
            if (i >= 6)
            {
                go.SetActive(false);
                locked[i - 6] = go;
            }
            i++;
        }

        var upgradesGo = GameObject.Find("Upgrades") ?? new GameObject("Upgrades");
        var unlocker = upgradesGo.GetComponent<PlotUnlocker>() ?? upgradesGo.AddComponent<PlotUnlocker>();
        unlocker.SetLockedPlots(locked);

        var save = FindAnyObjectByType<SaveManager>();
        // unlocker reference may already be set on SaveManager; runtime lock list is enough for BuyPlot
        Debug.Log("[FarmSim] PlotFieldBootstrap: da dat 9 o dat (6 mo, 3 khoa).");
    }
}
