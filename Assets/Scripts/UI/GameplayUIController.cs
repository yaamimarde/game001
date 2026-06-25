using UnityEngine;

/// <summary>
/// 游戏场景 UI 根节点，确保 HUD/暂停/背包/交易/雇佣 UI 存在。
/// </summary>
public class GameplayUIController : MonoBehaviour
{
    [SerializeField] GameplayHUD hud;
    [SerializeField] PauseMenuUI pauseMenu;
    [SerializeField] InventoryUI inventoryUI;
    [SerializeField] TradeUI tradeUI;
    [SerializeField] HireUI hireUI;
    [SerializeField] SettingsPanel settingsPanel;

    void Awake()
    {
        if (GameManager.Instance != null && GameManager.Instance.Session.IsActive)
            GameManager.Instance.ApplyPlayerStats();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
            hireUI?.OpenSquad();
    }
}
