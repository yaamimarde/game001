using System;
using UnityEngine;

[Serializable]
public class SettingsData
{
    public float masterVolume = 1f;
    public float musicVolume = 0.8f;
    public float sfxVolume = 1f;
    public bool fullscreen = true;
}

public class SettingsManager
{
    const string Key = "game_settings_v1";
    public SettingsData Data { get; private set; } = new SettingsData();

    public void Load()
    {
        if (!PlayerPrefs.HasKey(Key))
        {
            Apply();
            return;
        }

        string json = PlayerPrefs.GetString(Key);
        Data = JsonUtility.FromJson<SettingsData>(json) ?? new SettingsData();
        Apply();
    }

    public void Save()
    {
        PlayerPrefs.SetString(Key, JsonUtility.ToJson(Data));
        PlayerPrefs.Save();
        Apply();
        GameEventBus.RaiseSettingsChanged();
    }

    public void Apply()
    {
        AudioListener.volume = Data.masterVolume;
        Screen.fullScreen = Data.fullscreen;
    }
}
