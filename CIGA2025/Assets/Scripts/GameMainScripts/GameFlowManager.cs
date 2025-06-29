// GameFlowManager.cs
using System;
using UnityEngine;

/// <summary>
/// 管理游戏的核心流程，例如游戏的开始、结束和退出。
/// 采用单例模式和状态机，方便全局调用和管理。
/// </summary>
public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance { get; private set; }

    public enum GameState
    {
        MainMenu,
        OpeningCutscene,
        Gameplay,
        GameOver
    }

    public static event Action<GameState> OnGameStateChanged;

    public GameState CurrentState { get; private set; }
    
    // --- 【新增】背景音乐设置 ---
    [Header("背景音乐设置 (BGM)")]
    [Tooltip("主菜单界面循环播放的音乐")]
    public AudioConfigSO mainMenuMusic;
    [Tooltip("主菜单界面循环播放的音乐")]
    public AudioConfigSO openingCutsceneMusic;
    [Tooltip("主要游戏环节（Boss战）循环播放的音乐")]
    public AudioConfigSO gameplayMusic;
    [Tooltip("游戏结束界面循环播放的音乐")]
    public AudioConfigSO gameOverMusic;
    
    // --- 【新增】用于追踪当前BGM音轨ID的变量 ---
    private int _currentBgmTrackId = -1;

    // --- 你原来的代码 ---
    public bool IsGaming = false; 
    public EffectController objectWithEffect;
    private string[] dialogueMessages = new string[] { /*...*/ };

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    private void Start()
    {
        UpdateGameState(GameState.MainMenu);
    }

    /// <summary>
    /// 【核心】更新游戏状态，并广播给所有监听者
    /// </summary>
    /// <param name="newState">新的游戏状态</param>
    public void UpdateGameState(GameState newState)
    {
        if (CurrentState == newState) return;

        CurrentState = newState;
        Debug.Log($"[GameFlowManager] 游戏状态切换到: {newState}");
        
        // 【新增】调用BGM处理方法
        HandleBgmTransition(newState);

        OnGameStateChanged?.Invoke(newState);
        
        IsGaming = (newState == GameState.Gameplay);
    }
    
    // --- 【新增】处理BGM切换的逻辑 ---
    private void HandleBgmTransition(GameState newState)
    {
        // 1. 如果当前有BGM正在播放，则平滑地停止它
        if (_currentBgmTrackId != -1 && AudioManager.Instance != null)
        {
            AudioManager.Instance.Stop(_currentBgmTrackId, 1.0f); // 使用1秒淡出
            _currentBgmTrackId = -1;
        }

        // 2. 根据新状态选择要播放的音乐
        AudioConfigSO musicToPlay = null;
        switch (newState)
        {
            case GameState.MainMenu:
                musicToPlay = mainMenuMusic;
                break;
            case GameState.Gameplay:
                musicToPlay = gameplayMusic;
                break;
            case GameState.GameOver:
                musicToPlay = gameOverMusic;
                break;
            case GameState.OpeningCutscene:
                musicToPlay = openingCutsceneMusic;
                break;
        }

        // 3. 如果为新状态配置了音乐，则循环播放它
        if (musicToPlay != null && AudioManager.Instance != null)
        {
            // 使用1.5秒淡入，并循环播放，然后保存新的BGM音轨ID
            _currentBgmTrackId = AudioManager.Instance.Play(musicToPlay, isLooping: true, fadeInDuration: 1.5f);
        }
    }
    
    private void Update()
    {
        if (CurrentState == GameState.MainMenu && Input.GetKeyDown(KeyCode.Space))
        {
            UpdateGameState(GameState.Gameplay);
        }
    }
    
    public void StartGame()
    {
        UpdateGameState(GameState.Gameplay);
    }

    public void ReturnToMenu()
    {
        UpdateGameState(GameState.MainMenu);
    }

    public void QuitGame()
    {
        Debug.Log("GameFlowManager: QuitGame() 方法被调用。");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}