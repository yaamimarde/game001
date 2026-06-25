using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PauseMenuUI : MonoBehaviour
{
    [SerializeField] GameObject panelRoot;
    [SerializeField] Button resumeButton;
    [SerializeField] Button saveButton;
    [SerializeField] Button settingsButton;
    [SerializeField] Button mainMenuButton;
    [SerializeField] SettingsPanel settingsPanel;

    void Awake()
    {
        if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
        if (saveButton != null) saveButton.onClick.AddListener(Save);
        if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(ToMainMenu);
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (GameManager.Instance == null) return;
            bool show = panelRoot != null && !panelRoot.activeSelf;
            if (show) Show();
            else Resume();
        }
    }

    public void Show()
    {
        if (panelRoot != null) panelRoot.SetActive(true);
        GameManager.Instance?.SetPaused(true);
    }

    public void Resume()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        GameManager.Instance?.SetPaused(false);
    }

    void Save()
    {
        GameManager.Instance?.SaveCurrentGame();
    }

    void OpenSettings()
    {
        settingsPanel?.Show();
    }

    void ToMainMenu()
    {
        GameManager.Instance?.ReturnToMainMenu(true);
    }
}
