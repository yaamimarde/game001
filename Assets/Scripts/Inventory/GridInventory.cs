using System.Collections.Generic;
using UnityEngine;

public class GridInventory
{
    public int Width { get; }
    public int Height { get; }
    public float MaxWeight { get; }
    public float CurrentWeight { get; private set; }

    readonly InventoryItemInstance[,] cells;
    readonly List<InventoryItemInstance> allItems = new List<InventoryItemInstance>();
    readonly ItemDatabase database;

    public IReadOnlyList<InventoryItemInstance> Items => allItems;

    public GridInventory(int width, int height, float maxWeight)
    {
        Width = width;
        Height = height;
        MaxWeight = maxWeight;
        cells = new InventoryItemInstance[width, height];
        database = DefaultGameContent.GetItemDatabase();
    }

    public void LoadFromSave(List<InventoryItemSave> saves)
    {
        Clear();
        if (saves == null || database == null) return;

        foreach (var save in saves)
        {
            var item = InventoryItemInstance.FromSave(save, database);
            if (item == null) continue;
            if (!TryPlace(item, item.GridX, item.GridY, item.Rotated))
                allItems.Add(item);
        }
        RecalcWeight();
    }

    public List<InventoryItemSave> ToSaveList()
    {
        var list = new List<InventoryItemSave>();
        foreach (var item in allItems)
            list.Add(item.ToSave());
        return list;
    }

    public bool CanPlace(InventoryItemInstance item, int x, int y, bool rotated, InventoryItemInstance ignore = null)
    {
        if (item?.Definition == null) return false;

        item.Rotated = rotated;
        int w = item.Width;
        int h = item.Height;

        if (x < 0 || y < 0 || x + w > Width || y + h > Height)
            return false;

        float newWeight = CurrentWeight + item.TotalWeight;
        if (ignore != null) newWeight -= ignore.TotalWeight;
        if (newWeight > MaxWeight) return false;

        for (int ix = x; ix < x + w; ix++)
        {
            for (int iy = y; iy < y + h; iy++)
            {
                var occupant = cells[ix, iy];
                if (occupant != null && occupant != ignore)
                    return false;
            }
        }
        return true;
    }

    public bool TryPlace(InventoryItemInstance item, int x, int y, bool rotated)
    {
        if (!CanPlace(item, x, y, rotated)) return false;

        Remove(item);
        item.GridX = x;
        item.GridY = y;
        item.Rotated = rotated;
        Occupy(item);
        allItems.Add(item);
        RecalcWeight();
        GameEventBus.RaiseInventoryChanged();
        return true;
    }

    public bool TryAdd(ItemDefinition def, int stack = 1)
    {
        var item = InventoryItemInstance.FromDefinition(def, stack);
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (TryPlace(item, x, y, false))
                    return true;
                if (TryPlace(item, x, y, true))
                    return true;
            }
        }
        return false;
    }

    public void Remove(InventoryItemInstance item)
    {
        if (item == null) return;
        ClearCells(item);
        allItems.Remove(item);
        RecalcWeight();
        GameEventBus.RaiseInventoryChanged();
    }

    public bool Rotate(InventoryItemInstance item)
    {
        if (item == null) return false;
        bool newRot = !item.Rotated;
        if (!CanPlace(item, item.GridX, item.GridY, newRot, item)) return false;

        ClearCells(item);
        item.Rotated = newRot;
        Occupy(item);
        GameEventBus.RaiseInventoryChanged();
        return true;
    }

    void Occupy(InventoryItemInstance item)
    {
        for (int ix = item.GridX; ix < item.GridX + item.Width; ix++)
            for (int iy = item.GridY; iy < item.GridY + item.Height; iy++)
                cells[ix, iy] = item;
    }

    void ClearCells(InventoryItemInstance item)
    {
        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
                if (cells[x, y] == item)
                    cells[x, y] = null;
    }

    void Clear()
    {
        allItems.Clear();
        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
                cells[x, y] = null;
        CurrentWeight = 0f;
    }

    void RecalcWeight()
    {
        CurrentWeight = 0f;
        foreach (var item in allItems)
            CurrentWeight += item.TotalWeight;
    }

    public InventoryItemInstance GetAt(int x, int y)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height) return null;
        return cells[x, y];
    }
}
