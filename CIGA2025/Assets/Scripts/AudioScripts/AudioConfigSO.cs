using UnityEngine;
using UnityEngine.Audio;

[System.Serializable]
public struct AudioPlaybackParameters
{
    public AudioClip Clip;
    public float Volume;
    public AudioMixerGroup MixerGroup;
}

[CreateAssetMenu(fileName = "AudioConfig_", menuName = "Audio/Audio Configuration (Data-Only)")]
public class AudioConfigSO : ScriptableObject
{
    [Header("通用设置")]
    [Tooltip("声音将输出到此音频混合器轨道")]
    public AudioMixerGroup mixerGroup;

    // --- 单一音频源 ---
    [Header("单一音频源设置")]
    public AudioClip audioClip;
    [Tooltip("是否对单一音源使用随机音量")]
    public bool isRandomVolume = false;
    [Range(0f, 2f)] public float volume = 0.5f;
    [Range(0f, 2f)] public float minVolume = 0.3f;
    [Range(0f, 2f)] public float maxVolume = 0.7f;
    
    // --- 随机音频池 ---
    [Header("随机音频池设置")]
    [Tooltip("勾选以使用下方的“随机音频池”，否则使用“单一音频源”。")]
    public bool useRandomPool = false;
    [Tooltip("勾选后，播放池中音频时，会将其独立音量再乘以一个随机修正值。")]
    public bool applyRandomVolumeModifier = false;
    [Tooltip("随机音量修正的最小值 (乘数)")]
    public float volumeModifierMin = 0.8f;
    [Tooltip("随机音量修正的最大值 (乘数)")]
    public float volumeModifierMax = 1.2f;
    [Space]
    public AudioPoolItem[] audioPool;
    
    public AudioPlaybackParameters GetPlaybackParameters()
    {
        var parameters = new AudioPlaybackParameters
        {
            MixerGroup = this.mixerGroup
        };

        if (!useRandomPool)
        {
            parameters.Clip = this.audioClip;
            parameters.Volume = isRandomVolume ? Random.Range(minVolume, maxVolume) : volume;
        }
        else
        {
            if (audioPool == null || audioPool.Length == 0)
            {
                Debug.LogWarning($"音频配置 {name} 的随机池为空，无法获取参数！");
                return default; // 返回一个空的参数包
            }

            var selectedItem = audioPool[Random.Range(0, audioPool.Length)];
            parameters.Clip = selectedItem.audioClip;
            
            var baseVolume = selectedItem.volume;
            if (applyRandomVolumeModifier)
            {
                baseVolume *= Random.Range(volumeModifierMin, volumeModifierMax);
            }
            parameters.Volume = Mathf.Clamp01(baseVolume);
        }

        return parameters;
    }
}

[System.Serializable]
public class AudioPoolItem
{
    [Tooltip("要播放的音频剪辑")]
    public AudioClip audioClip;

    [Tooltip("该音频的基础音量")]
    [Range(0f, 1f)]
    public float volume = 0.7f;
}