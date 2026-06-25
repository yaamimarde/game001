using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsPanel : MonoBehaviour
{
    [SerializeField] GameObject panelRoot;
    [SerializeField] Slider masterSlider;
    [SerializeField] Slider musicSlider;
    [SerializeField] Slider sfxSlider;
    [SerializeField] Toggle fullscreenToggle;
    [SerializeField] Button closeButton;
    [SerializeField] Button applyButton;

    void Awake()
    {
        if (closeButton != null) closeButton.onClick.AddListener(Hide);
        if (applyButton != null) applyButton.onClick.AddListener(Apply);
    }

    void Start()
    {
        LoadFromManager();
    }

    public void Show()
    {
        LoadFromManager();
        if (panelRoot != null) panelRoot.SetActive(true);
    }

    public void Hide()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    void LoadFromManager()
    {
        if (GameManager.Instance == null) return;
        var d = GameManager.Instance.Settings.Data;
        if (masterSlider != null) masterSlider.value = d.masterVolume;
        if (musicSlider != null) musicSlider.value = d.musicVolume;
        if (sfxSlider != null) sfxSlider.value = d.sfxVolume;
        if (fullscreenToggle != null) fullscreenToggle.isOn = d.fullscreen;
    }

    void Apply()
    {
        if (GameManager.Instance == null) return;
        var d = GameManager.Instance.Settings.Data;
        if (masterSlider != null) d.masterVolume = masterSlider.value;
        if (musicSlider != null) d.musicVolume = musicSlider.value;
        if (sfxSlider != null) d.sfxVolume = sfxSlider.value;
        if (fullscreenToggle != null) d.fullscreen = fullscreenToggle.isOn;
        GameManager.Instance.Settings.Save();
    }
}
