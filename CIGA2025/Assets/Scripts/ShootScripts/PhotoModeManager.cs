// PhotoModeManager.cs
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PhotoModeManager : MonoBehaviour
{
    private static readonly int Live = Animator.StringToHash("Live");
    public static PhotoModeManager Instance { get; private set; }

    [Header("UI交互设置")]
    [Tooltip("需要临时禁用交互的UI画布上的GraphicRaycaster组件")]
    [SerializeField] private GraphicRaycaster uiRaycaster;
    
    [Header("视觉效果")]
    // 【修改】这里引用的应该是项目文件夹里的预制体，而不是场景里的对象
    [Tooltip("拍照时要生成的动画效果预制体")]
    [SerializeField] private GameObject livePhotoEffectPrefab;
    
    public Transform livePhotoEffectParent;
    
    // 【新增】用于持有当前生成的实例
    private GameObject _currentEffectInstance;
    private Animator _livePhotoAnimator; // 依然用来触发动画

    
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
        
        // 【核心修改】进入拍照模式时，生成(Instantiate)预制体
        if (livePhotoEffectPrefab != null)
        {
            // 生成实例
            _currentEffectInstance = Instantiate(livePhotoEffectPrefab, livePhotoEffectParent); 
            
            // 从新生成的实例上获取Animator
            _livePhotoAnimator = _currentEffectInstance.GetComponent<Animator>();
        }
        
        // 【核心改动】禁用UI射线检测，让按钮无法被点击
        if (uiRaycaster == null) return;
        uiRaycaster.enabled = false;
    }
    
    /// <summary>
    /// 给方法增加一个默认参数，用于控制是否立即隐藏动画效果
    /// </summary>
    private void ExitPhotoMode(bool hideEffectImmediately = true) // <--- 在这里增加参数
    {
        if (!IsPhotoMode) return;

        IsPhotoMode = false;

        if (CursorManager.Instance)
        {
            CursorManager.Instance.NotifyPhotoModeStatus(IsPhotoMode);
        }
        
        // 【核心修改】如果是取消拍照，则立即销毁(Destroy)实例
        if (hideEffectImmediately && _currentEffectInstance != null)
        {
            Destroy(_currentEffectInstance);
            _currentEffectInstance = null; // 销毁后清空引用，好习惯
        }

        if (!uiRaycaster) return;
        uiRaycaster.enabled = true;
    }

 
    /// <summary>
    /// 由外部（如 SelectionManager）调用，用于在激活对象后退出并锁定模式，
    /// 直到玩家松开空格键。
    /// </summary>
    public void DeactivateAndLock()
    {
        _isLockedAfterActivation = true;
        ExitPhotoMode(false);
    }

    /// <summary>
    /// 【新增】一个公共方法，供外部调用来触发 "Live" 动画
    /// </summary>
    public void TriggerLiveAnimation()
    {
        // 【修改】确保在实例上的Animator上触发
        if (_livePhotoAnimator != null)
        {
            _livePhotoAnimator.SetTrigger(Live);
        }
    }
}