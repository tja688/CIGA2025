using UnityEngine;
using UnityEngine.InputSystem; // 仍然需要此命名空间来访问回调上下文

/// <summary>
/// 拍照模式管理器（单例） - V2 (已集成 PlayerInputController)
/// 负责处理拍照模式的进入/退出、光标切换以及状态管理。
/// 输入事件通过 PlayerInputController 单例来订阅。
/// </summary>
public class PhotoModeManager : MonoBehaviour
{
    #region 单例模式 (Singleton)
    public static PhotoModeManager Instance { get; private set; }
    #endregion

    #region 公开状态 (Public State)
    public bool IsPhotoMode { get; set; }
    #endregion

    #region 光标设置 (Cursor Settings)
    [Header("光标设置 (Cursor Settings)")]
    [Tooltip("在摄像模式下要显示的自定义光标纹理")]
    [SerializeField] private Texture2D photoCursorTexture;
    [Tooltip("自定义光标的显示尺寸")]
    [SerializeField] private Vector2 photoCursorSize = new Vector2(64f, 64f);
    [Tooltip("自定义光标相对于真实鼠标位置的偏移量")]
    [SerializeField] private Vector2 photoCursorOffset;
    #endregion

    #region 调试 (Debug)
    [Header("调试 (Debug)")]
    [Tooltip("勾选后，进入摄像模式时会同时显示系统光标和自定义光标，便于调整偏移和大小。")]
    [SerializeField] private bool isDebugMode = false;
    #endregion

    #region Unity生命周期方法 (Lifecycle Methods)

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
            PlayerInputController.Instance.InputActions.PlayerControl.Shoot.performed += TogglePhotoModeViaInput;
        }
        else
        {
            Debug.LogError("PhotoModeManager: 未找到 PlayerInputController 实例，拍照模式的输入将无法工作！");
        }
    }

    private void OnDisable()
    {
        if (PlayerInputController.Instance != null)
        {
            PlayerInputController.Instance.InputActions.PlayerControl.Shoot.performed -= TogglePhotoModeViaInput;
        }
    }

    private void OnGUI()
    {
        if (IsPhotoMode && photoCursorTexture != null)
        {
            Vector2 mousePos = Event.current.mousePosition;
            Rect cursorRect = new Rect(
                mousePos.x + photoCursorOffset.x,
                mousePos.y + photoCursorOffset.y,
                photoCursorSize.x,
                photoCursorSize.y
            );
            GUI.depth = -999;
            GUI.DrawTexture(cursorRect, photoCursorTexture);
        }
    }

    #endregion

    #region 核心功能方法 (Core Methods)

    /// <summary>
    /// 响应输入事件的回调函数
    /// </summary>
    private void TogglePhotoModeViaInput(InputAction.CallbackContext context)
    {
        TogglePhotoMode();
    }

    /// <summary>
    /// 切换拍照模式的核心逻辑
    /// </summary>
    public void TogglePhotoMode()
    {
        IsPhotoMode = !IsPhotoMode;

        if (IsPhotoMode)
        {
            EnterPhotoMode();
        }
        else
        {
            ExitPhotoMode();
        }
    }

    private void EnterPhotoMode()
    {
        if (!isDebugMode)
        {
            Cursor.visible = false;
        }
        else
        {
            Cursor.visible = true;
        }
    }

    private void ExitPhotoMode()
    {
        Cursor.visible = true;
    }

    #endregion
}