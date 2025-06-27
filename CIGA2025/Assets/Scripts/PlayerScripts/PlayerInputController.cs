using UnityEngine;
using UnityEngine.InputSystem;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class PlayerInputController : MonoBehaviour
{
    private static PlayerInputController _instance;
    private static bool isShuttingDown = false; 
    private static readonly object _lock = new object(); 

    public static PlayerInputController Instance
    {
        get
        {
            if (isShuttingDown)
            {
                Debug.LogWarning("[Singleton] Instance 'PlayerInputController' already destroyed. Returning null.");
                return null;
            }

            lock (_lock) 
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
                }
                return _instance;
            }
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
        if (_instance == this)
        {
            isShuttingDown = true; 
        }

        InputActions?.Dispose();
        
        if (_instance == this)
            _instance = null;
    }

    private void OnApplicationQuit()
    {
        isShuttingDown = true; 
        if (_instance == this) 
            _instance = null;
    }

    public void ActivatePlayerControls()
    {
        if (InputActions == null) return;
        InputActions.PlayerControl.Enable();
        InputActions.UIControl.Disable();
    }

    public void ActivateUIControls()
    {
        if (InputActions == null) return;
        InputActions.UIControl.Enable();
        InputActions.PlayerControl.Disable();
    }
}