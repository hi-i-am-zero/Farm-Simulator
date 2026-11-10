using System;
using UnityEngine;

public enum ShopOfferKind
{
    Animal,
    Seed
}

[Serializable]
public class ShopOffer
{
    public string label = "Mua";
    public ShopOfferKind kind = ShopOfferKind.Animal;
    public PlotUnlocker animalShop;
    public CropData seedCrop;
}
