using UnityEngine;

// 物体必须有 Rigidbody2D 才能移动和检测触发器
[RequireComponent(typeof(Rigidbody2D))]
public class WanderController : MonoBehaviour
{
    // 定义游荡状态
    private enum WanderState
    {
        Idle,                 // 静止或被拖拽
        MovingToInitialPoint, // 正在移动到游荡区域的第一个随机点
        Wandering             // 在区域内持续游荡
    }
    
    private DraggableObject _draggable; // 【新增】缓存 DraggableObject 引用


    private WanderState _currentState = WanderState.Idle;
    
    private Rigidbody2D _rb;
    private Bounds _wanderBounds;
    private float _initialMoveSpeed; // 【新增】用于存储初始速度
    private float _wanderSpeed;      // 【修改】专门用于存储游荡速度
    private Vector2 _targetPosition;
    private Collider2D _destructionZone;


    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        
        _draggable = GetComponent<DraggableObject>(); // 在 Awake 中获取

    }

    private void FixedUpdate()
    {
        if (_currentState == WanderState.Idle)
        {
            _rb.velocity = Vector2.zero; // 确保静止时速度为0
            return;
        }

        // 【核心修改】根据当前状态选择合适的速度
        float currentSpeed = 0f;
        switch (_currentState)
        {
            case WanderState.MovingToInitialPoint:
                currentSpeed = _initialMoveSpeed;
                break;
            case WanderState.Wandering:
                currentSpeed = _wanderSpeed;
                break;
        }

        float distanceToTarget = Vector2.Distance(transform.position, _targetPosition);

        if (distanceToTarget < 0.1f)
        {
            if (_currentState == WanderState.MovingToInitialPoint)
            {
                _currentState = WanderState.Wandering; // 到达后，切换到游荡状态
                Debug.Log($"{name} 到达指定区域，开始慢速游荡。");
            }
            _targetPosition = GetRandomPointInBounds();
        }

        Vector2 direction = (_targetPosition - (Vector2)transform.position).normalized;
        _rb.velocity = direction * currentSpeed; // 使用当前状态对应的速度
    }

    /// <summary>
    /// 【修改】更新 StartWandering 方法签名以接收两个速度
    /// </summary>
    public void StartWandering(Bounds bounds, float initialSpeed, float wanderSpeed, Collider2D destructionZone)
    {
        Debug.Log($"{name} 开始冲向目标区域！");
        _wanderBounds = bounds;
        _initialMoveSpeed = initialSpeed; // 存储初始速度
        _wanderSpeed = wanderSpeed;       // 存储游荡速度
        _destructionZone = destructionZone;
        _targetPosition = GetRandomPointInBounds();
        _currentState = WanderState.MovingToInitialPoint;
        
        // 【修改】开始游荡时，将物体设为“可抓取”
        if (_draggable != null)
        {
            _draggable.IsDraggable = true;
        }
    }
    
    /// <summary>
    /// 停止游荡（例如被拖拽时调用）
    /// </summary>
    public void StopWandering()
    {
        if (_currentState == WanderState.Idle) return; // 如果已经是Idle，则无需操作
        
        Debug.Log($"{name} 停止游荡。");
        _currentState = WanderState.Idle;
        _rb.velocity = Vector2.zero; // 立即停止移动
        _destructionZone = null;
    }

    /// <summary>
    /// 在边界内获取一个随机点
    /// </summary>
    private Vector2 GetRandomPointInBounds()
    {
        float randomX = Random.Range(_wanderBounds.min.x, _wanderBounds.max.x);
        float randomY = Random.Range(_wanderBounds.min.y, _wanderBounds.max.y);
        return new Vector2(randomX, randomY);
    }

    /// <summary>
    /// 当离开某个触发器时被调用
    /// </summary>
    private void OnTriggerExit2D(Collider2D other)
    {
        // 只有在游荡状态下，并且离开的是指定的销毁区域时，才销毁自己
        if (other == _destructionZone)
        {
            Debug.Log($"{name} 已超出边界，销毁！");
            Destroy(gameObject);
        }
    }
}