using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 当 Resources 资产尚未创建时，提供运行时默认内容。
/// </summary>
public static class DefaultGameContent
{
    static ItemDatabase cachedDb;
    static RecruitDatabase cachedRecruitDb;
    static StatProgressionConfig cachedStatConfig;

    public static ItemDatabase GetItemDatabase()
    {
        if (cachedDb != null) return cachedDb;
        var loaded = Resources.Load<ItemDatabase>("Game/ItemDatabase");
        if (loaded != null) return cachedDb = loaded;

        cachedDb = ScriptableObject.CreateInstance<ItemDatabase>();
        cachedDb.items = new List<ItemDefinition>
        {
            CreateItem("sword_iron", "铁剑", 2, 1, ItemType.Weapon, EquipSlot.Weapon, 5, 0, 50, 25),
            CreateItem("potion_hp", "生命药水", 1, 1, ItemType.Consumable, EquipSlot.None, 0, 0, 20, 10),
            CreateItem("ore_copper", "铜矿", 1, 1, ItemType.Material, EquipSlot.None, 0, 0, 15, 8)
        };
        return cachedDb;
    }

    public static RecruitDatabase GetRecruitDatabase()
    {
        if (cachedRecruitDb != null) return cachedRecruitDb;
        var loaded = Resources.Load<RecruitDatabase>("Game/RecruitDatabase");
        if (loaded != null) return cachedRecruitDb = loaded;

        cachedRecruitDb = ScriptableObject.CreateInstance<RecruitDatabase>();
        var template = ScriptableObject.CreateInstance<RecruitTemplate>();
        template.templateId = "guard_recruit";
        template.displayName = "流浪卫兵";
        template.hireCost = 200;
        template.dailyWage = 20;
        template.baseStats = StatBlock.CreateDefault();
        cachedRecruitDb.templates = new List<RecruitTemplate> { template };
        return cachedRecruitDb;
    }

    public static StatProgressionConfig GetStatConfig()
    {
        if (cachedStatConfig != null) return cachedStatConfig;
        var loaded = Resources.Load<StatProgressionConfig>("Game/StatProgressionConfig");
        if (loaded != null) return cachedStatConfig = loaded;

        cachedStatConfig = ScriptableObject.CreateInstance<StatProgressionConfig>();
        return cachedStatConfig;
    }

    public static ShopDefinition CreateGeneralStore()
    {
        var shop = ScriptableObject.CreateInstance<ShopDefinition>();
        shop.shopId = "general_store";
        shop.shopName = "杂货铺";
        shop.townPriceModifier = 1f;
        shop.buybackRate = 0.5f;
        shop.buysCategories = new List<ItemType> { ItemType.Material, ItemType.Consumable };

        var db = GetItemDatabase();
        foreach (var item in db.items)
            shop.stock.Add(new ShopStockEntry { item = item, stock = 99, priceMultiplier = 1f });
        return shop;
    }

    static ItemDefinition CreateItem(string id, string name, int gw, int gh,
        ItemType type, EquipSlot slot, int dmg, int def, int buy, int sell)
    {
        var item = ScriptableObject.CreateInstance<ItemDefinition>();
        item.itemId = id;
        item.displayName = name;
        item.gridWidth = gw;
        item.gridHeight = gh;
        item.itemType = type;
        item.equipSlot = slot;
        item.damageBonus = dmg;
        item.defenseBonus = def;
        item.buyPrice = buy;
        item.sellPrice = sell;
        item.weight = 1f;
        return item;
    }
}
