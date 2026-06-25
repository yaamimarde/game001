using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Game/Item Database")]
public class ItemDatabase : ScriptableObject
{
    public List<ItemDefinition> items = new List<ItemDefinition>();

    Dictionary<string, ItemDefinition> lookup;

    public ItemDefinition Get(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return null;
        BuildLookup();
        lookup.TryGetValue(itemId, out ItemDefinition def);
        return def;
    }

    void BuildLookup()
    {
        if (lookup != null) return;
        lookup = new Dictionary<string, ItemDefinition>();
        foreach (var item in items)
        {
            if (item != null && !string.IsNullOrEmpty(item.itemId))
                lookup[item.itemId] = item;
        }
    }

    void OnEnable() => lookup = null;
}
