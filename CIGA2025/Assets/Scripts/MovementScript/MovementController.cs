using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerController : MonoBehaviour
{
    public Tilemap groundTilemap; 
    public float moveSpeed = 5f; 

    private Vector3Int _currentCellPos;
    private Vector3 _targetWorldPos;    
    private bool _isMoving = false;    

    public Animator animator;
    private static readonly int IsMoving = Animator.StringToHash("IsMoving");
    private static readonly int MoveX = Animator.StringToHash("MoveX");
    private static readonly int MoveY = Animator.StringToHash("MoveY");
    
    private GameInput _playerInputActions;

    private void OnEnable()
    {
        if (_playerInputActions == null)
        {
            if(PlayerInputController.Instance != null)
            {
                 _playerInputActions = PlayerInputController.Instance.InputActions;
            }
            else
            {
                return;
            }
        }
        
        _playerInputActions.PlayerControl.Enable();
    }

    private void OnDisable()
    {
        _playerInputActions?.PlayerControl.Disable();
    }


    void Start()
    {
        if (groundTilemap == null)
        {
            Debug.LogError("Ground Tilemap is not assigned to PlayerController!");
            enabled = false; 
            return;
        }

        _currentCellPos = GetCellPosition(transform.position);
        _targetWorldPos = GetWorldCenterPosition(_currentCellPos);
        transform.position = _targetWorldPos; 
        
        _playerInputActions = PlayerInputController.Instance.InputActions;
        
        if (!_playerInputActions.PlayerControl.enabled)
        {
            _playerInputActions.PlayerControl.Enable();
        }

    }

    private void Update()
    {
        HandleMovement();
    }

    private void HandleMovement()
    {
        if (_isMoving)
        {
            transform.position = Vector3.MoveTowards(transform.position, _targetWorldPos, moveSpeed * Time.deltaTime);

            if (!(Vector3.Distance(transform.position, _targetWorldPos) < 0.001f)) return;
            transform.position = _targetWorldPos;
            _isMoving = false;
            animator.SetBool(IsMoving, false);
        }
        else 
        {
            var moveDirection = Vector3Int.zero;
            
            if (_playerInputActions.PlayerControl.Up.WasPressedThisFrame())
            {
                moveDirection = Vector3Int.up;
            }
            else if (_playerInputActions.PlayerControl.Down.WasPressedThisFrame())
            {
                moveDirection = Vector3Int.down;
            }
            else if (_playerInputActions.PlayerControl.Left.WasPressedThisFrame())
            {
                moveDirection = Vector3Int.left;
            }
            else if (_playerInputActions.PlayerControl.Right.WasPressedThisFrame())
            {
                moveDirection = Vector3Int.right;
            }

            if (moveDirection == Vector3Int.zero) return;
            var nextCellPos = _currentCellPos + moveDirection;

            if (!IsCellWalkable(nextCellPos)) return;
            _currentCellPos = nextCellPos;
            _targetWorldPos = GetWorldCenterPosition(_currentCellPos);
            _isMoving = true;
                    
            animator.SetBool(IsMoving, true); 
            animator.SetFloat(MoveX, moveDirection.x);
            animator.SetFloat(MoveY, moveDirection.y);
        }
    }

    private Vector3Int GetCellPosition(Vector3 worldPosition)
    {
        return groundTilemap.WorldToCell(worldPosition);
    }

    private Vector3 GetWorldCenterPosition(Vector3Int cellPosition)
    {
        var worldPos = groundTilemap.CellToWorld(cellPosition);
        var cellSize = groundTilemap.cellSize;
        return worldPos + new Vector3(cellSize.x / 2, cellSize.y / 2, 0);
    }

    private bool IsCellWalkable(Vector3Int cellPosition)
    {
        return groundTilemap.GetTile(cellPosition);
    }
}