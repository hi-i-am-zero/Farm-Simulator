using UnityEngine;

[CreateAssetMenu(menuName = "Farm/Crop")]
public class CropData : ScriptableObject
{
    public string cropName = "Cà rốt";
    public int seedPrice = 10;
    public int sellPrice = 25;
    public float growTime = 10f; // giây
    public Sprite icon;
    public Sprite[] growthSprites; // từ mầm -> chín

    [Header("Mùa vụ")]
    public bool hasBestSeason = true; // bán giá cao vào mùa hợp
    public Season bestSeason = Season.Spring;
}
