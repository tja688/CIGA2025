// DragController.cs

using System;
using UnityEngine;
using UnityEngine.InputSystem; // 仍然需要引用

public class DragController : MonoBehaviour
{
    [SerializeField] private GameObject arrowPrefab; 
    private Camera mainCamera;
    private ArrowView currentArrow;
    private IDraggable draggedObject;
    private GameInput playerInputActions;

    private void Awake()
    {
        mainCamera = Camera.main;
        // 在开始时就获取一次引用，避免在Update中反复访问单例
        playerInputActions = PlayerInputController.Instance.InputActions;
    }

    private void OnEnable()
    {
        // 【核心改动】不再订阅C#事件，只是确保Action Map是启用的
        playerInputActions.PlayerControl.Enable();
    }

    private void OnDisable()
    {
        playerInputActions.PlayerControl.Disable();
    }

private void Update()
    {
        // 【核心改动】在每次更新的开始，首先检查正在拖拽的对象是否已被销毁。
        // 这是为了处理物体在拖拽过程中因碰撞等原因被销毁的边界情况。
        if (draggedObject != null && (draggedObject as MonoBehaviour) == null)
        {
            Debug.Log("被拖拽的物体已在外部被销毁，自动清理拖拽状态。");

            // 如果箭头还存在，销毁它
            if (currentArrow != null)
            {
                Destroy(currentArrow.gameObject);
                currentArrow = null;
            }

            // 清理对已销毁对象的引用，并将光标设为默认
            draggedObject = null;
            CursorManager.Instance.SetDefault();

            // 清理完毕，立即结束本帧的Update，防止后续代码因对象不存在而报错
            return;
        }

        // --- 以下是你的原始逻辑，保持不变 ---

        if (PhotoModeManager.Instance != null && PhotoModeManager.Instance.IsPhotoMode)
        {
            CancelDragIfNeeded();
            // 在拍照模式下，不执行任何拖拽逻辑
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
                        
                        GameObject arrowInstance = Instantiate(arrowPrefab);
                        currentArrow = arrowInstance.GetComponent<ArrowView>();
                        currentArrow.UpdateArrow(draggedObject.transform.position, mouseWorldPos);
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
                draggedObject = null;
                Destroy(currentArrow.gameObject);
                currentArrow = null;
            }
        }

        // 3. 处理拖拽过程与悬停状态
        if (draggedObject != null)
        {
            draggedObject.OnDrag(mouseWorldPos);
            currentArrow.UpdateArrow(draggedObject.transform.position, mouseWorldPos);
            CursorManager.Instance.SetGrab();
        }
        else
        {
            RaycastHit2D hit = Physics2D.Raycast(mouseWorldPos, Vector2.zero);
            // 悬停检查也应考虑 IsDraggable
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
    // 如果拍照模式开启，强制取消拖拽
    private void CancelDragIfNeeded()
    {
        if (draggedObject != null)
        {
            draggedObject.OnDragEnd();
            draggedObject = null;
            Destroy(currentArrow.gameObject);
            currentArrow = null;
            CursorManager.Instance.SetDefault();
        }
    }

    // 【新增】事件处理方法
    private void HandleDraggedObjectDestroyed()
    {
        
        if(draggedObject != null)
        {
            draggedObject.OnWillBeDestroyed -= HandleDraggedObjectDestroyed;
        }

        if (currentArrow != null)
        {
            Destroy(currentArrow.gameObject);
            currentArrow = null;
        }

        draggedObject = null;
        CursorManager.Instance.SetDefault();
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