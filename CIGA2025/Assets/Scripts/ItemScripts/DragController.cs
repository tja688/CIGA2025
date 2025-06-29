// DragController.cs

using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class DragController : MonoBehaviour
{
    [SerializeField] private GameObject arrowPrefab;
    private Camera mainCamera;
    private ArrowView currentArrow;
    private IDraggable draggedObject;
    private GameInput playerInputActions;

    // 【新增】用于存储拖拽物体的实际锚点Transform。
    // 它可以是物体本身，也可以是其特定的子对象（如Live2D的Parameters）。
    private Transform dragAnchor;

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
        if (draggedObject != null && (draggedObject as MonoBehaviour) == null)
        {
            Debug.Log("被拖拽的物体已在外部被销毁，自动清理拖拽状态。");
            HandleDraggedObjectDestroyed(); // 使用现有清理方法
            return;
        }

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

                        // --- 【核心修改点】---
                        // 在开始拖拽时，决定使用哪个Transform作为锚点
                        // ---------------------
                        // 检查被拖拽物体的标签是否为 "Live2d"
                        if (draggedObject.transform.CompareTag("Live2d"))
                        {
                            // 如果是，则尝试查找名为 "Parameters" 的子对象
                            Transform live2dAnchor = draggedObject.transform.Find("Parameters");
                            if (live2dAnchor != null)
                            {
                                // 找到了，就将它设为锚点
                                dragAnchor = live2dAnchor;
                                Debug.Log($"检测到Live2d模型 '{draggedObject.transform.name}'，已将锚点设置为其子对象 'Parameters'。");
                            }
                            else
                            {
                                // 如果没找到，打印一个警告，并使用物体本身的Transform作为备用方案
                                Debug.LogWarning($"Live2d模型 '{draggedObject.transform.name}' 没有找到名为 'Parameters' 的子对象作为锚点，将使用根对象位置。请检查模型结构。");
                                dragAnchor = draggedObject.transform;
                            }
                        }
                        else
                        {
                            dragAnchor = draggedObject.transform;
                        }

                        // 使用确定的锚点位置来更新箭头
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
                // 【修改点】清理所有拖拽相关引用
                ClearDragState();
            }
        }

        // 3. 处理拖拽过程与悬停状态
        if (draggedObject != null)
        {
            draggedObject.OnDrag(mouseWorldPos);
            // 【修改点】持续使用正确的锚点更新箭头
            currentArrow.UpdateArrow(dragAnchor.position, mouseWorldPos);
            CursorManager.Instance.SetGrab();
        }
        else
        {
            RaycastHit2D hit = Physics2D.Raycast(mouseWorldPos, Vector2.zero);
            if (hit.collider != null)
            {
                IDraggable draggable = hit.collider.GetComponent<IDraggable>();
                if (draggable != null && draggable.IsDraggable)
                {
                    CursorManager.Instance.SetGrab();
                }
                else
                {
                    CursorManager.Instance.SetDefault();
                }
            }
            else
            {
                CursorManager.Instance.SetDefault();
            }
        }
    }

    private void CancelDragIfNeeded()
    {
        if (draggedObject != null)
        {
            draggedObject.OnDragEnd();
            ClearDragState();
            CursorManager.Instance.SetDefault();
        }
    }

    private void HandleDraggedObjectDestroyed()
    {
        if (draggedObject != null)
        {
            draggedObject.OnWillBeDestroyed -= HandleDraggedObjectDestroyed;
        }
        ClearDragState();
        CursorManager.Instance.SetDefault();
    }
    
    // 【新增】一个统一的清理方法，用于清除所有与拖拽相关的状态
    private void ClearDragState()
    {
        if (currentArrow != null)
        {
            Destroy(currentArrow.gameObject);
            currentArrow = null;
        }
        draggedObject = null;
        dragAnchor = null; // 确保锚点引用也被清除
    }

    private Vector3 GetMouseWorldPosition()

    {

        Vector2 mouseScreenPos = playerInputActions.PlayerControl.Point.ReadValue<Vector2>();

        return mainCamera.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, mainCamera.nearClipPlane));

    }
    
    
    private void OnDestroy()
    {
        playerInputActions.PlayerControl.Disable();
        if (draggedObject != null)
        {
            draggedObject.OnWillBeDestroyed -= HandleDraggedObjectDestroyed;
        }
    }
}