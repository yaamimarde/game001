using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 当前局运行时状态，由 GameManager 持有。
/// </summary>
public class GameSession
{
    public SaveData Save { get; private set; }
    public bool IsActive { get; private set; }
    public float SessionStartTime { get; private set; }

    public GridInventory Inventory { get; private set; }
    public EquipmentManager Equipment { get; private set; }
    public StatProgressionSystem Progression { get; private set; }
    public TradeSystem Trade { get; private set; }
    public CompanionManager Companions { get; private set; }

    public int Gold
    {
        get => Save.gold;
        set
        {
            Save.gold = Mathf.Max(0, value);
            GameEventBus.RaiseGoldChanged();
        }
    }

    public void StartNew(SaveData save)
    {
        Save = save ?? SaveData.CreateNew(0);
        IsActive = true;
        SessionStartTime = Time.realtimeSinceStartup;
        InitSystems();
        GameEventBus.RaiseGameLoaded();
    }

    public void LoadFrom(SaveData save)
    {
        Save = save;
        IsActive = true;
        SessionStartTime = Time.realtimeSinceStartup;
        InitSystems();
        GameEventBus.RaiseGameLoaded();
    }

    void InitSystems()
    {
        Inventory = new GridInventory(Save.gridWidth, Save.gridHeight, Save.maxWeight);
        Inventory.LoadFromSave(Save.inventory);
        if (Save.inventory == null || Save.inventory.Count == 0)
            GiveStarterItems();

        Equipment = new EquipmentManager();
        Progression = new StatProgressionSystem(Save.stats);
        Trade = new TradeSystem(this);
        Companions = new CompanionManager(this);
        Companions.LoadFromSave(Save.companions);
    }

    void GiveStarterItems()
    {
        var db = DefaultGameContent.GetItemDatabase();
        if (db.items.Count > 0) Inventory.TryAdd(db.items[0], 1);
        if (db.items.Count > 1) Inventory.TryAdd(db.items[1], 2);
    }

    public void TickPlayTime()
    {
        if (!IsActive || Save == null) return;
        Save.playTimeSeconds += Time.unscaledDeltaTime;
    }

    public void SyncToSave()
    {
        if (Save == null) return;
        Save.lastSaveUnix = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        Save.inventory = Inventory != null ? Inventory.ToSaveList() : new List<InventoryItemSave>();
        Save.currentWeight = Inventory != null ? Inventory.CurrentWeight : 0f;
        Save.stats = Progression != null ? Progression.Stats.Clone() : Save.stats;
        Save.companions = Companions != null ? Companions.ToSaveList() : new List<CompanionSave>();
    }

    public void End()
    {
        IsActive = false;
        Companions?.ClearRuntime();
    }

    public bool HasWorldFlag(string flag)
    {
        return Save != null && Save.worldFlags != null && Save.worldFlags.Contains(flag);
    }

    public void SetWorldFlag(string flag)
    {
        if (Save == null) return;
        if (Save.worldFlags == null) Save.worldFlags = new List<string>();
        if (!Save.worldFlags.Contains(flag))
            Save.worldFlags.Add(flag);
    }
}
