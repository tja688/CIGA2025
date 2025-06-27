// PhotoModeManager.cs
using UnityEngine;
using UnityEngine.InputSystem;

public class PhotoModeManager : MonoBehaviour
{
    public static PhotoModeManager Instance { get; private set; }

    public bool IsPhotoMode { get; private set; } // 将 set 设为 private，只能通过方法切换

    // 【核心改动】所有与光标相关的 [SerializeField] 和 OnGUI 方法都已被移除

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
    }

    private void OnDisable()
    {
        if (PlayerInputController.Instance != null)
        {
            PlayerInputController.Instance.InputActions.PlayerControl.Shoot.performed -= TogglePhotoModeViaInput;
        }
    }

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

        // 【核心改动】不再自己操作 Cursor.visible，而是向 CursorManager 发送通知
        if (CursorManager.Instance != null)
        {
            CursorManager.Instance.NotifyPhotoModeStatus(IsPhotoMode);
        }
    }
}