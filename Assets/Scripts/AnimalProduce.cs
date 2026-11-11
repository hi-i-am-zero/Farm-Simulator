using TMPro;
using UnityEngine;

/// <summary>
/// Manages periodic animal yield production (egg, milk, wool), displays an overhead "!" alert
/// when ready, and allows harvesting once per cycle so unready animals are skipped cleanly.
/// </summary>
public class AnimalProduce : MonoBehaviour, IConditionalInteractable, IGrowable
{
    [SerializeField] CropData productItem;
    [SerializeField] float produceInterval = 30f;
    [SerializeField] float elapsed;

    static readonly Vector3 AlertBaseLocalPos = new(0f, 0.72f, 0f);

    GameObject alertGo;
    Transform alertTransform;
    TextMeshPro alertText;

    public CropData ProductItem => productItem;
    public float ProduceInterval => produceInterval;
    public bool IsReady => productItem != null && elapsed >= produceInterval;
    public bool CanInteract => IsReady;

    /// <summary>
    /// Assigns the product item and production cycle duration for this animal.
    /// </summary>
    public void Configure(CropData item, float interval = 30f)
    {
        productItem = item;
        produceInterval = Mathf.Max(1f, interval);
        elapsed = Mathf.Clamp(elapsed, 0f, produceInterval);
        if (Application.isPlaying && gameObject.activeInHierarchy)
            UpdateAlertVisual();
    }

    void OnEnable()
    {
        if (!Growables.All.Contains(this))
            Growables.All.Add(this);
        if (!Application.isPlaying) return;
        UpdateAlertVisual();
    }

    void OnDisable()
    {
        Growables.All.Remove(this);
        if (alertGo != null)
            alertGo.SetActive(false);
    }

    void Update()
    {
        if (productItem == null) return;

        if (!IsReady)
        {
            elapsed += Time.deltaTime;
            if (elapsed >= produceInterval)
            {
                elapsed = produceInterval;
                UpdateAlertVisual();
            }
            return;
        }

        if (alertTransform != null)
        {
            float bob = Mathf.Sin(Time.time * 5.5f) * 0.08f;
            alertTransform.localPosition = AlertBaseLocalPos + Vector3.up * bob;
        }
    }

    /// <summary>
    /// Advances the production timer (e.g. from weather speed buffs).
    /// </summary>
    public void AddGrowth(float seconds)
    {
        if (productItem == null || IsReady) return;
        float delta = Mathf.Max(0f, seconds);
        if (delta <= 0f) return;

        elapsed = Mathf.Min(produceInterval, elapsed + delta);
        if (IsReady && Application.isPlaying)
            UpdateAlertVisual();
    }

    /// <summary>
    /// Harvests the animal's yield into the player's inventory if ready, then restarts the timer.
    /// </summary>
    public void Interact()
    {
        if (!IsReady || productItem == null) return;

        int bagCount = 0;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddHarvest(productItem);
            bagCount = GameManager.Instance.Count(productItem);
        }

        if (AudioManager.Instance != null)
            AudioManager.Instance.Play("harvest");

        PopEffect.SpawnText($"+1 {productItem.cropName}", transform.position, new Color(1f, 0.92f, 0.2f, 1f));
        Toast.Show($"Thu hoạch +1 {productItem.cropName} (Túi: {bagCount})");

        elapsed = 0f;
        UpdateAlertVisual();
    }

    void EnsureAlert()
    {
        if (!gameObject.activeInHierarchy) return;
        if (alertGo != null && alertText != null) return;

        var existing = transform.Find("ProduceAlert");
        if (existing != null)
        {
            alertGo = existing.gameObject;
            alertTransform = existing;
            alertText = existing.GetComponent<TextMeshPro>();
            if (alertText != null) return;
        }

        alertGo = new GameObject("ProduceAlert");
        alertTransform = alertGo.transform;
        alertTransform.SetParent(transform, false);
        alertTransform.localPosition = AlertBaseLocalPos;
        alertTransform.localScale = Vector3.one;

        alertText = alertGo.AddComponent<TextMeshPro>();
        if (alertText.font == null && TMP_Settings.defaultFontAsset != null)
            alertText.font = TMP_Settings.defaultFontAsset;
        alertText.text = "!";
        alertText.fontSize = 5.4f;
        alertText.fontStyle = FontStyles.Bold;
        alertText.alignment = TextAlignmentOptions.Center;
        alertText.textWrappingMode = TextWrappingModes.NoWrap;
        alertText.color = new Color(1f, 0.86f, 0.1f, 1f);

        // Chỉ chỉnh outline khi Renderer và material của TMP đã được Awake khởi tạo
        var rend = alertGo.GetComponent<Renderer>();
        if (rend != null && rend.sharedMaterial != null)
        {
            alertText.outlineWidth = 0.35f;
            alertText.outlineColor = new Color32(25, 15, 5, 255);
        }

        alertText.sortingLayerID = SortingLayer.NameToID("Player");
        alertText.sortingOrder = 25;

        var rt = alertGo.GetComponent<RectTransform>();
        if (rt != null) rt.sizeDelta = new Vector2(1.4f, 1.4f);
    }

    void UpdateAlertVisual()
    {
        if (!Application.isPlaying || !gameObject.activeInHierarchy) return;

        bool ready = IsReady;
        if (!ready)
        {
            if (alertGo != null && alertGo.activeSelf)
                alertGo.SetActive(false);
            return;
        }

        EnsureAlert();
        if (alertGo == null) return;

        if (!alertGo.activeSelf)
            alertGo.SetActive(true);

        if (alertTransform != null)
            alertTransform.localPosition = AlertBaseLocalPos;
    }
}
