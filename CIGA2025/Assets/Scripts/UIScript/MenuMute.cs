using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class GlobalVolumeControl : MonoBehaviour
{
    public AudioMixer mixer;       // 
    public Slider volumeSlider;    // 
    public float default_value =1f;
    void Start()
    {
        // 初始化 slider 和音量
        volumeSlider.minValue = 0.0001f;  // 避免 log(0)
        volumeSlider.maxValue = 1f;
        volumeSlider.value = default_value;
        volumeSlider.onValueChanged.AddListener(SetVolume);
    }

    public void SetVolume(float value)
    {
        // Unity 的 AudioMixer 使用分贝（dB），人耳是对数感知的：
        // volume=1 -> 0dB（正常），volume=0.5 -> -6dB，volume=0 -> -80dB（静音）
        float volumeInDb = Mathf.Log10(value) * 20f;
        mixer.SetFloat("Volume", volumeInDb);
    }
}
