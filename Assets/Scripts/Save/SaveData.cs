using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public int slotIndex;
    public string playerName = "战士";
    public float playTimeSeconds;
    public long lastSaveUnix;
    public string currentSceneName = "Main";
    public string spawnPointId = "OutsideHouse";

    public StatBlock stats = StatBlock.CreateDefault();
    public int gold = 100;

    public List<InventoryItemSave> inventory = new List<InventoryItemSave>();
    public int gridWidth = 8;
    public int gridHeight = 6;
    public float maxWeight = 50f;
    public float currentWeight;

    public List<CompanionSave> companions = new List<CompanionSave>();
    public List<string> defeatedEnemyIds = new List<string>();
    public List<string> worldFlags = new List<string>();

    public static SaveData CreateNew(int slotIndex, string playerName = "战士")
    {
        return new SaveData
        {
            slotIndex = slotIndex,
            playerName = playerName,
            lastSaveUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            stats = StatBlock.CreateDefault(),
            gold = 100,
            currentSceneName = "Main",
            spawnPointId = "OutsideHouse"
        };
    }
}
