// PlaybackSettings.cs (Compatible Version)
using UnityEngine;

[System.Serializable]
public struct PlaybackSettings
{
    public bool isLooping;
    [Range(0f, 5f)]
    public float volumeMultiplier; 
    [Range(0f, 10f)]
    public float fadeInDuration;

    public static PlaybackSettings Default => new PlaybackSettings
    {
        isLooping = false,
        volumeMultiplier = 1.0f,
        fadeInDuration = 0f
    };
}