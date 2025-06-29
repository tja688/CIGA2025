// DragController.cs

using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class DragController : MonoBehaviour
{
    [Header("核心组件")]
    [SerializeField] private GameObject arrowPrefab;
    
    [Header("音效配置")]
    [Tooltip("拖拽物体时循环播放的音效。留空则不播放。")]
    [SerializeField] private AudioConfigSO dragLoopSound;
    [Tooltip("拖拽音效的淡入淡出时间")]
    [SerializeField] private float audioFadeTime = 0.1f;

    private Camera mainCamera;
    private ArrowView currentArrow;
    private IDraggable draggedObject;
    private GameInput playerInputActions;
    private Transform dragAnchor;
    
    // 用于存储当前播放的拖拽音效的轨道ID
    private int _dragAudioTrackId = -1;

    private void Awake()
    {
        mainCamera = Camera.main;
        playerInputActions = PlayerInputController.Instance.InputActions;
    }

    private void OnEnable()
    {
        playerInputActions.PlayerControl.Enable();
    }

    private void OnDisable()
    {
        playerInputActions.PlayerControl.Disable();
    }

    private void Update()
    {
        // 检查被拖拽的物体是否在外部被销毁
        if (draggedObject != null && (draggedObject as MonoBehaviour) == null)
        {
            Debug.Log("被拖拽的物体已在外部被销毁，自动清理拖拽状态。");
            HandleDraggedObjectDestroyed();
            return;
        }

        // 如果在拍照模式下，则取消任何正在进行的拖拽
        if (PhotoModeManager.Instance != null && PhotoModeManager.Instance.IsPhotoMode)
        {
            CancelDragIfNeeded();
            return;
        }

        Vector3 mouseWorldPos = GetMouseWorldPosition();

        // 1. 处理拖拽开始
        if (playerInputActions.PlayerControl.Click.WasPressedThisFrame())
        {
            if (draggedObject == null)
            {
                RaycastHit2D hit = Physics2D.Raycast(mouseWorldPos, Vector2.zero);
                if (hit.collider != null)
                {
                    IDraggable draggable = hit.collider.GetComponent<IDraggable>();
                    if (draggable != null && draggable.IsDraggable)
                    {
                        draggedObject = draggable;
                        draggedObject.OnDragStart();

                        // 开始播放拖拽音效
                        if (dragLoopSound != null && AudioManager.Instance != null)
                        {
                            _dragAudioTrackId = AudioManager.Instance.Play(dragLoopSound, true, audioFadeTime);
                        }
                        
                        // --- 锚点逻辑 ---
                        if (draggedObject.transform.CompareTag("Live2d"))
                        {
                            Transform live2dAnchor = draggedObject.transform.Find("Parameters");
                            if (live2dAnchor != null)
                            {
                                dragAnchor = live2dAnchor;
                                Debug.Log($"检测到Live2d模型 '{draggedObject.transform.name}'，已将锚点设置为其子对象 'Parameters'。");
                            }
                            else
                            {
                                Debug.LogWarning($"Live2d模型 '{draggedObject.transform.name}' 没有找到名为 'Parameters' 的子对象作为锚点，将使用根对象位置。请检查模型结构。");
                                dragAnchor = draggedObject.transform;
                            }
                        }
                        else
                        {
                            dragAnchor = draggedObject.transform;
                        }

                        // 实例化并更新拖拽箭头
                        GameObject arrowInstance = Instantiate(arrowPrefab);
                        currentArrow = arrowInstance.GetComponent<ArrowView>();
                        currentArrow.UpdateArrow(dragAnchor.position, mouseWorldPos);
                    }
                }
            }
        }

        // 2. 处理拖拽结束
        if (playerInputActions.PlayerControl.Click.WasReleasedThisFrame())
        {
            if (draggedObject != null)
            {
                draggedObject.OnDragEnd();
                ClearDragState(); // 清理状态
            }
        }

        // 3. 处理拖拽过程与悬停状态
        if (draggedObject != null)
        {
            // 正在拖拽
            draggedObject.OnDrag(mouseWorldPos);
            currentArrow.UpdateArrow(dragAnchor.position, mouseWorldPos);
            CursorManager.Instance.SetGrab(); // 设置为“抓取”光标
        }
        else
        {
            // 没有拖拽，处理悬停逻辑
            RaycastHit2D hit = Physics2D.Raycast(mouseWorldPos, Vector2.zero);
            if (hit.collider != null)
            {
                IDraggable draggable = hit.collider.GetComponent<IDraggable>();
                if (draggable != null && draggable.IsDraggable)
                {
                    CursorManager.Instance.SetGrab(); // 悬停在可拖拽物上，显示“抓取”光标
                }
                else
                {
                    CursorManager.Instance.SetDefault(); // 悬停在不可拖拽物上，显示默认光标
                }
            }
            else
            {
                CursorManager.Instance.SetDefault(); // 未悬停在任何物体上，显示默认光标
            }
        }
    }

    /// <summary>
    /// 如果需要，取消拖拽（例如进入拍照模式时）
    /// </summary>
    private void CancelDragIfNeeded()
    {
        if (draggedObject != null)
        {
            draggedObject.OnDragEnd();
            ClearDragState();
            CursorManager.Instance.SetDefault();
        }
    }

    /// <summary>
    /// 当检测到被拖拽的物体被销毁时，调用此方法清理状态
    /// </summary>
    private void HandleDraggedObjectDestroyed()
    {
        if (draggedObject != null)
        {
            draggedObject.OnWillBeDestroyed -= HandleDraggedObjectDestroyed;
        }
        ClearDragState();
        CursorManager.Instance.SetDefault();
    }
    
    /// <summary>
    /// 一个统一的清理方法，用于清除所有与拖拽相关的状态
    /// </summary>
    private void ClearDragState()
    {
        // 停止拖拽音效
        if (_dragAudioTrackId != -1 && AudioManager.Instance != null)
        {
            AudioManager.Instance.Stop(_dragAudioTrackId, audioFadeTime);
            _dragAudioTrackId = -1; // 重置ID
        }

        // 销毁箭头
        if (currentArrow != null)
        {
            Destroy(currentArrow.gameObject);
            currentArrow = null;
        }

        // 清除引用
        draggedObject = null;
        dragAnchor = null;
    }

    /// <summary>
    /// 获取鼠标在世界空间中的位置
    /// </summary>
    private Vector3 GetMouseWorldPosition()
    {
        Vector2 mouseScreenPos = playerInputActions.PlayerControl.Point.ReadValue<Vector2>();
        return mainCamera.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, mainCamera.nearClipPlane));
    }
    
    private void OnDestroy()
    {
        // 禁用输入并清理事件订阅
        playerInputActions.PlayerControl.Disable();
        if (draggedObject != null)
        {
            draggedObject.OnWillBeDestroyed -= HandleDraggedObjectDestroyed;
        }
    }
}