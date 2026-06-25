using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using Scene = UnityEngine.SceneManagement.Scene;

/// <summary>
/// 全局单例，DontDestroyOnLoad，管理游戏生命周期。
/// </summary>
public class GameManager : MonoBehaviour
{
    public const int SaveSlotCount = 3;
    public const string MainMenuScene = "MainMenu";
    public const string DefaultWorldScene = "Main";

    public static GameManager Instance { get; private set; }

    [SerializeField] float autoSaveInterval = 300f;

    public GameSession Session { get; private set; } = new GameSession();
    public SaveManager SaveManager { get; private set; }
    public SettingsManager Settings { get; private set; }

    public bool IsPaused { get; private set; }
    public int ActiveSlotIndex { get; private set; } = -1;

    float autoSaveTimer;
    float playTimeAccumulator;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SaveManager = new SaveManager();
        Settings = new SettingsManager();
        Settings.Load();
    }

    void Update()
    {
        if (!Session.IsActive) return;

        Session.TickPlayTime();

        autoSaveTimer += Time.unscaledDeltaTime;
        if (autoSaveTimer >= autoSaveInterval)
        {
            autoSaveTimer = 0f;
            AutoSave();
        }
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!Session.IsActive) return;

        Session.Save.currentSceneName = scene.name;
        Session.Companions?.RepositionNearPlayer();
        ApplyPlayerStats();

        if (scene.name != MainMenuScene && FindObjectOfType<GameWorldBootstrap>() == null)
        {
            var go = new GameObject("GameWorldBootstrap");
            go.AddComponent<GameWorldBootstrap>();
        }
    }

    public void StartNewGame(int slotIndex, string playerName = "战士")
    {
        ActiveSlotIndex = slotIndex;
        var save = SaveData.CreateNew(slotIndex, playerName);
        Session.StartNew(save);
        SceneTransitionContext.NextSpawnPointId = save.spawnPointId;
        SceneTransitionContext.IsLoadingFromSave = false;
        LoadScene(DefaultWorldScene);
    }

    public void LoadGame(int slotIndex)
    {
        if (!SaveManager.TryLoad(slotIndex, out SaveData save))
            return;

        ActiveSlotIndex = slotIndex;
        Session.LoadFrom(save);
        SceneTransitionContext.NextSpawnPointId = save.spawnPointId;
        SceneTransitionContext.IsLoadingFromSave = true;
        LoadScene(save.currentSceneName);
    }

    public bool ContinueLastGame()
    {
        int slot = SaveManager.GetMostRecentSlot();
        if (slot < 0) return false;
        LoadGame(slot);
        return true;
    }

    public void SaveCurrentGame()
    {
        if (!Session.IsActive || ActiveSlotIndex < 0) return;

        var player = FindPlayer();
        if (player != null)
        {
            Session.Save.spawnPointId = GetNearestSpawnId(player.transform.position) ?? Session.Save.spawnPointId;
        }

        Session.SyncToSave();
        SaveManager.Save(ActiveSlotIndex, Session.Save);
        GameEventBus.RaiseGameSaved();
    }

    public void AutoSave()
    {
        if (Session.IsActive && ActiveSlotIndex >= 0)
            SaveCurrentGame();
    }

    public void ReturnToMainMenu(bool saveFirst = true)
    {
        if (saveFirst && Session.IsActive)
            SaveCurrentGame();

        SetPaused(false);
        Session.End();
        ActiveSlotIndex = -1;
        LoadScene(MainMenuScene);
    }

    public void SetPaused(bool paused)
    {
        IsPaused = paused;
        Time.timeScale = paused ? 0f : 1f;
        GameEventBus.RaisePauseChanged(paused);
    }

    public void QuitGame()
    {
        if (Session.IsActive)
            SaveCurrentGame();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OnPlayerDeath()
    {
        GameEventBus.RaisePlayerDied();
    }

    public void ApplyPlayerStats()
    {
        var player = FindPlayer();
        if (player == null || Session?.Progression == null) return;

        var stats = Session.Progression.Stats;
        player.characterName = Session.Save.playerName;
        player.hp = stats.currentHp;
        player.damage = Session.Progression.GetEffectiveDamage();
        player.defense = Session.Progression.GetEffectiveDefense();
        player.maxHp = stats.maxHp;
    }

    public void SyncPlayerHpFromCharacter(Character player)
    {
        if (Session?.Progression == null || player == null) return;
        Session.Progression.Stats.currentHp = player.hp;
    }

    static WarriorPlayer FindPlayer()
    {
        return Object.FindObjectOfType<WarriorPlayer>();
    }

    static string GetNearestSpawnId(Vector3 position)
    {
        SpawnPoint nearest = null;
        float best = float.MaxValue;
        foreach (var sp in Object.FindObjectsOfType<SpawnPoint>())
        {
            float d = Vector3.SqrMagnitude(sp.transform.position - position);
            if (d < best)
            {
                best = d;
                nearest = sp;
            }
        }
        return nearest != null ? nearest.SpawnId : null;
    }

    static void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
