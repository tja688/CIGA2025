using UnityEngine;
using TMPro;
using Febucci.UI;
using Febucci.UI.Core;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System;

/// <summary>
/// 对话管理器 (V9 - 音效集成版)
/// 在 V8 的基础上，增加了在对话期间循环播放指定音效的功能。
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

        Log("DialogueManager.Awake() - 初始化开始 (V9 - 音效集成版)。");

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
            if (dialogueCanvasGroup != null) dialogueCanvasGroup.alpha = 0;
            Log("UI面板已在Awake中强制隐藏。");
        }
    }
    #endregion

    [Header("UI 组件引用")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private CanvasGroup dialogueCanvasGroup;

    [Header("Text Animator 控制器")]
    [Tooltip("【重要】用于触发打字机效果")]
    [SerializeField] private TypewriterCore typewriter;
    [Tooltip("【重要】用于检查动画状态")]
    [SerializeField] private TextAnimator_TMP textAnimator;

    // --- 【新增】音效配置 ---
    [Header("音效配置")]
    [Tooltip("指定对话期间循环播放的音效。将其留空则不播放任何音效。")]
    [SerializeField] private AudioConfigSO dialogueLoopSound;
    [Tooltip("对话音效的淡入时间")]
    [SerializeField] private float audioFadeInDuration = 0.5f;
    [Tooltip("对话音效的淡出时间")]
    [SerializeField] private float audioFadeOutDuration = 0.5f;
    // --- 新增结束 ---

    [Header("对话行为配置")]
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private float delayBetweenLines = 0.5f;

    [Header("调试")]
    [Tooltip("勾选后，将在控制台输出详细的诊断日志")]
    [SerializeField] private bool enableDebugLogging = true;

    private Queue<string> currentDialogueQueue = new Queue<string>();
    private bool isDialogueActive = false;
    private int _dialogueAudioTrackId = -1; // 用于存储循环音效的轨道ID

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
    private async UniTask ProcessDialogueQueueAsync()
    {
        isDialogueActive = true;
        Log("ProcessDialogueQueueAsync() - 对话序列开始，isDialogueActive = true。");

        // 使用 try-finally 确保无论对话如何结束（正常完成、异常、取消），
        // 清理逻辑（停止音效、重置状态）都会被执行。
        try
        {
            // 【新增】在对话开始时，播放循环音效
            if (dialogueLoopSound != null && AudioManager.Instance != null)
            {
                // 调用AudioManager播放循环音效，并记录返回的Track ID
                _dialogueAudioTrackId = AudioManager.Instance.Play(
                    config: dialogueLoopSound, 
                    isLooping: true, 
                    fadeInDuration: audioFadeInDuration
                );
                Log($"已启动对话循环音效，Track ID: {_dialogueAudioTrackId}");
            }

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

                // 等待当前句子显示完成
                await UniTask.WaitUntil(() => textAnimator.allLettersShown);
                
                Log("【成功！】textAnimator.allLettersShown 已变为 true。当前句子播放完毕。");

                // 如果后面还有话，就执行句间延迟
                if (currentDialogueQueue.Count > 0 && delayBetweenLines > 0)
                {
                    Log($"句间延迟开始，等待 {delayBetweenLines} 秒...");
                    await UniTask.Delay(TimeSpan.FromSeconds(delayBetweenLines));
                    Log("句间延迟结束。");
                }
            }

            // 3. 所有对话都已显示完毕，准备结束
            Log("对话队列为空，准备结束序列。");
            await UniTask.Delay(TimeSpan.FromSeconds(delayBetweenLines)); // 播放完最后一句后也停顿一下
            
            await FadeCanvasGroupAsync(1, 0, fadeDuration);
            dialoguePanel.SetActive(false);
        }
        finally
        {
            // 4. 【新增】在对话结束时，停止循环音效
            if (_dialogueAudioTrackId != -1 && AudioManager.Instance != null)
            {
                Log($"准备停止对话循环音效，Track ID: {_dialogueAudioTrackId}");
                // 调用AudioManager停止之前播放的音效
                AudioManager.Instance.Stop(_dialogueAudioTrackId, audioFadeOutDuration);
                _dialogueAudioTrackId = -1; // 重置ID，防止重复停止
            }

            // 5. 重置状态
            isDialogueActive = false;
            Log("ProcessDialogueQueueAsync() - 对话序列结束，isDialogueActive = false。");
        }
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
            string prefix = "[DialogueManager V9] "; // 版本号更新
            if (isWarning)
                Debug.LogWarning(prefix + message);
            else
                Debug.Log(prefix + message);
        }
    }
}