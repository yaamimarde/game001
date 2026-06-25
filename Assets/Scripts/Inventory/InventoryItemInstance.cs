using System;
using UnityEngine;

[Serializable]
public class InventoryItemInstance
{
    public ItemDefinition Definition;
    public string DefinitionId;
    public int StackCount = 1;
    public int GridX;
    public int GridY;
    public bool Rotated;
    public bool InHotbar;
    public int HotbarIndex = -1;

    public int Width => Rotated ? Definition.gridHeight : Definition.gridWidth;
    public int Height => Rotated ? Definition.gridWidth : Definition.gridHeight;
    public float TotalWeight => Definition != null ? Definition.weight * StackCount : 0f;

    public static InventoryItemInstance FromDefinition(ItemDefinition def, int stack = 1)
    {
        return new InventoryItemInstance
        {
            Definition = def,
            DefinitionId = def.itemId,
            StackCount = stack
        };
    }

    public InventoryItemSave ToSave()
    {
        return new InventoryItemSave
        {
            definitionId = DefinitionId,
            stackCount = StackCount,
            gridX = GridX,
            gridY = GridY,
            rotated = Rotated,
            inHotbar = InHotbar,
            hotbarIndex = HotbarIndex
        };
    }

    public static InventoryItemInstance FromSave(InventoryItemSave save, ItemDatabase db)
    {
        var def = db.Get(save.definitionId);
        if (def == null) return null;

        return new InventoryItemInstance
        {
            Definition = def,
            DefinitionId = save.definitionId,
            StackCount = save.stackCount,
            GridX = save.gridX,
            GridY = save.gridY,
            Rotated = save.rotated,
            InHotbar = save.inHotbar,
            HotbarIndex = save.hotbarIndex
        };
    }
}
