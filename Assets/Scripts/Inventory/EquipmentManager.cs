using System.Collections.Generic;

public class EquipmentManager
{
    readonly Dictionary<EquipSlot, InventoryItemInstance> equipped = new Dictionary<EquipSlot, InventoryItemInstance>();

    public bool TryEquip(InventoryItemInstance item, GridInventory inventory)
    {
        if (item?.Definition == null || item.Definition.equipSlot == EquipSlot.None)
            return false;

        var slot = item.Definition.equipSlot;
        if (equipped.TryGetValue(slot, out var old) && old != null)
            Unequip(slot, inventory);

        inventory.Remove(item);
        equipped[slot] = item;
        GameEventBus.RaiseInventoryChanged();
        return true;
    }

    public bool Unequip(EquipSlot slot, GridInventory inventory)
    {
        if (!equipped.TryGetValue(slot, out var item) || item == null)
            return false;

        if (!inventory.TryAdd(item.Definition, item.StackCount))
            return false;

        equipped.Remove(slot);
        GameEventBus.RaiseInventoryChanged();
        return true;
    }

    public int GetDamageBonus()
    {
        int total = 0;
        foreach (var kv in equipped)
            if (kv.Value?.Definition != null)
                total += kv.Value.Definition.damageBonus;
        return total;
    }

    public int GetDefenseBonus()
    {
        int total = 0;
        foreach (var kv in equipped)
            if (kv.Value?.Definition != null)
                total += kv.Value.Definition.defenseBonus;
        return total;
    }

    public int GetHpBonus()
    {
        int total = 0;
        foreach (var kv in equipped)
            if (kv.Value?.Definition != null)
                total += kv.Value.Definition.hpBonus;
        return total;
    }
}
