using UnityEngine;

public enum ItemType
{
    Generic,
    Consumable,
    Weapon,
    Armor,
    Material,
    Quest
}

public enum EquipSlot
{
    None,
    Weapon,
    Armor,
    Accessory
}

[CreateAssetMenu(fileName = "ItemDefinition", menuName = "Game/Item Definition")]
public class ItemDefinition : ScriptableObject
{
    public string itemId;
    public string displayName;
    [TextArea] public string description;
    public Sprite icon;
    public int gridWidth = 1;
    public int gridHeight = 1;
    public float weight = 1f;
    public int maxStack = 1;
    public ItemType itemType = ItemType.Generic;
    public EquipSlot equipSlot = EquipSlot.None;
    public int damageBonus;
    public int defenseBonus;
    public int hpBonus;
    public int buyPrice = 10;
    public int sellPrice = 5;
}
