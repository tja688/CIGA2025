// 示例 GameSettingsUI.cs
using UnityEngine;
using UnityEngine.UI; // 用于 Slider 和 Button

public class GameSettingsUI : MonoBehaviour
{
    [Header("Background Audio UI")]
    public Slider backgroundVolumeSlider;
    public Button backgroundMuteButton;

    [Header("SFX Audio UI")]
    public Slider sfxVolumeSlider;
    public Button sfxMuteButton;

    [Header("UI Sounds Audio UI")]
    public Slider uiVolumeSlider;
    public Button uiMuteButton;

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
        }
    }

    // --- 按钮点击回调 (用于更新按钮文本) ---
    void OnToggleBackgroundMute()
    {
        AudioManager.Instance.ToggleBackgroundMute();
        // (可选) 根据静音状态启用/禁用滑块
        if (backgroundVolumeSlider != null) backgroundVolumeSlider.interactable = !AudioManager.Instance.IsBackgroundMuted();
    }

    void OnToggleSFXMute()
    {
        AudioManager.Instance.ToggleSFXMute();
        if (sfxVolumeSlider != null) sfxVolumeSlider.interactable = !AudioManager.Instance.IsSFXMuted();
    }

    void OnToggleUIMute()
    {
        AudioManager.Instance.ToggleUIMute();
        if (uiVolumeSlider != null) uiVolumeSlider.interactable = !AudioManager.Instance.IsUIMuted();
    }
    
}