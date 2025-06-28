// CursorManager.cs
using UnityEngine;

public class CursorManager : MonoBehaviour
{
    public static CursorManager Instance { get; private set; }

    #region 光标贴图与设置 (所有光标都在此配置)
    [Header("抓取光标 (Grab Cursor)")]
    [SerializeField] private Texture2D grabCursorTexture;
    [SerializeField] private Vector2 grabCursorSize = new Vector2(64, 64);
    [SerializeField] private Vector2 grabCursorOffset;

    [Header("拍照光标 (Photo Mode Cursor)")]
    [SerializeField] private Texture2D photoCursorTexture;
    [SerializeField] private Vector2 photoCursorSize = new Vector2(64, 64);
    [SerializeField] private Vector2 photoCursorOffset;
    #endregion

    #region 私有状态变量
    private Texture2D currentGameplayTexture; // 用于存储“抓取”或null（默认）
    private bool isPhotoModeActive = false; // 用于存储是否处于拍照模式
    #endregion

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        SetDefault();
    }

    private void OnGUI()
    {
        // 【核心】建立优先级：拍照模式 > 游戏内自定义 > 系统默认
        // 1. 检查是否处于拍照模式 (最高优先级)
        if (isPhotoModeActive && photoCursorTexture != null)
        {
            Cursor.visible = false;
            DrawCursor(photoCursorTexture, photoCursorSize, photoCursorOffset);
        }
        // 2. 如果不处于拍照模式，再检查是否有游戏内自定义光标（如抓取）
        else if (currentGameplayTexture != null)
        {
            Cursor.visible = false;
            DrawCursor(currentGameplayTexture, grabCursorSize, grabCursorOffset);
        }
        // 3. 如果以上都不是，则使用系统默认光标
        else
        {
            Cursor.visible = true;
        }
    }
    
    /// <summary>
    /// 一个统一的绘制方法，避免代码重复
    /// </summary>
    private void DrawCursor(Texture2D texture, Vector2 size, Vector2 offset)
    {
        Vector2 mousePos = Event.current.mousePosition;
        Rect cursorRect = new Rect(
            mousePos.x + offset.x,
            mousePos.y + offset.y,
            size.x,
            size.y
        );
        GUI.depth = -999;
        GUI.DrawTexture(cursorRect, texture);
    }

    #region 公开API (供其他脚本调用)
    
    /// <summary>
    /// PhotoModeManager 调用此方法来通知状态变更
    /// </summary>
    public void NotifyPhotoModeStatus(bool isPhotoMode)
    {
        isPhotoModeActive = isPhotoMode;
    }

    /// <summary>
    /// DragController 调用此方法来设置为默认的系统光标
    /// </summary>
    public void SetDefault()
    {
        currentGameplayTexture = null;
    }

    /// <summary>
    /// DragController 调用此方法来设置为自定义的“抓取”光标
    /// </summary>
    public void SetGrab()
    {
        currentGameplayTexture = grabCursorTexture;
    }
    #endregion
}