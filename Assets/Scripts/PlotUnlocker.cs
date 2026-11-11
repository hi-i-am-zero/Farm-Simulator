using TMPro;
using UnityEngine;

public class PlotUnlocker : MonoBehaviour
{
    [SerializeField] GameObject[] lockedPlots = new GameObject[0];
    [SerializeField] int baseCost = 100;
    [SerializeField] int stepCost = 50;
    [SerializeField] string itemName = "ô đất";
    [SerializeField] TMP_Text buttonLabel;

    public int UnlockedCount { get; private set; }
    public string ItemName => itemName;
    public int BaseCost => baseCost;
    public int StepCost => stepCost;
    public int LockedCount => lockedPlots == null ? 0 : lockedPlots.Length;
    public int NextCost => GetCostForIndex(UnlockedCount);
    public bool IsSoldOut => UnlockedCount >= LockedCount;

    public int GetCostForIndex(int index) => baseCost + index * stepCost;

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
        if (lockedPlots == null || lockedPlots.Length == 0)
        {
            Debug.LogWarning($"PlotUnlocker '{name}': lockedPlots chưa được gán.", this);
            Toast.Show($"Chưa cấu hình {itemName} để mua!");
            return false;
        }

        if (IsSoldOut)
        {
            Toast.Show($"Đã bán hết {itemName}!");
            return false;
        }

        var gm = GameManager.Instance;
        if (gm == null) return false;

        int index = Mathf.Clamp(UnlockedCount, 0, lockedPlots.Length - 1);
        var target = lockedPlots[index];
        if (target == null)
        {
            Toast.Show($"Lỗi dữ liệu {itemName} #{index + 1}!");
            return false;
        }

        int cost = NextCost;
        if (!gm.TrySpend(cost))
        {
            Toast.Show($"Không đủ tiền! Cần {cost}$ (đang có {gm.Money}$)");
            return false;
        }

        target.SetActive(true);
        UnlockedCount++;
        Toast.Show($"Đã mua {itemName} #{UnlockedCount} (-{cost}$)");
        if (AudioManager.Instance != null) AudioManager.Instance.Play("click");
        RefreshLabel();
        return true;
    }

    public void SetLockedPlots(GameObject[] plots)
    {
        lockedPlots = plots ?? new GameObject[0];
        UnlockedCount = 0;
        RefreshLabel();
    }

    public void Configure(GameObject[] plots, int newBaseCost, int newStepCost, string newItemName)
    {
        lockedPlots = plots ?? new GameObject[0];
        baseCost = newBaseCost;
        stepCost = newStepCost;
        itemName = newItemName;
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
