using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioSettingsUI : MonoBehaviour
{
    [Header("混音器")]
    public AudioMixer mainMixer;

    /// <summary>
    /// 设置 BGM 音量。此方法被 BGM 滑块的 OnValueChanged 事件调用。
    /// </summary>
    /// <param name="value">滑块的值 (0.0 to 1.0)</param>
    public void SetBGMVolume(float value)
    {
        mainMixer.SetFloat("BGMVolume", ConvertToDecibel(value));
    }

    public void SetSFXVolume(float value)
    {
        mainMixer.SetFloat("SFXVolume", ConvertToDecibel(value));
    }

    public void SetUIVolume(float value)
    {
        mainMixer.SetFloat("UIVolume", ConvertToDecibel(value));
    }
    
    private static float ConvertToDecibel(float value)
    {
        value = Mathf.Clamp(value, 0.0001f, 1f);
        return Mathf.Log10(value) * 20f;
    }
}