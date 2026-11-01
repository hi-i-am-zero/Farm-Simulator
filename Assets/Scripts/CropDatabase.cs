using UnityEngine;

[CreateAssetMenu(menuName = "Farm/Crop Database")]
public class CropDatabase : ScriptableObject
{
    public CropData[] crops;

    public CropData Find(string cropName)
    {
        foreach (var c in crops)
            if (c != null && c.cropName == cropName) return c;
        return null;
    }
}
