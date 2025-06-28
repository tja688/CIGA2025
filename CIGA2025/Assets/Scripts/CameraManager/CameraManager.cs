using UnityEngine;

/// <summary>
/// 一个好用的2D摄像机管理器，实现了基于创伤（Trauma）的摄像机抖动效果。
/// 设计为单例模式，方便在任何地方调用。
/// 算法灵感来源于 GDC 演讲 "Math for Game Programmers: Juicing Your Cameras With Math"。
/// </summary>
public class CameraManager : MonoBehaviour
{
    #region Singleton
    
    // 单例实例
    public static CameraManager Instance { get; private set; }

    private void Awake()
    {
        // 实现经典的单例模式
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // 如果你希望在切换场景时不销毁此管理器，可以取消下面这行注释
            // DontDestroyOnLoad(gameObject); 
        }
    }

    #endregion

    #region Shake Parameters

    [Header("摄像机抖动参数")]
    [Tooltip("抖动随时间衰减的速度")]
    [SerializeField] private float _traumaDecaySpeed = 1.0f;

    [Tooltip("位置抖动的最大偏移距离")]
    [SerializeField] private float _maxPositionOffset = 1.5f;
    
    [Tooltip("旋转抖动的最大角度")]
    [SerializeField] private float _maxRotationOffset = 10.0f;

    [Tooltip("抖动频率")]
    [SerializeField] private float _shakeFrequency = 25.0f;

    #endregion

    #region Private Fields
    
    // 摄像机原始位置，用于计算抖动后的位置
    private Vector3 _originalPosition;
    // 摄像机原始旋转，用于计算抖动后的旋转
    private Quaternion _originalRotation;

    // 当前的创伤值，范围在 [0, 1]
    private float _trauma = 0.0f;
    
    // Perlin Noise 的种子，确保每次运行的抖动模式不同
    private float _noiseSeedX;
    private float _noiseSeedY;
    private float _noiseSeedZ;

    private Camera _mainCamera;

    #endregion

    private void Start()
    {
        _mainCamera = Camera.main;
        if (_mainCamera == null)
        {
            Debug.LogError("场景中没有找到主摄像机 (MainCamera tag)！");
            return;
        }

        // 保存摄像机的初始状态
        _originalPosition = _mainCamera.transform.position;
        _originalRotation = _mainCamera.transform.rotation;

        // 生成随机种子
        _noiseSeedX = Random.value * 100f;
        _noiseSeedY = Random.value * 100f;
        _noiseSeedZ = Random.value * 100f;
    }

    private void Update()
    {
        if (_mainCamera == null) return;
        
        // 如果有创伤值，则进行抖动计算
        if (_trauma > 0)
        {
            // 随时间衰减创伤值
            _trauma = Mathf.Clamp01(_trauma - Time.deltaTime * _traumaDecaySpeed);

            // 计算抖动强度，使用 trauma 的平方或三次方会让抖动在低创伤值时更轻微，高创伤值时更剧烈
            float shakeIntensity = Mathf.Pow(_trauma, 2);

            // --- 位置抖动 ---
            // 使用 Perlin Noise 生成平滑的随机值
            float offsetX = _maxPositionOffset * shakeIntensity * (Mathf.PerlinNoise(_noiseSeedX, Time.time * _shakeFrequency) * 2 - 1);
            float offsetY = _maxPositionOffset * shakeIntensity * (Mathf.PerlinNoise(_noiseSeedY, Time.time * _shakeFrequency) * 2 - 1);

            _mainCamera.transform.position = _originalPosition + new Vector3(offsetX, offsetY, 0);

            // --- 旋转抖动 ---
            float rotationZ = _maxRotationOffset * shakeIntensity * (Mathf.PerlinNoise(_noiseSeedZ, Time.time * _shakeFrequency) * 2 - 1);
            _mainCamera.transform.rotation = _originalRotation * Quaternion.Euler(0, 0, rotationZ);
        }
        else
        {
            // 当没有抖动时，确保摄像机回到原始位置和旋转
            if (_mainCamera.transform.position != _originalPosition)
            {
                _mainCamera.transform.position = _originalPosition;
            }
            if (_mainCamera.transform.rotation != _originalRotation)
            {
                _mainCamera.transform.rotation = _originalRotation;
            }
        }
    }

    #region Public API

    /// <summary>
    /// 对外暴露的接口，用于增加摄像机抖动的创伤值。
    /// </summary>
    /// <param name="amount">要增加的创伤值，建议范围在 0.1 到 1.0 之间。</param>
    public void AddTrauma(float amount)
    {
        _trauma = Mathf.Clamp01(_trauma + amount);
    }

    /// <summary>
    /// (可选) 一个更传统的抖动接口，内部转换为增加创伤值。
    /// </summary>
    /// <param name="duration">抖动持续时间（秒）。</param>
    /// <param name="intensity">抖动强度 [0, 1]。这个值会直接设置为创伤值。</param>
    public void Shake(float duration, float intensity)
    {
        // 这里的转换可以根据你的需求调整。
        // 一个简单的实现是直接设置 trauma，并调整衰减速度以匹配持续时间。
        _trauma = Mathf.Clamp01(intensity);
        if (duration > 0)
        {
            _traumaDecaySpeed = 1.0f / duration;
        }
    }

    /// <summary>
    /// (可选) 重置摄像机到初始位置和旋转。
    /// </summary>
    public void ResetCamera()
    {
        _trauma = 0;
        _mainCamera.transform.position = _originalPosition;
        _mainCamera.transform.rotation = _originalRotation;
    }

    #endregion
}