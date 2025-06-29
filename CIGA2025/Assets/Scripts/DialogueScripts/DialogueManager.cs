using UnityEngine;
using TMPro;
using Febucci.UI; // TextAnimator_TMP 所在的命名空间
using Febucci.UI.Core; // TypewriterCore 所在的命名K空间
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System;

/// <summary>
/// 对话管理器 (V8 - 轮询检查最终版)
/// 最终方案：放弃不可靠的事件系统，改用主动轮询 textAnimator.allLettersShown 属性来驱动对话。
/// </summary>
public class DialogueManager : MonoBehaviour
{
    #region Singleton & Initialization
    public static DialogueManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        Log("DialogueManager.Awake() - 初始化开始 (V8 - 轮询模式)。");

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
            if (dialogueCanvasGroup != null) dialogueCanvasGroup.alpha = 0;
            Log("UI面板已在Awake中强制隐藏。");
        }

        // 不再订阅任何事件
    }
    #endregion

    [Header("UI 组件引用")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private CanvasGroup dialogueCanvasGroup;

    [Header("Text Animator 控制器")]
    [Tooltip("【重要】用于触发打字机效果")]
    [SerializeField] private TypewriterCore typewriter;
    [Tooltip("【重要】用于检查动画状态")]
    [SerializeField] private TextAnimator_TMP textAnimator; // 需要 TextAnimator_TMP 来访问 allLettersShown 属性

    [Header("对话行为配置")]
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private float delayBetweenLines = 0.5f;

    [Header("调试")]
    [Tooltip("勾选后，将在控制台输出详细的诊断日志")]
    [SerializeField] private bool enableDebugLogging = true;

    private Queue<string> currentDialogueQueue = new Queue<string>();
    private bool isDialogueActive = false;

    public async UniTask ShowDialogue(string[] messages)
    {
        Log($"ShowDialogue() 被调用。当前是否正忙: {isDialogueActive}");
        if (isDialogueActive)
        {
            Log("对话系统正忙，新的对话请求被忽略。", true);
            return;
        }

        if (messages == null || messages.Length == 0)
        {
            Log("传入的对话内容为空，请求被忽略。", true);
            return;
        }

        currentDialogueQueue.Clear();
        foreach (var message in messages)
        {
            currentDialogueQueue.Enqueue(message);
        }
        Log($"已将 {messages.Length} 条消息入队。");

        // 启动一个统一的、从头管到尾的异步流程
        await ProcessDialogueQueueAsync();
    }

    /// <summary>
    /// 【核心】使用一个完整的异步方法来管理整个对话流程
    /// </summary>
    private async UniTask  ProcessDialogueQueueAsync()
    {
        isDialogueActive = true;
        Log("ProcessDialogueQueueAsync() - 对话序列开始，isDialogueActive = true。");

        // 1. UI 浮现
        dialoguePanel.SetActive(true);
        Log("UI面板渐变开始...");
        await FadeCanvasGroupAsync(0, 1, fadeDuration);
        Log("UI面板渐变完成。");

        // 2. 循环处理队列中的每一句话
        while (currentDialogueQueue.Count > 0)
        {
            string message = currentDialogueQueue.Dequeue();
            Log($"准备显示下一句 (剩余 {currentDialogueQueue.Count}句): '{message}'");
            
            typewriter.ShowText(message);
            Log("typewriter.ShowText() 已调用。现在开始轮询检查 textAnimator.allLettersShown 状态...");

            // 【关键的改动】
            // 不再等待事件，而是主动等待 allLettersShown 属性变为 true
            await UniTask.WaitUntil(() => textAnimator.allLettersShown);
            
            Log("【成功！】textAnimator.allLettersShown 已变为 true。当前句子播放完毕。");

            // 如果后面还有话，就执行句间延迟
            if (currentDialogueQueue.Count > 0)
            {
                if (delayBetweenLines > 0)
                {
                    Log($"句间延迟开始，等待 {delayBetweenLines} 秒...");
                    await UniTask.Delay(TimeSpan.FromSeconds(delayBetweenLines));
                    Log("句间延迟结束。");
                }
            }
        }

        // 3. 所有对话都已显示完毕，结束序列
        Log("对话队列为空，准备结束序列。");
        await UniTask.Delay(TimeSpan.FromSeconds(delayBetweenLines)); // 播放完最后一句后也停顿一下
        
        await FadeCanvasGroupAsync(1, 0, fadeDuration);
        dialoguePanel.SetActive(false);

        isDialogueActive = false;
    }

    private async UniTask FadeCanvasGroupAsync(float startAlpha, float endAlpha, float duration)
    {
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            if(dialogueCanvasGroup != null)
                dialogueCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);
            await UniTask.Yield();
        }
        if(dialogueCanvasGroup != null)
            dialogueCanvasGroup.alpha = endAlpha;
    }

    private void Log(string message, bool isWarning = false)
    {
        if (enableDebugLogging)
        {
            string prefix = "[DialogueManager V8] ";
            if (isWarning)
                Debug.LogWarning(prefix + message);
            else
                Debug.Log(prefix + message);
        }
    }
}