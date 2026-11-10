using TMPro;
using UnityEngine;

public class PlotUnlocker : MonoBehaviour
{
    [SerializeField] GameObject[] lockedPlots = new GameObject[0];
    [SerializeField] int baseCost = 100;
    [SerializeField] string itemName = "ô đất";
    [SerializeField] TMP_Text buttonLabel;

    public int UnlockedCount { get; private set; }
    public string ItemName => itemName;
    public int NextCost => baseCost * (UnlockedCount + 1);
    public bool IsSoldOut => UnlockedCount >= LockedCount;

    int LockedCount => lockedPlots == null ? 0 : lockedPlots.Length;

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

    // Gắn vào nút "Mua" / NPC shop
    public void BuyPlot() => TryBuy();

    public bool TryBuy()
    {
        if (IsSoldOut)
        {
            Toast.Show($"Đã bán hết {itemName}");
            return false;
        }

        var target = lockedPlots[UnlockedCount];
        if (target == null)
        {
            Debug.LogWarning($"PlotUnlocker '{name}': lockedPlots[{UnlockedCount}] chua gan object.", this);
            return false;
        }

        if (!GameManager.Instance.TrySpend(NextCost)) return false;

        target.SetActive(true);
        UnlockedCount++;
        Toast.Show($"Đã mua {itemName}!");
        AudioManager.Instance?.Play("click");
        RefreshLabel();
        return true;
    }

    public void SetLockedPlots(GameObject[] plots)
    {
        lockedPlots = plots ?? new GameObject[0];
        UnlockedCount = 0;
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
            ? "Đã mở hết"
            : $"Mua {itemName} ({NextCost}$)";
    }
}
