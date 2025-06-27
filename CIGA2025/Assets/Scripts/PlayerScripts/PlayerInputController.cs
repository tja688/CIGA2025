using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputController : MonoBehaviour
{
    // --- 单例模式实现 ---
    private static PlayerInputController _instance;
    public static PlayerInputController Instance
    {
        get
        {
            if (_instance) return _instance;
            _instance = FindObjectOfType<PlayerInputController>(); // 尝试查找现有实例
            if (_instance) return _instance;
            var singletonObject = new GameObject(nameof(PlayerInputController));
            _instance = singletonObject.AddComponent<PlayerInputController>();
            return _instance;
        }
    }

    public InputActions InputActions { get; private set; }

    private void Awake()
    {
        // --- 单例模式 Awake 处理 ---
        if (!_instance)
        {
            _instance = this;

        }
        else if (_instance != this)
        {
            Debug.LogWarning($"PlayerInputController: 场景中已存在实例 '{_instance.gameObject.name}'。正在销毁此重复实例 '{this.gameObject.name}'。");
            Destroy(this.gameObject);
            return; 
        }

        InputActions = new InputActions();
    }

    private void OnEnable()
    {
        // ActivateUIControls();
        ActivatePlayerControls();
    }

    private void OnDisable()
    {
        InputActions?.PlayerControlScheme
        InputActions?.UIControlScheme.Disable();
    }

    private void OnDestroy()
    {
        InputActions?.Dispose();
    }

    // --- 公开的 API 用于切换输入 Action Map ---
    /// <summary>
    /// 激活玩家控制相关的输入 (例如：移动、跳跃等)。
    /// 同时通常会禁用UI控制。
    /// </summary>
    public void ActivatePlayerControls()
    {
        if (InputActions == null) return;
        InputActions.PlayerControl.Enable();
        InputActions.UIControl.Disable();
    }

    /// <summary>
    /// 激活UI相关的输入 (例如：导航、确认、取消等)。
    /// 同时通常会禁用玩家控制。
    /// </summary>
    public void ActivateUIControls()
    {
        if (InputActions == null) return;
        InputActions.UIControl.Enable();
        InputActions.PlayerControl.Disable();
    }
    // --- API 结束 ---
    
}