using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;

/// <summary>
/// 控制游戏结局演出的总控制器。
/// 监听游戏状态，在 GameOver 状态时启动演出流程。
/// </summary>
public class EndingCutsceneController : MonoBehaviour
{
    [Header("结局画面与对话配置")]
    [Tooltip("请将4个结局UI画面的GameObject按顺序拖到这里")]
    [SerializeField] private List<GameObject> endingPanels;

    [Tooltip("请按顺序填入对应每个画面的对话内容")]
    [SerializeField] private List<string> endingDialogues;

    [Header("演出参数")]
    [Tooltip("每个画面展示的间隔时间（秒）")]
    [SerializeField] private float displayDuration = 6.0f;

    private bool _hasPlayed = false;

    private void OnEnable()
    {
        // 订阅GameFlowManager的状态变化事件
        GameFlowManager.OnGameStateChanged += HandleGameStateChanged;
    }

    private void OnDisable()
    {
        // 在对象销毁时取消订阅，防止内存泄漏
        GameFlowManager.OnGameStateChanged -= HandleGameStateChanged;
    }

    private void Start()
    {
        // 游戏开始时，确保所有结局画面都是隐藏的
        foreach (var panel in endingPanels)
        {
            if (panel != null)
            {
                panel.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 处理游戏状态变化的委托方法
    /// </summary>
    private void HandleGameStateChanged(GameFlowManager.GameState newState)
    {
        // 当游戏状态变为GameOver，并且结局动画从未播放过时
        if (newState == GameFlowManager.GameState.GameOver && !_hasPlayed)
        {
            _hasPlayed = true; // 标记为已播放，防止重复触发
            PlayEndingCutsceneAsync().Forget(); // 启动结局演出，并忽略其Task
        }
    }

    /// <summary>
    /// 【核心】播放结局演出的异步方法
    /// </summary>
    private async UniTask PlayEndingCutsceneAsync()
    {
        Debug.Log("[EndingCutscene] 结局演出开始！");

        // 检查配置是否正确
        if (endingPanels.Count == 0 || endingPanels.Count != endingDialogues.Count)
        {
            Debug.LogError("[EndingCutscene] 结局画板或对话配置不正确！请检查endingPanels和endingDialogues的数量是否一致且不为零。");
            // 【修复】使用 return; 来提前退出一个 async UniTask 方法
            return;
        }

        // 依次展示每个结局画面和对话
        for (int i = 0; i < endingPanels.Count; i++)
        {
            GameObject currentPanel = endingPanels[i];
            string currentDialogue = endingDialogues[i];

            // 1. 激活当前的UI画面
            if (currentPanel != null)
            {
                Debug.Log($"[EndingCutscene] 显示画面 {i + 1}");
                currentPanel.SetActive(true);
            }

            // 2. 调用对话管理器显示对应的对话
            // 注意：我们将对话包装成一个单句的数组来传递
            if (DialogueManager.Instance != null && !string.IsNullOrEmpty(currentDialogue))
            {
                await DialogueManager.Instance.ShowDialogue(new string[] { currentDialogue });
            }

            // 3. 等待指定的秒数
            Debug.Log($"[EndingCutscene] 等待 {displayDuration} 秒...");
            await UniTask.Delay(TimeSpan.FromSeconds(displayDuration));

            // 4. 在展示下一个画面之前，隐藏当前画面
            if (currentPanel != null)
            {
                currentPanel.SetActive(false);
            }
        }

        Debug.Log("[EndingCutscene] 结局演出结束。");
        // 在这里，您可以添加演出结束后的逻辑，比如返回主菜单
        // GameFlowManager.Instance.ReturnToMenu();
    }
}