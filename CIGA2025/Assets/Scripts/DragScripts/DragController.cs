using UnityEngine;
using UnityEngine.InputSystem;

public class DragController : MonoBehaviour
{
    [SerializeField] private GameObject arrowPrefab; // 箭头预制体，保持不变

    private Camera mainCamera;
    
    private ArrowView currentArrow;
    private bool isDragging = false;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void OnEnable()
    {

        if (PlayerInputController.Instance != null)
        {
            PlayerInputController.Instance.InputActions.PlayerControl.Click.started += OnClickStarted;
            PlayerInputController.Instance.InputActions.PlayerControl.Click.canceled += OnClickCanceled;
        }
        else
        {
            Debug.LogError("PlayerInputController instance not found!");
        }
    }

    private void OnDisable()
    {
        if (PlayerInputController.Instance == null) return;
        PlayerInputController.Instance.InputActions.PlayerControl.Click.started -= OnClickStarted;
        PlayerInputController.Instance.InputActions.PlayerControl.Click.canceled -= OnClickCanceled;
    }

    private void Update()
    {
        if (isDragging && currentArrow)
        {
            currentArrow.UpdateArrow(GetMouseWorldPosition());
        }
    }

    private void OnClickStarted(InputAction.CallbackContext context)
    {
        Debug.Log("<color=lime>OnClickStarted 事件被成功触发了!</color>");

        
        var mousePos = PlayerInputController.Instance.InputActions.PlayerControl.Point.ReadValue<Vector2>();
        var hit = Physics2D.Raycast(mainCamera.ScreenToWorldPoint(mousePos), Vector2.zero);

        if (hit.collider == null || hit.collider.gameObject != this.gameObject) return;
        isDragging = true;
        var arrowInstance = Instantiate(arrowPrefab);
        currentArrow = arrowInstance.GetComponent<ArrowView>();
        currentArrow.Setup(transform.position);
    }

    private void OnClickCanceled(InputAction.CallbackContext context)
    {
        if (!isDragging || currentArrow == null) return;

        var mousePos = PlayerInputController.Instance.InputActions.PlayerControl.Point.ReadValue<Vector2>();
        var hit = Physics2D.Raycast(mainCamera.ScreenToWorldPoint(mousePos), Vector2.zero);

        if (hit.collider != null && hit.collider.CompareTag("Target"))
        {
            Debug.Log("hit.collider.gameObject");
        }
        
        Destroy(currentArrow.gameObject);
        isDragging = false;
        currentArrow = null;
    }
    
    private Vector3 GetMouseWorldPosition()
    {
        var mousePos = PlayerInputController.Instance.InputActions.PlayerControl.Point.ReadValue<Vector2>();
        var worldPos = mainCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, mainCamera.nearClipPlane));
        
        return worldPos;
    }
}