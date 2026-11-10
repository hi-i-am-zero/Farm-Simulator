using UnityEngine;

/// <summary>
/// Dat tren trigger gan NPC. Bam E/Space de mo cua hang.
/// </summary>
public class NpcShop : MonoBehaviour, IInteractable
{
    [SerializeField] string shopTitle = "Cửa hàng";
    [SerializeField] ShopOffer[] offers = new ShopOffer[0];
    [SerializeField] string greeting = "Bạn muốn mua gì?";

    public string ShopTitle => shopTitle;
    public string Greeting => greeting;
    public ShopOffer[] Offers => offers;

    public void Interact()
    {
        if (ShopUI.Instance == null)
        {
            Toast.Show("Chưa có giao diện cửa hàng");
            return;
        }

        ShopUI.Instance.Open(this);
    }
}
