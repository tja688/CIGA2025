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

    /// <summary>
    /// 定义游戏的核心状态
    /// MainMenu: 待机状态，在主菜单界面
    /// OpeningCutscene: 开局演出
    /// Gameplay: 游戏进行中（开战）
    /// Gameover: 游戏结束演出
    /// </summary>
    public enum GameState
    {
        MainMenu,
        OpeningCutscene,
        Gameplay,
        GameOver
    }

    /// <summary>
    /// 当游戏状态发生改变时触发的全局事件
    /// </summary>
    public static event Action<GameState> OnGameStateChanged;

    /// <summary>
    /// 当前游戏状态
    /// </summary>
    public GameState CurrentState { get; private set; }
    
    // --- 以下是你原来的代码，保留即可 ---
    public AudioConfigSO beginMusic;
    public bool IsGaming = false; // 这个变量可以废弃，或与新状态关联
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
        // 游戏启动时，进入“待机”状态
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
        
        // 广播事件，通知所有订阅者
        OnGameStateChanged?.Invoke(newState);

        // 你可以根据状态更新旧的 IsGaming 变量，以兼容其他可能用到它的逻辑
        IsGaming = (newState == GameState.Gameplay);
    }
    
    // --- 为了方便测试，我们加一个按键来开始游戏 ---
    private void Update()
    {
        // 当在主菜单时，按下空格键开始游戏（这个之后可以改成UI按钮调用）
        if (CurrentState == GameState.MainMenu && Input.GetKeyDown(KeyCode.Space))
        {
            // 在这里可以先进入开场演出，演出结束后再进入Gameplay
            // 为快速见效，我们直接跳到“开战”状态
            UpdateGameState(GameState.Gameplay);
        }

        // // --- 你原来的测试代码 ---
        // if (Input.GetKeyDown(KeyCode.L)) { /*...*/ }
        // if (Input.GetKeyDown(KeyCode.F)) { /*...*/ }
        // if (Input.GetKeyDown(KeyCode.K)) { /*...*/ }
    }
    
    // 你可以创建一些公共方法给UI按钮等调用
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