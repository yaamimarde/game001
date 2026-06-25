using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SaveSlotPanel : MonoBehaviour
{
    [SerializeField] GameObject panelRoot;
    [SerializeField] SaveSlotButton[] slotButtons;
    [SerializeField] Button closeButton;
    [SerializeField] TMP_InputField playerNameInput;
    [SerializeField] TMP_Text panelTitleText;
    [SerializeField] TMP_Text panelHintText;
    [SerializeField] MainMenuUI mainMenuUI;

    bool isNewGameMode;

    void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);

        for (int i = 0; i < slotButtons.Length; i++)
        {
            int slot = i;
            if (slotButtons[i] != null)
                slotButtons[i].Setup(slot, OnSlotClicked);
        }
    }

    public void ShowForNewGame()
    {
        isNewGameMode = true;
        Show();
    }

    public void ShowForLoad()
    {
        isNewGameMode = false;
        Show();
    }

    void Show()
    {
        if (panelRoot != null) panelRoot.SetActive(true);
        mainMenuUI?.HideMainButtons(true);

        if (playerNameInput != null)
        {
            var nameRow = playerNameInput.transform.parent != null
                ? playerNameInput.transform.parent.gameObject
                : playerNameInput.gameObject;
            nameRow.SetActive(isNewGameMode);
        }

        if (panelTitleText != null)
            panelTitleText.text = isNewGameMode ? "新游戏 · 选择存档槽位" : "读档 · 选择存档槽位";

        if (panelHintText != null)
            panelHintText.text = isNewGameMode
                ? "点击空槽位开始游戏，已有存档将被覆盖"
                : "点击已有存档的槽位继续游戏";

        RefreshSlots();
    }

    public void Hide()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        mainMenuUI?.HideMainButtons(false);
    }

    void RefreshSlots()
    {
        for (int i = 0; i < slotButtons.Length; i++)
        {
            string summary = GetSlotSummary(i);
            slotButtons[i]?.SetLabel($"槽位 {i + 1}", summary);
        }
    }

    string GetSlotSummary(int slotIndex)
    {
        if (GameManager.Instance == null)
        {
            return isNewGameMode ? "空 · 点击开始" : "空";
        }

        bool exists = GameManager.Instance.SaveManager.SlotExists(slotIndex);
        if (isNewGameMode)
        {
            if (!exists)
                return "空 · 点击开始";

            return $"{GameManager.Instance.SaveManager.GetSlotSummary(slotIndex)} · 点击覆盖并开始";
        }

        if (!exists)
            return "空";

        return GameManager.Instance.SaveManager.GetSlotSummary(slotIndex);
    }

    void OnSlotClicked(int slot)
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("SaveSlotPanel: GameManager 未初始化，无法开始游戏。");
            return;
        }

        if (isNewGameMode)
        {
            string name = playerNameInput != null && !string.IsNullOrWhiteSpace(playerNameInput.text)
                ? playerNameInput.text
                : "战士";
            GameManager.Instance.StartNewGame(slot, name);
        }
        else
        {
            if (GameManager.Instance.SaveManager.SlotExists(slot))
                GameManager.Instance.LoadGame(slot);
        }

        Hide();
    }
}

public class SaveSlotButton : MonoBehaviour
{
    [SerializeField] Button button;
    [SerializeField] TMP_Text titleText;
    [SerializeField] TMP_Text summaryText;

    int slotIndex;
    Action<int> onClick;

    public void Setup(int slot, Action<int> callback)
    {
        slotIndex = slot;
        onClick = callback;
        if (button != null)
            button.onClick.AddListener(() => onClick?.Invoke(slotIndex));
    }

    public void SetLabel(string title, string summary)
    {
        if (titleText != null) titleText.text = title;
        if (summaryText != null) summaryText.text = summary;
    }
}
