using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameplayHUD : MonoBehaviour
{
    [SerializeField] Slider hpSlider;
    [SerializeField] TMP_Text hpText;
    [SerializeField] TMP_Text goldText;
    [SerializeField] TMP_Text statsText;
    [SerializeField] GameObject gameOverPanel;
    [SerializeField] Button gameOverMenuButton;

    WarriorPlayer player;

    void Awake()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (gameOverMenuButton != null)
            gameOverMenuButton.onClick.AddListener(() => GameManager.Instance?.ReturnToMainMenu(false));
    }

    void OnEnable()
    {
        GameEventBus.OnGoldChanged += RefreshGold;
        GameEventBus.OnStatChanged += RefreshStats;
        GameEventBus.OnPlayerDied += ShowGameOver;
        GameEventBus.OnGameLoaded += RefreshAll;
    }

    void OnDisable()
    {
        GameEventBus.OnGoldChanged -= RefreshGold;
        GameEventBus.OnStatChanged -= RefreshStats;
        GameEventBus.OnPlayerDied -= ShowGameOver;
        GameEventBus.OnGameLoaded -= RefreshAll;
    }

    void Start()
    {
        player = FindObjectOfType<WarriorPlayer>();
        if (player != null)
            player.OnDamaged += (_, __) => RefreshHp();
        RefreshAll();
    }

    void Update()
    {
        RefreshHp();
    }

    void RefreshAll()
    {
        RefreshHp();
        RefreshGold();
        RefreshStats();
    }

    void RefreshHp()
    {
        if (player == null) player = FindObjectOfType<WarriorPlayer>();
        if (player == null) return;

        if (hpSlider != null)
        {
            hpSlider.maxValue = player.maxHp;
            hpSlider.value = player.hp;
        }
        if (hpText != null)
            hpText.text = $"HP {player.hp}/{player.maxHp}";
    }

    void RefreshGold()
    {
        if (goldText == null || GameManager.Instance == null || !GameManager.Instance.Session.IsActive) return;
        goldText.text = $"金币: {GameManager.Instance.Session.Gold}";
    }

    void RefreshStats()
    {
        if (statsText == null || GameManager.Instance == null || !GameManager.Instance.Session.IsActive) return;
        var s = GameManager.Instance.Session.Progression.Stats;
        statsText.text = $"力{s.strength} 敏{s.dexterity} 韧{s.toughness} 跑{s.athletics} 近{s.melee}";
    }

    void ShowGameOver()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
        GameManager.Instance?.SetPaused(true);
    }
}
