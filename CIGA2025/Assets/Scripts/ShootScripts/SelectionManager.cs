using UnityEngine;
using UnityEngine.InputSystem;

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

    // 【新增】音效配置
    [Header("音效配置")]
    [Tooltip("成功激活一个物体时播放的“拍照”音效。")]
    [SerializeField] private AudioConfigSO activationSound;
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

        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        _currentSelectedObject = FindClosestSelectable(mouseWorldPos);

        if (_currentSelectedObject != null)
        {
            _selectionBoxInstance.transform.position = _currentSelectedObject.SelectionBounds.center;
            _selectionBoxInstance.UpdateBounds(_currentSelectedObject.SelectionBounds);
            _selectionBoxInstance.SetVisible(true);
            
            // 当玩家点击时
            if (_playerInputActions.PlayerControl.Click.WasPressedThisFrame())
            {
                // 【新增】播放激活音效
                if (activationSound != null && AudioManager.Instance != null)
                {
                    // 播放一次性音效，无需循环
                    AudioManager.Instance.Play(activationSound, false);
                }
                
                // 调用该物体的激活方法
                _currentSelectedObject.OnActivate();
                
                if (PhotoModeManager.Instance != null)
                {
                    PhotoModeManager.Instance.TriggerLiveAnimation();
                }
                
                BatteryManager.Instance.UseBattery(1);
                
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