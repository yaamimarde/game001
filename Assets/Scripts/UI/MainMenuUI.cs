using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] Button startButton;
    [SerializeField] Button continueButton;
    [SerializeField] Button loadButton;
    [SerializeField] Button settingsButton;
    [SerializeField] Button quitButton;
    [SerializeField] SaveSlotPanel saveSlotPanel;
    [SerializeField] SettingsPanel settingsPanel;
    [SerializeField] GameObject mainButtons;

    void Start()
    {
        if (startButton != null) startButton.onClick.AddListener(OnStart);
        if (continueButton != null) continueButton.onClick.AddListener(OnContinue);
        if (loadButton != null) loadButton.onClick.AddListener(OnLoad);
        if (settingsButton != null) settingsButton.onClick.AddListener(OnSettings);
        if (quitButton != null) quitButton.onClick.AddListener(OnQuit);

        if (continueButton != null && GameManager.Instance != null)
            continueButton.interactable = GameManager.Instance.SaveManager.GetMostRecentSlot() >= 0;
    }

    void OnStart()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("MainMenuUI: GameManager 未初始化，无法开始游戏。");
            return;
        }

        GameManager.Instance.StartNewGame(0, "战士");
    }

    void OnContinue()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.ContinueLastGame();
    }

    void OnLoad()
    {
        saveSlotPanel?.ShowForLoad();
    }

    void OnSettings()
    {
        settingsPanel?.Show();
    }

    void OnQuit()
    {
        GameManager.Instance?.QuitGame();
    }

    public void HideMainButtons(bool hide)
    {
        if (mainButtons != null)
            mainButtons.SetActive(!hide);
    }
}
