using TMPro;
using UnityEngine;

public class PlotUnlocker : MonoBehaviour
{
    [SerializeField] GameObject[] lockedPlots;
    [SerializeField] int baseCost = 100;
    [SerializeField] string itemName = "ô đất";
    [SerializeField] TMP_Text buttonLabel;

    public int UnlockedCount { get; private set; }
    int NextCost => baseCost * (UnlockedCount + 1);

    void Start() => RefreshLabel();

    // Gắn vào nút "Mua"
    public void BuyPlot()
    {
        if (UnlockedCount >= lockedPlots.Length) return;
        if (!GameManager.Instance.TrySpend(NextCost)) return;

        lockedPlots[UnlockedCount].SetActive(true);
        UnlockedCount++;
        AudioManager.Instance?.Play("click");
        RefreshLabel();
    }

    public void ApplyUnlocked(int count)
    {
        UnlockedCount = Mathf.Clamp(count, 0, lockedPlots.Length);
        for (int i = 0; i < lockedPlots.Length; i++)
            lockedPlots[i].SetActive(i < UnlockedCount);
        RefreshLabel();
    }

    void RefreshLabel()
    {
        if (buttonLabel == null) return;
        buttonLabel.text = UnlockedCount >= lockedPlots.Length
            ? "Đã mở hết"
            : $"Mua {itemName} ({NextCost}$)";
    }
}
