// GameFlowManager.cs
using System;
using UnityEngine;
using Cysharp.Threading.Tasks; // 【新增】为使用 .Forget() 添加 UniTask 命名空间

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
    
    [Header("背景音乐设置 (BGM)")]
    [Tooltip("主菜单界面循环播放的音乐")]
    public AudioConfigSO mainMenuMusic;
    
    [Header("背景音乐设置 (BGM)")]
    [Tooltip("主菜单界面循环播放的音乐")]
    public AudioConfigSO beginMenuMusic;
    
    [Tooltip("主要游戏环节（Boss战）循环播放的音乐")]
    public AudioConfigSO gameplayMusic;
    [Tooltip("游戏结束界面循环播放的音乐")]
    public AudioConfigSO gameOverMusic;
    
    private int _currentBgmTrackId = -1;

    public bool IsGaming = false; 
    public EffectController objectWithEffect;
    
    // dialogueMessages 变量现在可以考虑移除，因为对话已由 DialogueManager 统一处理
    // private string[] dialogueMessages = new string[] { /*...*/ };

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
        
        HandleBgmTransition(newState);

        OnGameStateChanged?.Invoke(newState);
        
        IsGaming = (newState == GameState.Gameplay);
    }
    
    private void HandleBgmTransition(GameState newState)
    {
        if (_currentBgmTrackId != -1 && AudioManager.Instance != null)
        {
            AudioManager.Instance.Stop(_currentBgmTrackId, 1.0f);
            _currentBgmTrackId = -1;
        }

        AudioConfigSO musicToPlay = null;
        switch (newState)
        {
            case GameState.MainMenu:
                musicToPlay = mainMenuMusic;
                break;
            case GameState.OpeningCutscene:
                musicToPlay = beginMenuMusic;
                break;
            case GameState.Gameplay:
                musicToPlay = gameplayMusic;
                break;
            case GameState.GameOver:
                musicToPlay = gameOverMusic;
                
                // --- 【新增代码】 ---
                // 当进入游戏结束状态时，触发对话
                if (DialogueManager.Instance != null)
                {
                    Log("检测到进入GameOver状态，正在触发胜利对话...");
                    var victoryDialogue = new string[] { "我们。。。胜利了吗？" };
                    // 调用对话系统的 ShowDialogue 方法，并使用 .Forget() 异步执行
                    DialogueManager.Instance.ShowDialogue(victoryDialogue).Forget();
                }
                else
                {
                    Log("DialogueManager 实例未找到，无法显示胜利对话！", true);
                }
                // --- 新增代码结束 ---
                
                break;
        }

        if (musicToPlay != null && AudioManager.Instance != null)
        {
            _currentBgmTrackId = AudioManager.Instance.Play(musicToPlay, isLooping: true, fadeInDuration: 1.5f);
        }
    }
    
    private void Update()
    {
        // if (CurrentState == GameState.MainMenu && Input.GetKeyDown(KeyCode.Space))
        // {
        //     UpdateGameState(GameState.Gameplay);
        // }
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

    // 辅助Log方法，方便统一管理日志输出
    private void Log(string message, bool isWarning = false)
    {
        string prefix = "[GameFlowManager] ";
        if (isWarning)
            Debug.LogWarning(prefix + message);
        else
            Debug.Log(prefix + message);
    }
}