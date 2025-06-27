using UnityEngine;
using UnityEngine.InputSystem;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class PlayerInputController : MonoBehaviour
{
    private static PlayerInputController _instance;
    public static PlayerInputController Instance
    {
        get
        {
            if (_instance != null) 
                return _instance;

            _instance = FindObjectOfType<PlayerInputController>();
            if (_instance != null) 
                return _instance;

            if (Application.isPlaying)
            {
                var singletonObject = new GameObject(nameof(PlayerInputController));
                _instance = singletonObject.AddComponent<PlayerInputController>();
                DontDestroyOnLoad(singletonObject);
            }
            return _instance;
        }
    }

    public GameInput InputActions { get; private set; }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Debug.LogWarning($"Duplicate PlayerInputController on '{name}', destroying.");
            Destroy(gameObject);
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

        if (_instance == this)
            _instance = null;
    }

    private void OnApplicationQuit()
    {
        if (_instance == this) 
            _instance = null;
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