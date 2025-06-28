// DragController.cs
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
        if (PhotoModeManager.Instance != null && PhotoModeManager.Instance.IsPhotoMode)
        {
            CancelDragIfNeeded();
        }

        Vector3 mouseWorldPos = GetMouseWorldPosition();
        
        // 1. 处理拖拽开始
        if (playerInputActions.PlayerControl.Click.WasPressedThisFrame())
        {
            if (draggedObject == null)
            {
                RaycastHit2D hit = Physics2D.Raycast(mouseWorldPos, Vector2.zero);
                IDraggable draggable = hit.collider?.GetComponent<IDraggable>();

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
            // 【优化】悬停检查也应考虑 IsDraggable
            IDraggable draggable = hit.collider?.GetComponent<IDraggable>();
            if (draggable != null && draggable.IsDraggable)
            {
                CursorManager.Instance.SetGrab();
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

    private Vector3 GetMouseWorldPosition()
    {
        Vector2 mouseScreenPos = playerInputActions.PlayerControl.Point.ReadValue<Vector2>();
        return mainCamera.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, mainCamera.nearClipPlane));
    }
}