using UnityEngine;
using Cysharp.Threading.Tasks; // 引入 UniTask
using System.Threading;

/// <summary>
/// 一个功能丰富的2D摄像机管理器。
/// 实现了基于创伤（Trauma）的摄像机抖动效果和基于 UniTask 的平滑运镜功能。
/// 设计为单例模式，方便在任何地方调用。
/// 抖动算法灵感来源于 GDC 演讲 "Math for Game Programmers: Juicing Your Cameras With Math"。
/// </summary>
public class CameraManager : MonoBehaviour
{
    #region Singleton
    
    public static CameraManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); 
        }
    }

    #endregion

    #region Parameters

    [Header("摄像机抖动参数")]
    [Tooltip("抖动随时间衰减的速度")]
    [SerializeField] private float _traumaDecaySpeed = 1.0f;
    [Tooltip("位置抖动的最大偏移距离")]
    [SerializeField] private float _maxPositionOffset = 1.5f;
    [Tooltip("旋转抖动的最大角度")]
    [SerializeField] private float _maxRotationOffset = 10.0f;
    [Tooltip("抖动频率")]
    [SerializeField] private float _shakeFrequency = 25.0f;

    [Header("摄像机运镜参数")]
    [Tooltip("镜头移动到目标所需的时间")]
    [SerializeField] private float _defaultMoveDuration = 1.0f;
    [Tooltip("镜头缩放所需的时间")]
    [SerializeField] private float _defaultZoomDuration = 0.5f;

    #endregion

    #region Private Fields
    
    // --- 初始状态 ---
    private Vector3 _initialPosition;
    private Quaternion _initialRotation;
    private float _initialOrthographicSize;

    // --- 动态基准状态 (由运镜任务更新) ---
    private Vector3 _basePosition;
    private Quaternion _baseRotation;
    private float _baseOrthographicSize;

    // --- 抖动状态 ---
    private float _trauma = 0.0f;
    private float _noiseSeedX, _noiseSeedY, _noiseSeedZ;

    // --- 运镜状态 ---
    private Transform _currentTarget = null;
    private CancellationTokenSource _focusCts;

    private Camera _mainCamera;

    #endregion

    private void Start()
    {
        _mainCamera = Camera.main;
        if (_mainCamera == null)
        {
            Debug.LogError("场景中没有找到主摄像机 (MainCamera tag)！");
            enabled = false; // 禁用此脚本
            return;
        }

        // 检查摄像机是否为正交
        if (!_mainCamera.orthographic)
        {
            Debug.LogWarning("警告: 摄像机不是正交模式 (Orthographic)。缩放功能可能无法按预期工作。");
        }

        // 保存摄像机的完整初始状态
        _initialPosition = _mainCamera.transform.position;
        _initialRotation = _mainCamera.transform.rotation;
        _initialOrthographicSize = _mainCamera.orthographicSize;

        // 初始化动态基准状态
        _basePosition = _initialPosition;
        _baseRotation = _initialRotation;
        _baseOrthographicSize = _initialOrthographicSize;

        // 生成随机种子
        _noiseSeedX = Random.value * 100f;
        _noiseSeedY = Random.value * 100f;
        _noiseSeedZ = Random.value * 100f;
        
        // 初始化任务取消令牌
        _focusCts = new CancellationTokenSource();
    }

    // 使用 LateUpdate 来应用最终的摄像机变换，确保所有逻辑（如玩家移动）都已执行完毕
    private void LateUpdate()
    {
        if (_mainCamera == null) return;
        
        Vector3 finalPosition = _basePosition;
        Quaternion finalRotation = _baseRotation;

        // 如果有创伤值，则在基准位置和旋转上应用抖动
        if (_trauma > 0)
        {
            _trauma = Mathf.Clamp01(_trauma - Time.deltaTime * _traumaDecaySpeed);
            float shakeIntensity = Mathf.Pow(_trauma, 2);

            float time = Time.time * _shakeFrequency;
            float offsetX = _maxPositionOffset * shakeIntensity * (Mathf.PerlinNoise(_noiseSeedX, time) * 2 - 1);
            float offsetY = _maxPositionOffset * shakeIntensity * (Mathf.PerlinNoise(_noiseSeedY, time) * 2 - 1);
            float rotationZ = _maxRotationOffset * shakeIntensity * (Mathf.PerlinNoise(_noiseSeedZ, time) * 2 - 1);

            finalPosition += new Vector3(offsetX, offsetY, 0);
            finalRotation *= Quaternion.Euler(0, 0, rotationZ);
        }
        
        // 应用所有计算后的最终变换
        _mainCamera.transform.position = finalPosition;
        _mainCamera.transform.rotation = finalRotation;
        _mainCamera.orthographicSize = _baseOrthographicSize;
    }

    private void OnDestroy()
    {
        // 组件销毁时，取消并释放 CancellationTokenSource
        _focusCts?.Cancel();
        _focusCts?.Dispose();
    }
    
    /// <summary>
    /// 异步地将摄像机平滑地重置回其初始位置和大小。
    /// </summary>
    public async UniTask ResetCameraAsync(float moveDuration = -1, float zoomDuration = -1)
    {
        _focusCts?.Cancel();
        _focusCts = new CancellationTokenSource();
        var token = _focusCts.Token;

        _currentTarget = null;
    
        float finalMoveDuration = moveDuration >= 0 ? moveDuration : _defaultMoveDuration;
        float finalZoomDuration = zoomDuration >= 0 ? zoomDuration : _defaultZoomDuration;
    
        // 同时进行移动和缩放
        await UniTask.WhenAll(
            PanAsync(_initialPosition, finalMoveDuration, token),
            ZoomAsync(_initialOrthographicSize, finalZoomDuration, token)
        );
    }


    #region Public API - Shake

    /// <summary>
    /// 增加摄像机抖动的创伤值。
    /// </summary>
    /// <param name="amount">要增加的创伤值，建议范围在 0.1 到 1.0 之间。</param>
    public void AddTrauma(float amount)
    {
        _trauma = Mathf.Clamp01(_trauma + amount);
    }

    #endregion

    #region Public API - Camera Movement

    /// <summary>
    /// 异步地将摄像机聚焦于一个目标。
    /// </summary>
    /// <param name="target">要聚焦的目标 Transform。如果为 null，则返回初始位置。</param>
    /// <param name="targetSize">聚焦时的正交大小。</param>
    /// <param name="moveDuration">移动过程的持续时间。小于0则使用默认值。</param>
    /// <param name="zoomDuration">缩放过程的持续时间。小于0则使用默认值。</param>
    public async UniTask FocusOnTargetAsync(Transform target, float targetSize, float moveDuration = -1, float zoomDuration = -1)
    {
        // 取消上一个正在进行的运镜任务
        _focusCts?.Cancel();
        _focusCts = new CancellationTokenSource();
        var token = _focusCts.Token;

        // 如果目标没有变化，则不执行任何操作
        if (target == _currentTarget) return;

        float finalMoveDuration = moveDuration >= 0 ? moveDuration : _defaultMoveDuration;
        float finalZoomDuration = zoomDuration >= 0 ? zoomDuration : _defaultZoomDuration;
        
        // --- 逻辑分支 ---

        // 1. 从一个目标切换到另一个目标
        if (target != null && _currentTarget != null)
        {
            // 流程：先缩放回初始大小 -> 再移动到新目标 -> 最后缩放到目标大小
            await ZoomAsync(_initialOrthographicSize, finalZoomDuration, token);
            if (token.IsCancellationRequested) return;
            
            await PanAsync(GetTargetPosition(target), finalMoveDuration, token);
            if (token.IsCancellationRequested) return;

            await ZoomAsync(targetSize, finalZoomDuration, token);
        }
        // 2. 从初始位置移动到新目标
        else if (target != null && _currentTarget == null)
        {
            // 流程：先移动到目标 -> 再缩放
            await PanAsync(GetTargetPosition(target), finalMoveDuration, token);
            if (token.IsCancellationRequested) return;

            await ZoomAsync(targetSize, finalZoomDuration, token);
        }
        // 3. 从一个目标返回初始位置
        else if (target == null && _currentTarget != null)
        {
            // 流程：先缩放回初始大小 -> 再移动回初始位置
            await ZoomAsync(_initialOrthographicSize, finalZoomDuration, token);
            if (token.IsCancellationRequested) return;

            await PanAsync(_initialPosition, finalMoveDuration, token);
        }
        
        _currentTarget = target;
    }

    /// <summary>
    /// 重置摄像机，立即回到初始状态并停止所有运镜。
    /// </summary>
    public void ResetCamera()
    {
        // 取消任何正在进行的运镜任务
        _focusCts?.Cancel();
        
        _trauma = 0;
        _currentTarget = null;
        
        _basePosition = _initialPosition;
        _baseRotation = _initialRotation;
        _baseOrthographicSize = _initialOrthographicSize;
    }

    #endregion

    #region Private Helpers

    // 平滑平移摄像机
    public async UniTask PanAsync(Vector3 targetPosition, float duration, CancellationToken token)
    {
        float elapsedTime = 0f;
        Vector3 startPosition = _basePosition;

        while (elapsedTime < duration)
        {
            // 检查任务是否已被取消
            if (token.IsCancellationRequested) return;

            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            // 使用 SmoothStep 实现缓动效果
            float easedT = Mathf.SmoothStep(0f, 1f, t); 
            
            _basePosition = Vector3.Lerp(startPosition, targetPosition, easedT);

            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }

        _basePosition = targetPosition; // 确保最终位置精确
    }

    // 平滑缩放摄像机
    public async UniTask ZoomAsync(float targetSize, float duration, CancellationToken token)
    {
        float elapsedTime = 0f;
        float startSize = _baseOrthographicSize;

        while (elapsedTime < duration)
        {
            if (token.IsCancellationRequested) return;

            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            float easedT = Mathf.SmoothStep(0f, 1f, t);

            _baseOrthographicSize = Mathf.Lerp(startSize, targetSize, easedT);
            
            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }

        _baseOrthographicSize = targetSize; // 确保最终大小精确
    }
    
    // 获取目标的最终摄像机位置 (Z轴保持初始值)
    private Vector3 GetTargetPosition(Transform target)
    {
        return new Vector3(target.position.x, target.position.y, _initialPosition.z);
    }

    #endregion
}