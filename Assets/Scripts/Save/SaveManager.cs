using System;
using System.IO;
using UnityEngine;

public class SaveManager
{
    const string SaveFolder = "saves";
    const string SlotFileFormat = "slot_{0}.json";

    string SaveDirectory => Path.Combine(Application.persistentDataPath, SaveFolder);

    public SaveManager()
    {
        if (!Directory.Exists(SaveDirectory))
            Directory.CreateDirectory(SaveDirectory);
    }

    string GetPath(int slotIndex) =>
        Path.Combine(SaveDirectory, string.Format(SlotFileFormat, slotIndex));

    public bool SlotExists(int slotIndex)
    {
        return File.Exists(GetPath(slotIndex));
    }

    public bool TryLoad(int slotIndex, out SaveData data)
    {
        data = null;
        string path = GetPath(slotIndex);
        if (!File.Exists(path)) return false;

        try
        {
            string json = File.ReadAllText(path);
            data = JsonUtility.FromJson<SaveData>(json);
            data.slotIndex = slotIndex;
            return data != null;
        }
        catch (Exception e)
        {
            Debug.LogError($"读档失败 slot {slotIndex}: {e.Message}");
            return false;
        }
    }

    public SaveData LoadOrNull(int slotIndex)
    {
        TryLoad(slotIndex, out SaveData data);
        return data;
    }

    public void Save(int slotIndex, SaveData data)
    {
        data.slotIndex = slotIndex;
        data.lastSaveUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(GetPath(slotIndex), json);
    }

    public void Delete(int slotIndex)
    {
        string path = GetPath(slotIndex);
        if (File.Exists(path))
            File.Delete(path);
    }

    public int GetMostRecentSlot()
    {
        int bestSlot = -1;
        long bestTime = 0;
        for (int i = 0; i < GameManager.SaveSlotCount; i++)
        {
            if (!TryLoad(i, out SaveData data)) continue;
            if (data.lastSaveUnix > bestTime)
            {
                bestTime = data.lastSaveUnix;
                bestSlot = i;
            }
        }
        return bestSlot;
    }

    public string GetSlotSummary(int slotIndex)
    {
        if (!TryLoad(slotIndex, out SaveData data))
            return "空";

        var dt = DateTimeOffset.FromUnixTimeSeconds(data.lastSaveUnix).LocalDateTime;
        return $"{data.playerName} | 金币 {data.gold} | {FormatTime(data.playTimeSeconds)} | {dt:yyyy-MM-dd HH:mm}";
    }

    static string FormatTime(float seconds)
    {
        int h = (int)(seconds / 3600f);
        int m = (int)((seconds % 3600f) / 60f);
        return h > 0 ? $"{h}h{m}m" : $"{m}m";
    }
}
