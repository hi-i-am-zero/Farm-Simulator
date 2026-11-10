using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopUI : MonoBehaviour
{
    public static ShopUI Instance { get; private set; }
    public static bool IsOpen { get; private set; }

    [SerializeField] GameObject panel;
    [SerializeField] TMP_Text titleText;
    [SerializeField] TMP_Text greetingText;
    [SerializeField] Transform offerRoot;
    [SerializeField] Button offerButtonPrefab;
    [SerializeField] Button closeButton;

    NpcShop current;
    readonly List<GameObject> spawned = new();

    void Awake()
    {
        Instance = this;
        if (panel != null) panel.SetActive(false);
        IsOpen = false;
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (!IsOpen) return;
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Q))
            Close();
    }

    public void Open(NpcShop shop)
    {
        if (shop == null || panel == null) return;
        current = shop;
        IsOpen = true;
        panel.SetActive(true);

        if (titleText != null) titleText.text = shop.ShopTitle;
        if (greetingText != null) greetingText.text = shop.Greeting;

        RebuildOffers();
        AudioManager.Instance?.Play("click");
    }

    public void Close()
    {
        IsOpen = false;
        current = null;
        if (panel != null) panel.SetActive(false);
        ClearOffers();
    }

    void RebuildOffers()
    {
        ClearOffers();
        if (current == null || offerRoot == null || offerButtonPrefab == null) return;

        foreach (var offer in current.Offers)
        {
            if (offer == null) continue;
            var btn = Instantiate(offerButtonPrefab, offerRoot);
            btn.gameObject.SetActive(true);
            var label = btn.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = BuildLabel(offer);

            var captured = offer;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnBuy(captured));
            spawned.Add(btn.gameObject);
        }
    }

    static string BuildLabel(ShopOffer offer)
    {
        if (offer.kind == ShopOfferKind.Seed && offer.seedCrop != null)
        {
            var n = GameManager.Instance != null ? GameManager.Instance.SeedCount(offer.seedCrop) : 0;
            return $"Mua hạt {offer.seedCrop.cropName} ({offer.seedCrop.seedPrice}$)  [túi:{n}]";
        }

        if (offer.kind == ShopOfferKind.Animal && offer.animalShop != null)
        {
            if (offer.animalShop.IsSoldOut)
                return $"Hết {offer.animalShop.ItemName}";
            return $"Mua {offer.animalShop.ItemName} ({offer.animalShop.NextCost}$)";
        }

        return string.IsNullOrEmpty(offer.label) ? "Mua" : offer.label;
    }

    void OnBuy(ShopOffer offer)
    {
        if (offer == null || GameManager.Instance == null) return;

        if (offer.kind == ShopOfferKind.Seed)
        {
            if (offer.seedCrop == null) return;
            if (GameManager.Instance.TryBuySeed(offer.seedCrop))
                RebuildOffers();
            return;
        }

        if (offer.animalShop == null) return;
        if (offer.animalShop.TryBuy())
            RebuildOffers();
    }

    void ClearOffers()
    {
        foreach (var go in spawned)
            if (go != null) Destroy(go);
        spawned.Clear();
    }
}
