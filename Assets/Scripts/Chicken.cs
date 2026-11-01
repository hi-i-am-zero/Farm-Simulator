using System;
using UnityEngine;

public class Chicken : MonoBehaviour, IInteractable, IGrowable
{
    [SerializeField] CropData eggItem;
    [SerializeField] int feedCost = 5;
    [SerializeField] float eggTime = 30f;
    [SerializeField] SpriteRenderer body;
    [SerializeField] GameObject eggIcon;

    public long FedTicks { get; private set; } // 0 = chưa cho ăn
    public bool IsFed => FedTicks != 0;

    double ElapsedSeconds =>
        (DateTime.UtcNow - new DateTime(FedTicks, DateTimeKind.Utc)).TotalSeconds;

    public bool IsReady => IsFed && ElapsedSeconds >= eggTime;

    void OnEnable() => Growables.All.Add(this);
    void OnDisable() => Growables.All.Remove(this);

    public void Interact()
    {
        if (!IsFed) Feed();
        else if (IsReady) Collect();
        else
        {
            float left = Mathf.Max(0f, eggTime - (float)ElapsedSeconds);
            Toast.Show($"Gà sẽ đẻ trứng sau {Mathf.Ceil(left)} giây");
        }
    }

    void Feed()
    {
        if (!GameManager.Instance.TrySpend(feedCost)) return;
        SetFed(DateTime.UtcNow.Ticks);
        AudioManager.Instance?.Play("plant");
    }

    void Collect()
    {
        if (eggItem == null) { Debug.LogWarning("Chicken: chưa kéo Egg Item", this); return; }
        GameManager.Instance.AddHarvest(eggItem);
        AudioManager.Instance?.Play("harvest");
        PopEffect.Spawn(eggItem.icon, transform.position);
        SetFed(0);
    }

    public void SetFed(long ticks)
    {
        FedTicks = ticks;
        UpdateVisual();
    }

    public void AddGrowth(float seconds)
    {
        if (!IsFed) return;
        FedTicks -= (long)((double)seconds * TimeSpan.TicksPerSecond);
    }

    void Update() => UpdateVisual();

    void UpdateVisual()
    {
        if (eggIcon != null) eggIcon.SetActive(IsReady);
        if (body != null) body.color = IsFed ? Color.white : new Color(0.75f, 0.75f, 0.75f, 1f);
    }
}
