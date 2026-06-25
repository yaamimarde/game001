using System;

[Serializable]
public class InventoryItemSave
{
    public string definitionId;
    public int stackCount = 1;
    public int gridX;
    public int gridY;
    public bool rotated;
    public bool inHotbar;
    public int hotbarIndex = -1;
}
