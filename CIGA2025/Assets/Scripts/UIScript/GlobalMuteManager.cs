using UnityEngine;
using UnityEngine.Audio; 
using UnityEngine.UI;   

public class GlobalMuteManager : MonoBehaviour
{

    [Header("UI视觉反馈 (可选)")]
    [Tooltip("关联静音按钮的Image组件，用于切换图标")]
    public Image muteButtonImage;
    [Tooltip("正常发声状态下显示的图标")]
    public Sprite unmuteSprite;
    [Tooltip("静音状态下显示的图标")]
    public Sprite muteSprite;

    // 用于追踪当前是否处于静音状态
    private bool isMuted = false;
    
    /// <summary>
    /// 公开方法，用于UI按钮调用来切换静音状态
    /// </summary>
    public void ToggleMute()
    {
        isMuted = !isMuted; // 翻转静音状态
        
        AudioManager.Instance.ToggleBackgroundMute();
        AudioManager.Instance.ToggleSFXMute();
        AudioManager.Instance.ToggleUIMute();

        UpdateMuteButtonVisuals();
    }

    /// <summary>
    /// 更新静音按钮的图标
    /// </summary>
    private void UpdateMuteButtonVisuals()
    {
        if (muteButtonImage == null) return;

        muteButtonImage.sprite = isMuted ? muteSprite : unmuteSprite;
    }
}