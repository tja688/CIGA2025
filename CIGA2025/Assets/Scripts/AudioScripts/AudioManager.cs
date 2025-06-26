// AudioManager.cs (Final Version with Debugging)
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;

[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    [Header("调试开关")]
    [Tooltip("勾选后，将在控制台打印详细的音频播放流程日志。")]
    public bool enableDebugLogging = false;

    #region Singleton & Pooling
    public static AudioManager Instance { get; private set; }
    private List<AudioSource> _audioSourcePool;
    private void Awake() { if (Instance != null) { Destroy(gameObject); return; } Instance = this; DontDestroyOnLoad(gameObject); InitializePool(15); }
    private void InitializePool(int size) { _audioSourcePool = new List<AudioSource>(); for (var i = 0; i < size; i++) CreatePooledAudioSource(); }
    private AudioSource GetPooledAudioSource() { var source = _audioSourcePool.FirstOrDefault(s => !s.gameObject.activeInHierarchy); if (source == null) source = CreatePooledAudioSource(); source.gameObject.SetActive(true); return source; }
    private AudioSource CreatePooledAudioSource() { var go = new GameObject("PooledAudioSource_" + _audioSourcePool.Count); go.transform.SetParent(transform); var source = go.AddComponent<AudioSource>(); source.playOnAwake = false; _audioSourcePool.Add(source); go.SetActive(false); return source; }
    #endregion

    #region Track Management
    private readonly Dictionary<int, Coroutine> _activeTracks = new Dictionary<int, Coroutine>();
    private readonly Dictionary<int, AudioSource> _activeSources = new Dictionary<int, AudioSource>();
    private readonly Dictionary<int, float> _trackTargetVolumes = new Dictionary<int, float>();
    private int _nextTrackId = 0;
    #endregion

    #region Public Core Methods
    public int Play(AudioConfigSO config, PlaybackSettings settings = default, float stopFadeOutDuration = 0f)
    {
        if (!config)
        {
            Log("<color=red><b>Play Error:</b> AudioConfigSO is null.</color>");
            return -1;
        }

        if (settings.Equals(default(PlaybackSettings)))
        {
            settings = PlaybackSettings.Default;
            Log("PlaybackSettings is default, applying AudioManager defaults.");
        }
        else if (settings.volumeMultiplier == 0.0f)
        {
            settings.volumeMultiplier = 1.0f;
            Log("PlaybackSettings.volumeMultiplier was 0, corrected to 1.0.");
        }

        var trackId = _nextTrackId++;
        var source = GetPooledAudioSource();
        _activeSources[trackId] = source;

        var settingsDesc = new StringBuilder();
        settingsDesc.Append($"Looping: {settings.isLooping}, ");
        settingsDesc.Append($"VolumeMultiplier: {settings.volumeMultiplier}, ");
        settingsDesc.Append($"FadeIn: {settings.fadeInDuration}s, ");
        settingsDesc.Append($"StopFadeOut: {stopFadeOutDuration}s");
        Log($"<color=cyan><b>Play Request (Track ID: {trackId}):</b></color> Config='{config.name}', Settings=[{settingsDesc}]");

        var coroutine = StartCoroutine(PlayRoutine(trackId, config, settings, source, stopFadeOutDuration));
        _activeTracks[trackId] = coroutine;

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

        if (fadeOutDuration > 0)
        {
            StartCoroutine(FadeAndPauseRoutine(source, fadeOutDuration));
        }
        else
        {
            source.Pause();
            Log($"Track {trackId} paused instantly.");
        }
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

        if (fadeInDuration > 0)
        {
            StartCoroutine(FadeAndResumeRoutine(source, fadeInDuration, targetVolume));
        }
        else
        {
            source.volume = targetVolume;
            source.UnPause();
            Log($"Track {trackId} resumed instantly to volume {targetVolume}.");
        }
    }

    public void Stop(int trackId, float fadeOutDuration = 0f)
    {
        Log($"<color=red><b>Stop Request (Track ID: {trackId}):</b></color> FadeOut: {fadeOutDuration}s");
        if (_activeTracks.TryGetValue(trackId, out var coroutine) && coroutine != null)
        {
            StopCoroutine(coroutine);
            Log($"PlayRoutine for track {trackId} stopped.");
        }

        if (_activeSources.TryGetValue(trackId, out var source))
        {
            if (fadeOutDuration > 0 && source.isPlaying)
            {
                StartCoroutine(FadeAndStopRoutine(source, fadeOutDuration));
            }
            else
            {
                ReturnSourceToPool(source);
            }
        }
        
        CleanUpTrack(trackId);
    }
    #endregion

    #region Coroutines
    private IEnumerator PlayRoutine(int trackId, AudioConfigSO config, PlaybackSettings settings, AudioSource source, float stopFadeOutDuration)
    {
        Log($"Track {trackId}: PlayRoutine started.");
        do
        {
            // ========================= 【核心修正】 =========================
            // 在播放下一个剪辑之前，显式调用 Stop()。
            // 这会强制 AudioSource 进入一个已知的干净状态，清除了上一次播放可能存在的任何状态惯性，
            // 从而确保下一次 Play() 指令能够被可靠地执行。这是解决短音效循环问题的关键。
            source.Stop();
            // ===================================================================

            var playbackParams = config.GetPlaybackParameters();
            if (playbackParams.Clip == null)
            {
                Log($"<color=orange>Track {trackId} Warning:</color> Clip is null, aborting routine.");
                break;
            }

            source.clip = playbackParams.Clip;
            source.outputAudioMixerGroup = playbackParams.MixerGroup;
            float targetVolume = playbackParams.Volume * settings.volumeMultiplier;
            _trackTargetVolumes[trackId] = targetVolume;

            Log($"Track {trackId}: Playing clip '{source.clip.name}' at target volume {targetVolume}.");
            source.Play();

            if (settings.fadeInDuration > 0)
            {
                yield return StartCoroutine(FadeRoutine(source, 0, targetVolume, settings.fadeInDuration, $"Track {trackId} FadeIn"));
            }
            else
            {
                source.volume = targetVolume;
            }

            Log($"Track {trackId}: Now waiting for clip to finish (robustly)...");
            float clipLength = source.clip.length;
            float timer = 0f; // 每次循环都从0开始计时

            while (timer < clipLength)
            {
                if (source.isPlaying)
                {
                    timer += Time.deltaTime;
                }

                if (!source.gameObject.activeInHierarchy || source.clip == null)
                {
                    Log($"Track {trackId}: Source was stopped externally. Exiting wait loop.");
                    yield break;
                }

                yield return null;
            }

            Log($"Track {trackId}: Clip '{source.clip.name}' finished.");

            // 在尝试下一次循环前，仍然保留这一帧的等待，作为双重保险
            if (settings.isLooping)
            {
                yield return null;
            }

        } while (settings.isLooping);

        Log($"Track {trackId}: PlayRoutine loop finished. Calling Stop with fade {stopFadeOutDuration}s.");
        Stop(trackId, stopFadeOutDuration);
    }
    private IEnumerator FadeRoutine(AudioSource source, float startVolume, float endVolume, float duration, string debugName)
    {
        Log($"<color=grey><i>Fade '{debugName}' started: {startVolume:F2} -> {endVolume:F2} over {duration}s.</i></color>");
        if (duration <= 0) { source.volume = endVolume; yield break; }
        var timer = 0f;
        while (timer < duration)
        {
            source.volume = Mathf.Lerp(startVolume, endVolume, timer / duration);
            timer += Time.deltaTime;
            yield return null;
        }
        source.volume = endVolume;
        Log($"<color=grey><i>Fade '{debugName}' finished.</i></color>");
    }

    private IEnumerator FadeAndStopRoutine(AudioSource source, float duration)
    {
        yield return StartCoroutine(FadeRoutine(source, source.volume, 0, duration, "FadeToStop"));
        ReturnSourceToPool(source);
    }
    
    private IEnumerator FadeAndPauseRoutine(AudioSource source, float duration)
    {
        yield return StartCoroutine(FadeRoutine(source, source.volume, 0, duration, "FadeToPause"));
        source.Pause();
        Log($"Source '{source.name}' paused after fade.");
    }
    
    private IEnumerator FadeAndResumeRoutine(AudioSource source, float duration, float targetVolume)
    {
        Log($"Source '{source.name}' resuming...");
        source.UnPause();
        yield return StartCoroutine(FadeRoutine(source, source.volume, targetVolume, duration, "FadeToResume"));
    }

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
        _activeTracks.Remove(trackId);
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
}