// PhotoModeManager.cs
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PhotoModeManager : MonoBehaviour
{
    public static PhotoModeManager Instance { get; private set; }

    [Header("UI交互设置")]
    [Tooltip("需要临时禁用交互的UI画布上的GraphicRaycaster组件")]
    [SerializeField] private GraphicRaycaster uiRaycaster;
    
    public bool IsPhotoMode { get; private set; }

    // 【新增】用于防止激活对象后，在未松开空格键的情况下立即重新进入拍照模式
    private bool _isLockedAfterActivation = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        if (PlayerInputController.Instance != null)
        {
            PlayerInputController.Instance.InputActions.PlayerControl.Shoot.started += HandleShootStarted;
            PlayerInputController.Instance.InputActions.PlayerControl.Shoot.canceled += HandleShootCanceled;
        }
    }

    private void OnDisable()
    {
        if (PlayerInputController.Instance != null)
        {
            // 【核心改动】取消订阅
            PlayerInputController.Instance.InputActions.PlayerControl.Shoot.started -= HandleShootStarted;
            PlayerInputController.Instance.InputActions.PlayerControl.Shoot.canceled -= HandleShootCanceled;
        }
    }
    
    /// <summary>
    /// 【新增】处理按键按下（例如空格键）的逻辑
    /// </summary>
    private void HandleShootStarted(InputAction.CallbackContext context)
    {
        // 如果模式没有被锁定，则进入拍照模式
        if (!_isLockedAfterActivation)
        {
            EnterPhotoMode();
        }
    }

    /// <summary>
    /// 【新增】处理按键松开（例如空格键）的逻辑
    /// </summary>
    private void HandleShootCanceled(InputAction.CallbackContext context)
    {
        // 任何时候松开按键，都应该退出拍照模式，并解除锁定状态
        _isLockedAfterActivation = false;
        ExitPhotoMode();
    }

    /// <summary>
    /// 【新增】进入拍照模式的具体实现
    /// </summary>
    private void EnterPhotoMode()
    {
        if (IsPhotoMode) return; // 防止重复进入

        IsPhotoMode = true;
        
        if (CursorManager.Instance != null)
        {
            CursorManager.Instance.NotifyPhotoModeStatus(IsPhotoMode);
        }
        
        // 【核心改动】禁用UI射线检测，让按钮无法被点击
        if (uiRaycaster == null) return;
        uiRaycaster.enabled = false;
    }
    
    /// <summary>
    /// 【新增】退出拍照模式的具体实现
    /// </summary>
    private void ExitPhotoMode()
    {
        if (!IsPhotoMode) return; // 防止重复退出

        IsPhotoMode = false;

        if (CursorManager.Instance)
        {
            CursorManager.Instance.NotifyPhotoModeStatus(IsPhotoMode);
        }
        
        // 【核心改动】恢复UI射线检测，让按钮恢复正常
        if (!uiRaycaster) return;
        uiRaycaster.enabled = true;
    }

    /// <summary>
    /// 【新增】由外部（如 SelectionManager）调用，用于在激活对象后退出并锁定模式，
    /// 直到玩家松开空格键。
    /// </summary>
    public void DeactivateAndLock()
    {
        _isLockedAfterActivation = true;
        ExitPhotoMode();
    }
}