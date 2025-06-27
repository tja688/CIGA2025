using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputController : MonoBehaviour
{
    private static PlayerInputController _instance;
    public static PlayerInputController Instance
    {
        get
        {
            if (_instance) return _instance;
            _instance = FindObjectOfType<PlayerInputController>(); 
            if (_instance) return _instance;
            var singletonObject = new GameObject(nameof(PlayerInputController));
            _instance = singletonObject.AddComponent<PlayerInputController>();
            return _instance;
        }
    }

    public GameInput InputActions { get; private set; }

    private void Awake()
    {
        if (!_instance)
        {
            _instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else if (_instance != this)
        {
            Debug.LogWarning($"PlayerInputController: 场景中已存在实例 '{_instance.gameObject.name}'。正在销毁此重复实例 '{this.gameObject.name}'。");
            Destroy(this.gameObject);
            return; 
        }

        InputActions = new GameInput();
    }

    private void OnEnable()
    {
        InputActions.PlayerControl.Enable();
        InputActions.UIControl.Enable();
    }

    private void OnDisable()
    {
        InputActions?.PlayerControl.Disable();
        InputActions?.UIControl.Disable();
    }

    private void OnDestroy()
    {
        InputActions?.Dispose();
    }

    /// <summary>
    /// 激活玩家控制相关的输入。
    /// 同时通常会禁用UI控制。
    /// </summary>
    public void ActivatePlayerControls()
    {
        if (InputActions == null) return;
        InputActions.PlayerControl.Enable();
        InputActions.UIControl.Disable();
    }

    /// <summary>
    /// 激活UI相关的输入。
    /// 同时通常会禁用玩家控制。
    /// </summary>
    public void ActivateUIControls()
    {
        if (InputActions == null) return;
        InputActions.UIControl.Enable();
        InputActions.PlayerControl.Disable();
    }
}