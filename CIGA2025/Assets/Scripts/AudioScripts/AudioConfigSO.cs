using UnityEngine;
using UnityEngine.Audio; 

[CreateAssetMenu(fileName = "AudioConfig_", menuName = "Audio/Audio Configuration")]
public class AudioConfigSO : ScriptableObject
{
    [Header("音频混合器轨道")]
    [Tooltip("将声音输出到指定的音频混合器轨道 (Group)")]
    public AudioMixerGroup mixerGroup;

    [Header("是否启用随机音频池")]
    [Tooltip("如果勾选，将从下方的音频池中随机选择一个音频进行播放，并忽略上方的“单一音频源”设置。")]
    public bool useRandomPool = false;

    // --- 单一音频源设置 ---
    [Header("单一音频源")]
    [Tooltip("要播放的音频剪辑")]
    public AudioClip audioClip;

    [Tooltip("是否使用随机音量")]
    public bool isRandomVolume = false;

    [Tooltip("固定音量大小 (0-1)")]
    [Range(0f, 1f)]
    public float volume = 1f;

    [Tooltip("最小音量")]
    [Range(0f, 1f)]
    public float minVolume = 0.8f;

    [Tooltip("最大音量")]
    [Range(0f, 1f)]
    public float maxVolume = 1.2f;


    // --- 随机音频池设置 ---
    [Header("随机音频池 (仅当启用时生效)")]
    public AudioPoolItem[] audioPool;
    
    
    public AudioClip GetClip()
    {
        if (useRandomPool)
        {
            if (audioPool != null && audioPool.Length != 0)
                return audioPool[Random.Range(0, audioPool.Length)].audioClip;
            Debug.LogWarning($"音频配置 {name} 已启用随机池，但池为空！");
            return null;
        }
        else
        {
            return audioClip;
        }
    }
    
    
    public float GetVolume()
    {
        return isRandomVolume ? Random.Range(minVolume, maxVolume) : volume;
    }
}

[System.Serializable]
public class AudioPoolItem
{
    public AudioClip audioClip;
}