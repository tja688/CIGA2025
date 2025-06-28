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
    private Vector2 onScreenPosition;
    private Vector2 offScreenPosition;
    private Canvas rootCanvas;

    private bool isInitialized = false;
    private bool isVisible = false;
    private Tweener currentTweeter;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        
        var canvasInParent = GetComponentInParent<Canvas>();
        if (canvasInParent)
        {
            rootCanvas = canvasInParent.rootCanvas;
        }

        if (!rootCanvas)
        {
            Debug.LogError("UIFlyInOut: 未找到根Canvas! 离屏位置计算可能不准确。请确保此UI元素在Canvas下。", this);
        }
        
        // 立即初始化位置信息，即使gameObject初始非激活，也能在后续激活时正确工作
        InitializePositions();

        if (initializeOffScreen)
        {
            rectTransform.anchoredPosition = offScreenPosition;
            isVisible = false;
        }
        else
        {
            isVisible = true;
        }
    }

    /// <summary>
    /// 【核心改动】使用更稳健的方式计算屏幕外位置，以适应Canvas Scaler。
    /// </summary>
    private void InitializePositions()
    {
        if (isInitialized || !rootCanvas) return;

        onScreenPosition = rectTransform.anchoredPosition;

        // 1. 获取Canvas的矩形和世界空间的四个角
        Rect canvasRect = rootCanvas.GetComponent<RectTransform>().rect;
        Vector3[] panelWorldCorners = new Vector3[4];
        rectTransform.GetWorldCorners(panelWorldCorners);

        // 2. 将世界空间的角转换为Canvas的本地坐标
        Vector2 panelMinInCanvas = rootCanvas.transform.InverseTransformPoint(panelWorldCorners[0]); // 左下角
        Vector2 panelMaxInCanvas = rootCanvas.transform.InverseTransformPoint(panelWorldCorners[2]); // 右上角

        float calculatedOffX = onScreenPosition.x;
        float calculatedOffY = onScreenPosition.y;

        switch (entryDirection)
        {
            case UIFlyDirection.Down: // 从上方进入 (目标位置在屏幕顶部之上)
                // 需要向上移动的距离 = Canvas顶部到面板顶部的距离 + 面板自身高度 + buffer
                // (Canvas顶部Y - 面板顶部Y) + (面板顶部Y - 面板底部Y)
                // = Canvas顶部Y - 面板底部Y
                calculatedOffY = onScreenPosition.y + (canvasRect.yMax - panelMinInCanvas.y) + offScreenBuffer;
                break;
            case UIFlyDirection.Up:   // 从下方进入 (目标位置在屏幕底部之下)
                // 需要向下移动的距离 = 面板底部到Canvas底部的距离 + 面板自身高度 + buffer
                // = 面板顶部Y - Canvas底部Y
                calculatedOffY = onScreenPosition.y - (panelMaxInCanvas.y - canvasRect.yMin) - offScreenBuffer;
                break;
            case UIFlyDirection.Left: // 从右方进入 (目标位置在屏幕右侧之外)
                calculatedOffX = onScreenPosition.x + (canvasRect.xMax - panelMinInCanvas.x) + offScreenBuffer;
                break;
            case UIFlyDirection.Right: // 从左方进入 (目标位置在屏幕左侧之外)
                calculatedOffX = onScreenPosition.x - (panelMaxInCanvas.x - canvasRect.xMin) - offScreenBuffer;
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
        // 如果在运行时才激活，确保位置被正确初始化
        if (!isInitialized)
        {
            InitializePositions();
            if (initializeOffScreen) rectTransform.anchoredPosition = offScreenPosition;
        }

        if (currentTweeter != null && currentTweeter.IsActive())
        {
            if ((Vector2)currentTweeter.PathGetPoint(1f) == onScreenPosition) return; // 正在飞入或已在目标点
            currentTweeter.Kill();
        } else if (isVisible) {
             return; // 已经静止在屏幕上
        }
        
        gameObject.SetActive(true);

        currentTweeter = rectTransform.DOAnchorPos(onScreenPosition, entryDuration)
            .SetEase(entryEase)
            .SetDelay(entryDelay)
            .OnComplete(() => {
                isVisible = true;
                currentTweeter = null;
            });
    }

    /// <summary>
    /// 动画隐藏UI元素（飞出）
    /// </summary>
    public void Hide()
    {
        if (!isInitialized)
        {
            InitializePositions();
        }

        if (currentTweeter != null && currentTweeter.IsActive())
        {
            if ((Vector2)currentTweeter.PathGetPoint(1f) == offScreenPosition) return; // 正在飞出或已在目标点
            currentTweeter.Kill();
        } else if (!isVisible) {
            return; // 已经静止在屏幕外
        }
        
        currentTweeter = rectTransform.DOAnchorPos(offScreenPosition, exitDuration)
            .SetEase(exitEase)
            .SetDelay(exitDelay)
            .OnComplete(() => {
                isVisible = false;
                currentTweeter = null;
                // 在隐藏后禁用GameObject是一个常见需求，可以取消下面的注释
                // if (initializeOffScreen)
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
        if (currentTweeter != null && currentTweeter.IsActive())
        {
            // 如果正在飞入，则反向飞出
            if ((Vector2)currentTweeter.PathGetPoint(1f) == onScreenPosition) Hide();
            // 如果正在飞出，则反向飞入
            else if ((Vector2)currentTweeter.PathGetPoint(1f) == offScreenPosition) Show();
        }
        else // 没有动画在进行
        {
            if (isVisible) Hide();
            else Show();
        }
    }
    
    [ContextMenu("Force Reinitialize Positions")]
    public void ForceReinitialize()
    {
        isInitialized = false;
        InitializePositions();
        if (initializeOffScreen)
        {
            rectTransform.anchoredPosition = isVisible ? onScreenPosition : offScreenPosition;
        }
        Debug.Log($"Positions reinitialized. On-screen: {onScreenPosition}, Off-screen: {offScreenPosition}");
    }
}