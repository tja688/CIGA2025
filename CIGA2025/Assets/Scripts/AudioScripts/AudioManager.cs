using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks; 
using UnityEngine.Audio; // 【新增】引入 AudioMixer 命名空间

[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    [Header("调试开关")]
    public bool enableDebugLogging = false;

    // --- 【新增】从旧脚本移植过来的混音器设置 ---
    [Header("Audio Mixer Settings")]
    [Tooltip("在此处指定场景中的主 Audio Mixer")]
    public AudioMixer masterMixer; 

    private const string BACKGROUND_MIXER_PARAM = "BackGround";
    private const string SFX_MIXER_PARAM = "SFX";
    private const string UI_MIXER_PARAM = "UI";

    private const string PREFS_BG_VOL = "AudioManager_BackgroundVolume";
    private const string PREFS_BG_MUTE = "AudioManager_BackgroundMute";
    private const string PREFS_SFX_VOL = "AudioManager_SFXVolume";
    private const string PREFS_SFX_MUTE = "AudioManager_SFXMute";
    private const string PREFS_UI_VOL = "AudioManager_UIVolume";
    private const string PREFS_UI_MUTE = "AudioManager_UIMute";

    private bool _isBackgroundMuted = false;
    private float _lastBackgroundVolumeLinear = 0.75f;

    private bool _isSFXMuted = false;
    private float _lastSFXVolumeLinear = 0.75f;

    private bool _isUIMuted = false;
    private float _lastUIVolumeLinear = 0.75f;
    // --- 新增部分结束 ---


    #region Singleton & Pooling
    public static AudioManager Instance { get; private set; }
    private List<AudioSource> _audioSourcePool;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializePool(15);
        
        // 【新增】初始化混音器设置
        InitializeMixerSettings();
    }

    private void InitializePool(int size)
    {
        _audioSourcePool = new List<AudioSource>();
        for (var i = 0; i < size; i++) CreatePooledAudioSource();
    }

    private AudioSource GetPooledAudioSource()
    {
        var source = _audioSourcePool.FirstOrDefault(s => !s.gameObject.activeInHierarchy);
        if (!source) source = CreatePooledAudioSource();
        source.gameObject.SetActive(true);
        return source;
    }

    private AudioSource CreatePooledAudioSource()
    {
        var go = new GameObject("PooledAudioSource_" + _audioSourcePool.Count);
        go.transform.SetParent(transform);
        var source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        _audioSourcePool.Add(source);
        go.SetActive(false);
        return source;
    }
    #endregion

    #region Track Management
    private readonly Dictionary<int, CancellationTokenSource> _trackCts = new Dictionary<int, CancellationTokenSource>();
    private readonly Dictionary<int, AudioSource> _activeSources = new Dictionary<int, AudioSource>();
    private readonly Dictionary<int, float> _trackTargetVolumes = new Dictionary<int, float>();
    private int _nextTrackId = 0;
    #endregion

    #region Public Core Methods - Refactored API

    /// <summary>
    /// 播放音频
    /// </summary>
    public int Play(AudioConfigSO config, bool isLooping = false, float fadeInDuration = 0f, float volumeMultiplier = 1.0f, float stopFadeOutDuration = 0f)
    {
        if (!config)
        {
            Log("<color=red><b>Play Error:</b> AudioConfigSO is null.</color>");
            return -1;
        }

        var trackId = _nextTrackId++;
        var source = GetPooledAudioSource();
        var cts = new CancellationTokenSource();
        
        _activeSources[trackId] = source;
        _trackCts[trackId] = cts;

        var settingsDesc = new StringBuilder();
        settingsDesc.Append($"Looping: {isLooping}, ");
        settingsDesc.Append($"VolumeMultiplier: {volumeMultiplier}, ");
        settingsDesc.Append($"FadeIn: {fadeInDuration}s, ");
        settingsDesc.Append($"StopFadeOut: {stopFadeOutDuration}s");
        Log($"<color=cyan><b>Play Request (Track ID: {trackId}):</b></color> Config='{config.name}', Settings=[{settingsDesc}]");

        PlayRoutineAsync(trackId, config, isLooping, fadeInDuration, volumeMultiplier, stopFadeOutDuration, source, cts.Token).Forget();

        return trackId;
    }

    public void Pause(int trackId, float fadeOutDuration = 0f)
    {
        Log($"<color=yellow><b>Pause Request (Track ID: {trackId}):</b></color> FadeOut: {fadeOutDuration}s");
        if (!_activeSources.TryGetValue(trackId, out var source) || !source.isPlaying)
        {
            Log($"<color=orange>Pause Warning:</color> Track {trackId} not found or not playing.");
            return;
        }
        PauseAsync(source, fadeOutDuration).Forget();
    }

    public void Resume(int trackId, float fadeInDuration = 0f)
    {
        Log($"<color=yellow><b>Resume Request (Track ID: {trackId}):</b></color> FadeIn: {fadeInDuration}s");
        if (!_activeSources.TryGetValue(trackId, out var source) || source.isPlaying)
        {
            Log($"<color=orange>Resume Warning:</color> Track {trackId} not found or already playing.");
            return;
        }
        _trackTargetVolumes.TryGetValue(trackId, out var targetVolume);
        ResumeAsync(source, fadeInDuration, targetVolume).Forget();
    }

    public void Stop(int trackId, float fadeOutDuration = 0f)
    {
        Log($"<color=red><b>Stop Request (Track ID: {trackId}):</b></color> FadeOut: {fadeOutDuration}s");
        
        if (_trackCts.TryGetValue(trackId, out var cts))
        {
            cts.Cancel();
            cts.Dispose(); 
            Log($"PlayRoutine for track {trackId} cancelled.");
        }

        if (_activeSources.TryGetValue(trackId, out var source))
        {
            FadeAndStopAsync(source, fadeOutDuration).Forget();
        }
        
        CleanUpTrack(trackId);
    }
    #endregion

    #region UniTask Routines

    private async UniTaskVoid PlayRoutineAsync(int trackId, AudioConfigSO config, bool isLooping, float fadeInDuration, float volumeMultiplier, float stopFadeOutDuration, AudioSource source, CancellationToken cancellationToken)
    {
        Log($"Track {trackId}: PlayRoutine started.");
        try
        {
            do
            {
                if (cancellationToken.IsCancellationRequested) break;
                source.Stop();
                var playbackParams = config.GetPlaybackParameters();
                if (!playbackParams.Clip)
                {
                    Log($"<color=orange>Track {trackId} Warning:</color> Clip is null, aborting routine.");
                    break;
                }
                source.clip = playbackParams.Clip;
                source.outputAudioMixerGroup = playbackParams.MixerGroup;
                var targetVolume = playbackParams.Volume * volumeMultiplier;
                _trackTargetVolumes[trackId] = targetVolume;
                Log($"Track {trackId}: Playing clip '{source.clip.name}' at target volume {targetVolume}.");
                source.Play();
                if (fadeInDuration > 0)
                {
                    await FadeAsync(source, 0, targetVolume, fadeInDuration, $"Track {trackId} FadeIn", cancellationToken);
                }
                else
                {
                    source.volume = targetVolume;
                }
                await UniTask.Delay(System.TimeSpan.FromSeconds(source.clip.length), ignoreTimeScale: false, cancellationToken: cancellationToken);
                Log($"Track {trackId}: Clip '{source.clip.name}' finished.");
            } while (isLooping && !cancellationToken.IsCancellationRequested);
        }
        catch (System.OperationCanceledException)
        {
            Log($"Track {trackId}: PlayRoutine was cancelled externally.");
        }
        finally
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                Log($"Track {trackId}: PlayRoutine loop finished. Calling Stop with fade {stopFadeOutDuration}s.");
                Stop(trackId, stopFadeOutDuration);
            }
        }
    }

    private async UniTask FadeAsync(AudioSource source, float startVolume, float endVolume, float duration, string debugName, CancellationToken cancellationToken)
    {
        Log($"<color=grey><i>Fade '{debugName}' started: {startVolume:F2} -> {endVolume:F2} over {duration}s.</i></color>");
        if (duration <= 0) { source.volume = endVolume; return; }
        var timer = 0f;
        while (timer < duration)
        {
            cancellationToken.ThrowIfCancellationRequested();
            source.volume = Mathf.Lerp(startVolume, endVolume, timer / duration);
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            timer += Time.deltaTime;
        }
        source.volume = endVolume;
        Log($"<color=grey><i>Fade '{debugName}' finished.</i></color>");
    }

    private async UniTaskVoid FadeAndStopAsync(AudioSource source, float duration)
    {
        if (source.isPlaying && duration > 0)
        {
            await FadeAsync(source, source.volume, 0, duration, "FadeToStop", this.GetCancellationTokenOnDestroy());
        }
        ReturnSourceToPool(source);
    }
    
    private async UniTaskVoid PauseAsync(AudioSource source, float duration)
    {
        if (duration > 0)
        {
            await FadeAsync(source, source.volume, 0, duration, "FadeToPause", this.GetCancellationTokenOnDestroy());
        }
        if(source && source.gameObject.activeInHierarchy) 
        {
            source.Pause();
            Log($"Source '{source.name}' paused after fade.");
        }
    }
    
    private async UniTaskVoid ResumeAsync(AudioSource source, float duration, float targetVolume)
    {
        Log($"Source '{source.name}' resuming...");
        source.UnPause();
        await FadeAsync(source, source.volume, targetVolume,  duration,"FadeToResume", this.GetCancellationTokenOnDestroy());
    }

    #endregion

    #region Utility
    private void ReturnSourceToPool(AudioSource source)
    {
        if (!source) return;
        var sourceName = source.name;
        source.Stop();
        source.clip = null;
        source.gameObject.SetActive(false);
        Log($"Source '{sourceName}' returned to pool.");
    }

    private void CleanUpTrack(int trackId)
    {
        _trackCts.Remove(trackId);
        _activeSources.Remove(trackId);
        _trackTargetVolumes.Remove(trackId);
        Log($"Track {trackId}: All records cleaned up.");
    }


    private void Log(string message)
    {
        if (enableDebugLogging)
        {
            Debug.Log($"[AudioManager] {message}");
        }
    }
    #endregion
    
    // ========================================================================================
    // --- 【新增】从旧脚本移植过来的 Mixer 和 PlayerPrefs 控制逻辑 ---
    // ========================================================================================

    #region Mixer Control and Persistence
    private void InitializeMixerSettings()
    {
        if (masterMixer == null)
        {
            Log("<color=orange><b>Warning:</b> Master AudioMixer is not assigned in the Inspector. Volume control will be unavailable.</color>");
            return;
        }
        LoadAndApplyMixerSettings();
    }
    
    private void SetMixerVolume(string exposedParamName, float linearValue)
    {
        if (masterMixer == null) return;
        // 将线性值 (0.0001 to 1) 转换为分贝 (-80 to 0)
        float decibels = Mathf.Log10(Mathf.Clamp(linearValue, 0.0001f, 1f)) * 20f;
        masterMixer.SetFloat(exposedParamName, decibels);
    }
    
    private void LoadAndApplyMixerSettings()
    {
        // Background
        _lastBackgroundVolumeLinear = PlayerPrefs.GetFloat(PREFS_BG_VOL, 0.75f);
        _isBackgroundMuted = PlayerPrefs.GetInt(PREFS_BG_MUTE, 0) == 1;
        SetMixerVolume(BACKGROUND_MIXER_PARAM, _isBackgroundMuted ? 0.0001f : _lastBackgroundVolumeLinear);

        // SFX
        _lastSFXVolumeLinear = PlayerPrefs.GetFloat(PREFS_SFX_VOL, 0.75f);
        _isSFXMuted = PlayerPrefs.GetInt(PREFS_SFX_MUTE, 0) == 1;
        SetMixerVolume(SFX_MIXER_PARAM, _isSFXMuted ? 0.0001f : _lastSFXVolumeLinear);

        // UI
        _lastUIVolumeLinear = PlayerPrefs.GetFloat(PREFS_UI_VOL, 0.75f);
        _isUIMuted = PlayerPrefs.GetInt(PREFS_UI_MUTE, 0) == 1;
        SetMixerVolume(UI_MIXER_PARAM, _isUIMuted ? 0.0001f : _lastUIVolumeLinear);
    }

    // --- Public API for UI ---

    public float GetInitialBackgroundVolume() => _lastBackgroundVolumeLinear;
    public float GetInitialSFXVolume() => _lastSFXVolumeLinear;
    public float GetInitialUIVolume() => _lastUIVolumeLinear;

    public bool IsBackgroundMuted() => _isBackgroundMuted;
    public bool IsSFXMuted() => _isSFXMuted;
    public bool IsUIMuted() => _isUIMuted;
    
    public void SetBackgroundVolumeSlider(float linearValue)
    {
        _lastBackgroundVolumeLinear = linearValue;
        if (!_isBackgroundMuted)
        {
            SetMixerVolume(BACKGROUND_MIXER_PARAM, _lastBackgroundVolumeLinear);
        }
        PlayerPrefs.SetFloat(PREFS_BG_VOL, _lastBackgroundVolumeLinear);
        PlayerPrefs.Save();
    }

    public void ToggleBackgroundMute()
    {
        _isBackgroundMuted = !_isBackgroundMuted;
        SetMixerVolume(BACKGROUND_MIXER_PARAM, _isBackgroundMuted ? 0.0001f : _lastBackgroundVolumeLinear);
        PlayerPrefs.SetInt(PREFS_BG_MUTE, _isBackgroundMuted ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetSFXVolumeSlider(float linearValue)
    {
        _lastSFXVolumeLinear = linearValue;
        if (!_isSFXMuted)
        {
            SetMixerVolume(SFX_MIXER_PARAM, _lastSFXVolumeLinear);
        }
        PlayerPrefs.SetFloat(PREFS_SFX_VOL, _lastSFXVolumeLinear);
        PlayerPrefs.Save();
    }

    public void ToggleSFXMute()
    {
        _isSFXMuted = !_isSFXMuted;
        SetMixerVolume(SFX_MIXER_PARAM, _isSFXMuted ? 0.0001f : _lastSFXVolumeLinear);
        PlayerPrefs.SetInt(PREFS_SFX_MUTE, _isSFXMuted ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetUIVolumeSlider(float linearValue)
    {
        _lastUIVolumeLinear = linearValue;
        if (!_isUIMuted)
        {
            SetMixerVolume(UI_MIXER_PARAM, _lastUIVolumeLinear);
        }
        PlayerPrefs.SetFloat(PREFS_UI_VOL, _lastUIVolumeLinear);
        PlayerPrefs.Save();
    }

    public void ToggleUIMute()
    {
        _isUIMuted = !_isUIMuted;
        SetMixerVolume(UI_MIXER_PARAM, _isUIMuted ? 0.0001f : _lastUIVolumeLinear);
        PlayerPrefs.SetInt(PREFS_UI_MUTE, _isUIMuted ? 1 : 0);
        PlayerPrefs.Save();
    }
    #endregion
}