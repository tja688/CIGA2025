using System;
using UnityEngine;

/// <summary>
/// 管理游戏的核心流程，例如游戏的开始、结束和退出。
/// 采用单例模式，方便全局调用。
/// </summary>
public class GameFlowManager : MonoBehaviour
{
    /// <summary>
    /// 全局唯一的 GameFlowManager 实例
    /// </summary>
    public static GameFlowManager Instance { get; private set; }
    
    public AudioConfigSO beginMusic;
    
    public bool IsGaming = false;
    
    public EffectController objectWithEffect; 

    private string[] dialogueMessages = new string[]
    {
        "你好，冒险者！<shake>欢迎来到这个世界。</shake>",
        "我这里有一些信息要告诉你。",
        "请注意，<wiggle>危险</wiggle>就潜伏在前方...<wait d=1>一定要小心！"
    };

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
        if (beginMusic != null)
            AudioManager.Instance.Play(beginMusic);
    }

    // 测试：按空格键触发抖动
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            Debug.Log("添加 0.5 的创伤值！");
            CameraManager.Instance.AddTrauma(0.5f);
        }
        
        // 按下 'F' 键，给对象施加 0.5 秒的负片效果
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (objectWithEffect != null)
            {
                objectWithEffect.TriggerInvertEffect(0.5f);
            }
        }
        
        if (Input.GetKeyDown(KeyCode.K))
        {
            if (DialogueManager.Instance)
            {
                DialogueManager.Instance.ShowDialogue(dialogueMessages);
            }
        }
        

    }

    /// <summary>
    /// 公开的退出游戏方法。
    /// 这个方法可以在任何地方通过 GameFlowManager.Instance.QuitGame() 调用。
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("GameFlowManager: QuitGame() 方法被调用。");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // 如果是构建好的游戏（PC, Mac, Linux 等），则退出应用程序
        Application.Quit();
#endif
    }
}