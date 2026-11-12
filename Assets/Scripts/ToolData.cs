using UnityEngine;

public enum ToolType
{
    Hoe,
    WateringCan,
    Seed,
    Hand,
    Bucket,
    Sickle
}

[CreateAssetMenu(menuName = "Farm/Tool")]
public class ToolData : ScriptableObject
{
    public string displayName = "Tool";
    public ToolType type = ToolType.Hand;
    public Sprite icon;
    public int price = 30;
}
