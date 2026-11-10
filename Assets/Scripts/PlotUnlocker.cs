using TMPro;
using UnityEngine;

public class PlotUnlocker : MonoBehaviour
{
    [SerializeField] GameObject[] lockedPlots = new GameObject[0];
    [SerializeField] int baseCost = 100;
    [SerializeField] string itemName = "o dat";
    [SerializeField] TMP_Text buttonLabel;

    public int UnlockedCount { get; private set; }

    int LockedCount => lockedPlots == null ? 0 : lockedPlots.Length;
    int NextCost => baseCost * (UnlockedCount + 1);

    void Start()
    {
        if (LockedCount > 0)
        {
            int on = 0;
            for (int i = 0; i < lockedPlots.Length; i++)
            {
                if (lockedPlots[i] != null && lockedPlots[i].activeSelf) on = i + 1;
                else break;
            }
            if (on > 0) UnlockedCount = on;
        }
        RefreshLabel();
    }

    // Gắn vào nút "Mua"
    public void BuyPlot()
    {
        if (UnlockedCount >= LockedCount) return;
        var target = lockedPlots[UnlockedCount];
        if (target == null)
        {
            Debug.LogWarning($"PlotUnlocker '{name}': lockedPlots[{UnlockedCount}] chua gan object.", this);
            return;
        }
        if (!GameManager.Instance.TrySpend(NextCost)) return;

        target.SetActive(true);
        UnlockedCount++;
        AudioManager.Instance?.Play("click");
        RefreshLabel();
    }

    public void ApplyUnlocked(int count)
    {
        if (lockedPlots == null)
        {
            UnlockedCount = 0;
            RefreshLabel();
            return;
        }

        UnlockedCount = Mathf.Clamp(count, 0, lockedPlots.Length);
        for (int i = 0; i < lockedPlots.Length; i++)
        {
            if (lockedPlots[i] != null)
                lockedPlots[i].SetActive(i < UnlockedCount);
        }
        RefreshLabel();
    }

    void RefreshLabel()
    {
        if (buttonLabel == null) return;
        buttonLabel.text = UnlockedCount >= LockedCount
            ? "Da mo het"
            : $"Mua {itemName} ({NextCost}$)";
    }
}
