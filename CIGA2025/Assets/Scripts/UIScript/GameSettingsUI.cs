// 示例 GameSettingsUI.cs
using UnityEngine;
using UnityEngine.UI; // 用于 Slider 和 Button

public class GameSettingsUI : MonoBehaviour
{
    [Header("Background Audio UI")]
    public Slider backgroundVolumeSlider;
    public Button backgroundMuteButton;
    public Text backgroundMuteButtonText; // (可选) 用于显示 "静音"/"取消静音"

    [Header("SFX Audio UI")]
    public Slider sfxVolumeSlider;
    public Button sfxMuteButton;
    public Text sfxMuteButtonText;

    [Header("UI Sounds Audio UI")]
    public Slider uiVolumeSlider;
    public Button uiMuteButton;
    public Text uiMuteButtonText;

    void Start()
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogError("GameSettingsUI: AudioManager.Instance is not found!");
            return;
        }

        // --- 初始化背景音量UI ---
        if (backgroundVolumeSlider != null)
        {
            backgroundVolumeSlider.minValue = 0.0001f; // 确保与AudioManager中处理一致
            backgroundVolumeSlider.maxValue = 1f;
            backgroundVolumeSlider.value = AudioManager.Instance.GetInitialBackgroundVolume();
            backgroundVolumeSlider.onValueChanged.AddListener(AudioManager.Instance.SetBackgroundVolumeSlider);
        }
        if (backgroundMuteButton != null)
        {
            backgroundMuteButton.onClick.AddListener(OnToggleBackgroundMute); // 调用本地方法更新UI
            UpdateMuteButtonText(backgroundMuteButtonText, AudioManager.Instance.IsBackgroundMuted(), "背景");
        }

        // --- 初始化SFX音量UI ---
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.minValue = 0.0001f;
            sfxVolumeSlider.maxValue = 1f;
            sfxVolumeSlider.value = AudioManager.Instance.GetInitialSFXVolume();
            sfxVolumeSlider.onValueChanged.AddListener(AudioManager.Instance.SetSFXVolumeSlider);
        }
        if (sfxMuteButton != null)
        {
            sfxMuteButton.onClick.AddListener(OnToggleSFXMute);
            UpdateMuteButtonText(sfxMuteButtonText, AudioManager.Instance.IsSFXMuted(), "音效");
        }

        // --- 初始化UI音效音量UI ---
        if (uiVolumeSlider != null)
        {
            uiVolumeSlider.minValue = 0.0001f;
            uiVolumeSlider.maxValue = 1f;
            uiVolumeSlider.value = AudioManager.Instance.GetInitialUIVolume();
            uiVolumeSlider.onValueChanged.AddListener(AudioManager.Instance.SetUIVolumeSlider);
        }
        if (uiMuteButton != null)
        {
            uiMuteButton.onClick.AddListener(OnToggleUIMute);
            UpdateMuteButtonText(uiMuteButtonText, AudioManager.Instance.IsUIMuted(), "界面");
        }
    }

    // --- 按钮点击回调 (用于更新按钮文本) ---
    void OnToggleBackgroundMute()
    {
        AudioManager.Instance.ToggleBackgroundMute();
        UpdateMuteButtonText(backgroundMuteButtonText, AudioManager.Instance.IsBackgroundMuted(), "背景");
        // (可选) 根据静音状态启用/禁用滑块
        if (backgroundVolumeSlider != null) backgroundVolumeSlider.interactable = !AudioManager.Instance.IsBackgroundMuted();
    }

    void OnToggleSFXMute()
    {
        AudioManager.Instance.ToggleSFXMute();
        UpdateMuteButtonText(sfxMuteButtonText, AudioManager.Instance.IsSFXMuted(), "音效");
        if (sfxVolumeSlider != null) sfxVolumeSlider.interactable = !AudioManager.Instance.IsSFXMuted();
    }

    void OnToggleUIMute()
    {
        AudioManager.Instance.ToggleUIMute();
        UpdateMuteButtonText(uiMuteButtonText, AudioManager.Instance.IsUIMuted(), "界面");
        if (uiVolumeSlider != null) uiVolumeSlider.interactable = !AudioManager.Instance.IsUIMuted();
    }

    void UpdateMuteButtonText(Text buttonText, bool isMuted, string channelName)
    {
        if (buttonText != null)
        {
            buttonText.text = isMuted ? $"取消{channelName}静音" : $"{channelName}静音";
        }
    }
}