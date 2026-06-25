using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ShopStockEntry
{
    public ItemDefinition item;
    public int stock = 99;
    public float priceMultiplier = 1f;
}

[CreateAssetMenu(fileName = "ShopDefinition", menuName = "Game/Shop Definition")]
public class ShopDefinition : ScriptableObject
{
    public string shopId;
    public string shopName;
    public float townPriceModifier = 1f;
    public List<ShopStockEntry> stock = new List<ShopStockEntry>();
    public List<ItemType> buysCategories = new List<ItemType>();
    public float buybackRate = 0.5f;
}

public class TradeSystem
{
    readonly GameSession session;

    public TradeSystem(GameSession session)
    {
        this.session = session;
    }

    public int GetBuyPrice(ItemDefinition item, ShopDefinition shop)
    {
        if (item == null) return 0;
        float mod = shop != null ? shop.townPriceModifier : 1f;
        return Mathf.Max(1, Mathf.RoundToInt(item.buyPrice * mod));
    }

    public int GetSellPrice(ItemDefinition item, ShopDefinition shop)
    {
        if (item == null) return 0;
        float rate = shop != null ? shop.buybackRate : 0.5f;
        float mod = shop != null ? shop.townPriceModifier : 1f;
        return Mathf.Max(1, Mathf.RoundToInt(item.sellPrice * rate * mod));
    }

    public bool TryBuyFromShop(ItemDefinition item, ShopDefinition shop)
    {
        if (item == null || session?.Inventory == null) return false;

        int price = GetBuyPrice(item, shop);
        if (session.Gold < price) return false;
        if (!session.Inventory.TryAdd(item, 1)) return false;

        session.Gold -= price;
        GameEventBus.RaiseTradeCompleted();
        return true;
    }

    public bool TrySellToShop(InventoryItemInstance item, ShopDefinition shop)
    {
        if (item?.Definition == null || session?.Inventory == null) return false;
        if (shop != null && shop.buysCategories.Count > 0 &&
            !shop.buysCategories.Contains(item.Definition.itemType))
            return false;

        int price = GetSellPrice(item.Definition, shop);
        session.Gold += price;
        session.Inventory.Remove(item);
        GameEventBus.RaiseTradeCompleted();
        return true;
    }
}
