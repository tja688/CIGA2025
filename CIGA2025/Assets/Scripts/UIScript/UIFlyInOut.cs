using System;
using UnityEngine;
using DG.Tweening; // 确保你已经导入了 DOTween 插件

public enum UIFlyDirection
{
    Down, // 从上方进入，向下移动到目标位置
    Up,   // 从下方进入，向上移动到目标位置
    Left, // 从右方进入，向左移动到目标位置
    Right // 从左方进入，向右移动到目标位置
}

[RequireComponent(typeof(RectTransform))]
public class UIFlyInOut : MonoBehaviour
{
    [Header("动画设置")]
    [Tooltip("UI元素飞入的方向 (指进入屏幕时的运动方向)")]
    public UIFlyDirection entryDirection = UIFlyDirection.Down;

    [Tooltip("飞入动画的时长（秒）")]
    public float entryDuration = 0.5f;

    [Tooltip("飞出动画的时长（秒）")]
    public float exitDuration = 0.5f;

    [Tooltip("飞入动画的缓动类型")]
    public Ease entryEase = Ease.OutExpo;

    [Tooltip("飞出动画的缓动类型")]
    public Ease exitEase = Ease.InExpo;

    [Tooltip("飞入动画的延迟时间（秒）")]
    public float entryDelay = 0f;

    [Tooltip("飞出动画的延迟时间（秒）")]
    public float exitDelay = 0f;

    [Header("初始化设置")]
    [Tooltip("如果为 true, UI元素在 Awake 时会立即移动到屏幕外准备飞入")]
    public bool initializeOffScreen = true;
    
    [Header("出场位置微调")]
    [Tooltip("将UI元素移出屏幕时，额外再移出的距离。用于确保完全移出屏幕，避免边缘残留。默认5个单位。")]
    public float offScreenBuffer = 5f; 

    private RectTransform rectTransform;
    private Vector2 onScreenPosition;  // UI元素在屏幕上的最终位置
    private Vector2 offScreenPosition; // UI元素在屏幕外用于动画的起始/结束位置
    private Canvas rootCanvas;         // 用于计算边界的根Canvas

    private bool isInitialized = false;
    private bool isVisible = false;    // 标记UI当前是否在屏幕上（动画完成后）
    private Tweener currentTweeter;    // 当前活动的Tweener

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        // 获取根Canvas，它用于精确计算屏幕边界
        // 通常，UI元素是某个Canvas的子对象
        var canvasInParent = GetComponentInParent<Canvas>();
        if (canvasInParent)
        {
            rootCanvas = canvasInParent.rootCanvas;
        }

        if (!rootCanvas)
        {
            Debug.LogError("UIFlyInOut: 未找到根Canvas! 离屏位置计算可能不准确。请确保此UI元素在Canvas下。", this);
            // 如果找不到，可以尝试用Screen.width/height做后备，但这对于不同Canvas设置可能不准
        }

        // 初始化位置信息（计算屏幕内和屏幕外的位置）
        InitializePositions();

        if (initializeOffScreen)
        {
            // 立即将UI元素移动到计算好的屏幕外位置
            rectTransform.anchoredPosition = offScreenPosition;
            // 初始状态认为是不可见的 (即使GameObject是Active的，但它在屏幕外)
            isVisible = false;
        }
        else
        {
            // 如果不预先移到屏幕外，则认为其初始状态是可见的（在其设计位置）
            isVisible = true;
        }
    }

    private void InitializePositions()
    {
        if (isInitialized) return;

        onScreenPosition = rectTransform.anchoredPosition;

        var canvasRect = rootCanvas ? rootCanvas.GetComponent<RectTransform>().rect : new Rect(0, 0, Screen.width, Screen.height);
        var panelSize = rectTransform.rect.size;
        var panelPivot = rectTransform.pivot;

        var calculatedOffX = onScreenPosition.x;
        var calculatedOffY = onScreenPosition.y;

        var canvasHalfWidth = canvasRect.width / 2f;
        var canvasHalfHeight = canvasRect.height / 2f;

        switch (entryDirection)
        {
            case UIFlyDirection.Down: // 从上方进入 (初始位置在Canvas顶部之外)
                // 面板的底部边缘 与 Canvas的顶部边缘 对齐, 再向上推一个 buffer
                calculatedOffY = canvasHalfHeight + (panelSize.y * panelPivot.y) + offScreenBuffer; // <--- 修改点
                break;
            case UIFlyDirection.Up:   // 从下方进入 (初始位置在Canvas底部之外)
                // 面板的顶部边缘 与 Canvas的底部边缘 对齐, 再向下推一个 buffer
                calculatedOffY = -canvasHalfHeight - (panelSize.y * (1 - panelPivot.y)) - offScreenBuffer; // <--- 修改点
                break;
            case UIFlyDirection.Left: // 从右方进入 (初始位置在Canvas右侧之外)
                // 面板的左侧边缘 与 Canvas的右侧边缘 对齐, 再向右推一个 buffer
                calculatedOffX = canvasHalfWidth + (panelSize.x * panelPivot.x) + offScreenBuffer; // <--- 修改点
                break;
            case UIFlyDirection.Right: // 从左方进入 (初始位置在Canvas左侧之外)
                // 面板的右侧边缘 与 Canvas的左侧边缘 对齐, 再向左推一个 buffer
                calculatedOffX = -canvasHalfWidth - (panelSize.x * (1 - panelPivot.x)) - offScreenBuffer; // <--- 修改点
                break;
        }
        offScreenPosition = new Vector2(calculatedOffX, calculatedOffY);
        isInitialized = true;
    }
    /// <summary>
    /// 动画显示UI元素（飞入）
    /// </summary>
    public void Show()
    {
        if (!isInitialized)
        {
            InitializePositions(); // 确保已初始化
            // 如果之前没有预设到屏幕外，且现在才初始化，需要手动设置一下初始位置
             if (!initializeOffScreen) rectTransform.anchoredPosition = offScreenPosition;
        }


        // 如果当前面板正在隐藏（或者已经隐藏但动画可能还在队列里），先杀死旧动画
        if (currentTweeter != null && currentTweeter.IsActive())
        {
            // 如果目标是offScreenPosition，说明是正在隐藏或已隐藏，可以打断
            if ((Vector2)currentTweeter.PathGetPoint(1f) == offScreenPosition)
            {
                 currentTweeter.Kill();
            }
            else if (rectTransform.anchoredPosition == onScreenPosition && isVisible) // 已经在屏幕上且可见
            {
                return; // 无需操作
            }
        }


        gameObject.SetActive(true); // 确保GameObject是激活的才能播放动画

        // Debug.Log($"Show: Animating from {rectTransform.anchoredPosition} to {onScreenPosition}");
        currentTweeter = rectTransform.DOAnchorPos(onScreenPosition, entryDuration)
            .SetEase(entryEase)
            .SetDelay(entryDelay)
            .OnComplete(() => {
                isVisible = true;
                currentTweeter = null;
                // Debug.Log("Show Complete. Now at: " + rectTransform.anchoredPosition);
            });
    }

    /// <summary>
    /// 动画隐藏UI元素（飞出）
    /// </summary>
    public void Hide()
    {
        if (!isInitialized)
        {
            InitializePositions(); // 确保已初始化
        }

        // 如果当前面板正在显示（或者已经显示但动画可能还在队列里），先杀死旧动画
        if (currentTweeter != null && currentTweeter.IsActive())
        {
             // 如果目标是onScreenPosition，说明是正在显示或已显示，可以打断
            if((Vector2)currentTweeter.PathGetPoint(1f) == onScreenPosition)
            {
                currentTweeter.Kill();
            }
            else if (rectTransform.anchoredPosition == offScreenPosition && !isVisible) // 已经在屏幕外且不可见
            {
                 // gameObject.SetActive(false); // 可选：如果需要隐藏后禁用
                return; // 无需操作
            }
        }

        // Debug.Log($"Hide: Animating from {rectTransform.anchoredPosition} to {offScreenPosition}");
        currentTweeter = rectTransform.DOAnchorPos(offScreenPosition, exitDuration)
            .SetEase(exitEase)
            .SetDelay(exitDelay)
            .OnComplete(() => {
                isVisible = false;
                currentTweeter = null;
                // Debug.Log("Hide Complete. Now at: " + rectTransform.anchoredPosition);
                // 根据需求，可以选择在隐藏动画完成后禁用GameObject
                // if (initializeOffScreen) // 或者根据其他逻辑
                // {
                //     gameObject.SetActive(false);
                // }
            });
    }

    /// <summary>
    /// 切换显示/隐藏状态
    /// </summary>
    public void Toggle()
    {
        // 一个更稳健的判断方法是检查目标位置或当前是否真的在屏幕上
        // isVisible只在动画完成后更新，所以动画过程中它可能不准确
        // 因此我们主要判断动画的目标或者最终的静止状态
        if (currentTweeter != null && currentTweeter.IsActive())
        {
            // 如果正在飞入，则反向飞出
            if ((Vector2)currentTweeter.PathGetPoint(1f) == onScreenPosition)
            {
                Hide();
            }
            // 如果正在飞出，则反向飞入
            else if ((Vector2)currentTweeter.PathGetPoint(1f) == offScreenPosition)
            {
                Show();
            }
        }
        else // 没有动画在进行
        {
            if (isVisible) // 如果当前是可见的（在屏幕上）
            {
                Hide();
            }
            else // 如果当前是不可见的（在屏幕外或初始隐藏）
            {
                Show();
            }
        }
    }

    /// <summary>
    /// (编辑器用) 如果在运行时更改了参数，可以强制重新计算位置
    /// </summary>
    [ContextMenu("Force Reinitialize Positions")]
    public void ForceReinitialize()
    {
        isInitialized = false;
        InitializePositions();
        if (initializeOffScreen && !isVisible) // 如果当前应该是隐藏的
        {
             rectTransform.anchoredPosition = offScreenPosition;
        } else if (isVisible) {
            rectTransform.anchoredPosition = onScreenPosition;
        }
        Debug.Log("Positions reinitialized. Off-screen: " + offScreenPosition + ", On-screen: " + onScreenPosition);
    }

}