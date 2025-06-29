using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 负责处理游戏中“可选择对象”的框选逻辑和激活逻辑。
/// 它会根据鼠标位置，在指定的层上查找实现了 ISelectable 接口的对象。
/// 此管理器的核心逻辑仅在 PhotoModeManager 的拍照模式下激活。
/// </summary>
public class SelectionManager : MonoBehaviour
{
    #region 单例模式 (Singleton)
    public static SelectionManager Instance { get; private set; }
    #endregion

    #region Inspector 设置
    [Header("核心配置")]
    [Tooltip("带控制器脚本的框选框预制体")]
    [SerializeField] private SelectionBoxController selectionBoxPrefab;
    [Tooltip("鼠标检测范围")]
    [SerializeField] private float selectionRadius = 1f;
    #endregion

    private SelectionBoxController _selectionBoxInstance;
    private ISelectable _currentSelectedObject;
    private GameInput _playerInputActions;

    #region Unity 生命周期
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _playerInputActions = PlayerInputController.Instance.InputActions;

        if (selectionBoxPrefab != null)
        {
            _selectionBoxInstance = Instantiate(selectionBoxPrefab);
            _selectionBoxInstance.name = "SelectionBox_Instance";
            _selectionBoxInstance.SetVisible(false);
        }
        else
        {
            Debug.LogError("SelectionManager: 未配置 selectionBoxPrefab！框选功能将无法工作。");
        }
    }
    
    private void OnEnable()
    {
        _playerInputActions.PlayerControl.Enable();
    }

    private void OnDisable()
    {
        _playerInputActions.PlayerControl.Disable();
    }

    private void Update()
    {
        if (!PhotoModeManager.Instance || !PhotoModeManager.Instance.IsPhotoMode)
        {
            if (_currentSelectedObject != null)
            {
                _currentSelectedObject = null;
                _selectionBoxInstance.SetVisible(false);
            }
            return;
        }

        // --- 核心逻辑 ---

        // 第1步：检测鼠标下方是否有可选择的对象
        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        _currentSelectedObject = FindClosestSelectable(mouseWorldPos);

        // 第2步：根据当前是否选中对象，更新框选框状态
        if (_currentSelectedObject != null)
        {
            _selectionBoxInstance.transform.position = _currentSelectedObject.SelectionBounds.center;
            _selectionBoxInstance.UpdateBounds(_currentSelectedObject.SelectionBounds);
            _selectionBoxInstance.SetVisible(true);
            
            if (_playerInputActions.PlayerControl.Click.WasPressedThisFrame())
            {
                // 调用该物体的激活方法
                _currentSelectedObject.OnActivate();
                
                // 【核心改动】通知 PhotoModeManager 播放 "Live" 动画
                if (PhotoModeManager.Instance != null)
                {
                    PhotoModeManager.Instance.TriggerLiveAnimation();
                }
                
                // 拍照使用一个电池
                BatteryManager.Instance.UseBattery(1);
                
                // 立即退出拍照模式并设置锁定，防止立即重入
                if (PhotoModeManager.Instance != null)
                {
                    PhotoModeManager.Instance.DeactivateAndLock();
                }
            }
        }
        else
        {
            _selectionBoxInstance.SetVisible(false);
        }
    }
    #endregion

    #region 私有辅助方法
    /// <summary>
    /// 在指定位置附近查找最近的、实现了 ISelectable 接口的对象。
    /// </summary>
    /// <param name="searchPosition">搜索的中心点</param>
    /// <returns>找到的 ISelectable 对象，如果没找到则返回 null</returns>
    private ISelectable FindClosestSelectable(Vector2 searchPosition)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(searchPosition, selectionRadius);
        ISelectable closest = null;
        float minDistance = float.MaxValue;

        foreach (var hit in hits)
        {
            ISelectable selectable = hit.GetComponent<ISelectable>();

            if (selectable != null && selectable.IsSelectionEnabled)
            {
                float distance = Vector2.Distance(searchPosition, hit.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closest = selectable;
                }
            }
        }
        return closest;
    }
    #endregion
}