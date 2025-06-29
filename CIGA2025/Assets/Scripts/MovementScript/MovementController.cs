using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerController : MonoBehaviour
{
    [Header("组件关联")]
    public Tilemap groundTilemap; 
    public Animator animator;

    [Header("移动参数")]
    public float moveSpeed = 5f; 

    // 【新增】音效配置
    [Header("音效配置")]
    [Tooltip("玩家移动一格时播放的音效")]
    [SerializeField] private AudioConfigSO moveSound;

    // --- 内部状态变量 ---
    private Vector3Int _currentCellPos;
    private Vector3 _targetWorldPos;    
    private bool _isMoving = false;    
    private bool _canMove = true;
    
    // --- 输入与动画 ---
    private GameInput _playerInputActions;
    private static readonly int IsMoving = Animator.StringToHash("IsMoving");
    private static readonly int MoveX = Animator.StringToHash("MoveX");
    private static readonly int MoveY = Animator.StringToHash("MoveY");
    
    private void OnEnable()
    {
        // 确保能获取到输入实例
        if (_playerInputActions == null)
        {
            if(PlayerInputController.Instance != null)
            {
                 _playerInputActions = PlayerInputController.Instance.InputActions;
            }
            else
            {
                Debug.LogError("PlayerInputController 实例未找到！");
                return;
            }
        }
        _playerInputActions.PlayerControl.Enable();
        
        // 订阅游戏状态变化事件 (如果你的项目中有 GameFlowManager)
        // GameFlowManager.OnGameStateChanged += HandleGameStateChanged;
    }

    private void OnDisable()
    {
        _playerInputActions?.PlayerControl.Disable();
        
        // 取消订阅
        // GameFlowManager.OnGameStateChanged -= HandleGameStateChanged;
    }

    void Start()
    {
        if (groundTilemap == null)
        {
            Debug.LogError("未在 PlayerController 上关联 Ground Tilemap！");
            enabled = false; 
            return;
        }

        // 初始化玩家位置到Tilemap网格中心
        _currentCellPos = GetCellPosition(transform.position);
        _targetWorldPos = GetWorldCenterPosition(_currentCellPos);
        transform.position = _targetWorldPos; 
        
    }
    
    /// <summary>
    /// 处理游戏状态变化，决定玩家是否可以移动。
    /// </summary>
    private void HandleGameStateChanged(GameFlowManager.GameState newState)
    {
        // 只有在Gameplay状态下才允许移动
        SetMovementEnabled(newState == GameFlowManager.GameState.Gameplay);
    }

    /// <summary>
    /// 【核心公共方法】启用或禁用玩家的移动能力。
    /// </summary>
    public void SetMovementEnabled(bool isEnabled)
    {
        _canMove = isEnabled;
        
        // 如果被禁用移动，要确保角色立即停止当前的移动动画和物理移动
        if (!_canMove)
        {
            _isMoving = false;
            if(animator != null) animator.SetBool(IsMoving, false);
        }
    }

    private void Update()
    {
        // 判断条件大大简化
        if (!_canMove) return;
        
        HandleMovement();
    }

    private void HandleMovement()
    {
        if (_isMoving)
        {
            // 如果正在移动，则向目标位置靠近
            transform.position = Vector3.MoveTowards(transform.position, _targetWorldPos, moveSpeed * Time.deltaTime);

            // 判断是否已到达目标点
            if (!(Vector3.Distance(transform.position, _targetWorldPos) < 0.001f)) return;
            
            // 到达后停止移动
            transform.position = _targetWorldPos;
            _isMoving = false;
            animator.SetBool(IsMoving, false);
        }
        else 
        {
            // 如果不在移动中，则检测输入
            var moveDirection = Vector3Int.zero;
            
            if (_playerInputActions.PlayerControl.Up.WasPressedThisFrame()) moveDirection = Vector3Int.up;
            else if (_playerInputActions.PlayerControl.Down.WasPressedThisFrame()) moveDirection = Vector3Int.down;
            else if (_playerInputActions.PlayerControl.Left.WasPressedThisFrame()) moveDirection = Vector3Int.left;
            else if (_playerInputActions.PlayerControl.Right.WasPressedThisFrame()) moveDirection = Vector3Int.right;

            // 如果没有移动输入，则直接返回
            if (moveDirection == Vector3Int.zero) return;
            
            // 计算下一个目标格子
            var nextCellPos = _currentCellPos + moveDirection;

            // 检查目标格子是否可以行走
            if (!IsCellWalkable(nextCellPos)) return;
            
            // 【核心改动】在确认可以移动后，播放音效
            if (moveSound != null && AudioManager.Instance != null)
            {
                // 播放一次性的移动音效，不循环
                AudioManager.Instance.Play(moveSound, isLooping: false);
            }

            // 更新目标位置并开始移动
            _currentCellPos = nextCellPos;
            _targetWorldPos = GetWorldCenterPosition(_currentCellPos);
            _isMoving = true;
                    
            // 更新动画状态
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
        // 检查Tilemap在目标位置是否有Tile
        return groundTilemap.GetTile(cellPosition);
    }
}