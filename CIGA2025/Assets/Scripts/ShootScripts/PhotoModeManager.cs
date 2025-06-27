using UnityEngine;
using UnityEngine.InputSystem; // 引入新的输入系统命名空间

/// <summary>
/// 拍照模式管理器（单例）
/// 负责处理拍照模式的进入/退出、光标切换以及状态管理
/// </summary>
public class PhotoModeManager : MonoBehaviour
{
    #region 单例模式 (Singleton)

    // 公开的静态实例，采用饿加载方式
    public static PhotoModeManager Instance { get; private set; }

    #endregion

    #region 公开状态 (Public State)
    
    [Tooltip("【只读】当前是否处于摄像模式。可供其他脚本读取。")]
    public bool IsPhotoMode { get; private set; }

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

    #region 私有变量 (Private Variables)

    private InputAction inputActions; // 输入控制器的实例

    #endregion

    #region Unity生命周期方法 (Lifecycle Methods)

    private void Awake()
    {
        // --- 单例模式实现 ---
        // 检查是否已有实例存在
        if (Instance != null && Instance != this)
        {
            // 如果有，则销毁这个重复的实例，确保全局唯一
            Destroy(gameObject);
            return;
        }
        // 如果没有，则将自身设为实例，并设置为切换场景时不销毁
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // --- 初始化输入 ---
        inputActions = new InputAction();
    }

    // private void OnEnable()
    // {
    //     // 启用 "Shoot" Action Map
    //     inputActions.Shoot.Enable();
    //     // 订阅 "Shoot" Action 的 performed 事件
    //     inputActions.Shoot.Shoot.performed += TogglePhotoModeViaInput;
    // }
    //
    // private void OnDisable()
    // {
    //     // 取消订阅，防止内存泄漏
    //     inputActions.Shoot.Shoot.performed -= TogglePhotoModeViaInput;
    //     // 禁用 Action Map
    //     inputActions.Shoot.Disable();
    // }

    /// <summary>
    /// 使用 OnGUI 在屏幕上绘制自定义光标。
    /// OnGUI 是一个适合做UI调试和立即模式GUI的函数。
    /// </summary>
    private void OnGUI()
    {
        // 仅在拍照模式下且设置了光标纹理时才绘制
        if (IsPhotoMode && photoCursorTexture != null)
        {
            // GUI.DrawTexture 使用的是GUI坐标系，其原点在左上角，与鼠标位置一致
            // 所以我们可以直接使用 Event.current.mousePosition
            Vector2 mousePos = Event.current.mousePosition;

            // 定义绘制光标的矩形区域，应用上我们设置的偏移和尺寸
            Rect cursorRect = new Rect(
                mousePos.x + photoCursorOffset.x,
                mousePos.y + photoCursorOffset.y,
                photoCursorSize.x,
                photoCursorSize.y
            );

            // 绘制纹理。确保鼠标不会被UI元素阻挡
            GUI.depth = -999; // 确保光标在最顶层渲染
            GUI.DrawTexture(cursorRect, photoCursorTexture);
        }
    }

    #endregion

    #region 核心功能方法 (Core Methods)

    /// <summary>
    /// 这是响应输入事件的回调函数。
    /// </summary>
    /// <param name="context">输入动作的回调上下文</param>
    private void TogglePhotoModeViaInput(InputAction.CallbackContext context)
    {
        TogglePhotoMode();
    }

    /// <summary>
    /// 切换拍照模式的核心逻辑。也可以被其他脚本手动调用。
    /// </summary>
    public void TogglePhotoMode()
    {
        // 翻转状态
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
        // 如果不是调试模式，就隐藏系统默认光标
        if (!isDebugMode)
        {
            Cursor.visible = false;
        }
        else
        {
            // 在调试模式下，保持系统光标可见，用于对比
            Cursor.visible = true;
        }
    }

    private void ExitPhotoMode()
    {
        // 退出拍照模式时，总是恢复系统光标的可见性
        Cursor.visible = true;
    }

    #endregion
}