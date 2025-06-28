using UnityEngine;

// 物体必须有 Rigidbody2D 才能移动和检测触发器
[RequireComponent(typeof(Rigidbody2D))]
public class WanderController : MonoBehaviour
{
    // 定义游荡状态
    private enum WanderState
    {
        // 【修改】移除Idle状态，因为脚本要么在游荡，要么被禁用
        MovingToInitialPoint,
        Wandering
    }

    private DraggableObject _draggable;
    private WanderState _currentState;

    private Rigidbody2D _rb;
    private Bounds _wanderBounds;
    private float _initialMoveSpeed;
    private float _wanderSpeed;
    private Vector2 _targetPosition;
    private Collider2D _destructionZone;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _draggable = GetComponent<DraggableObject>();
    }

    private void FixedUpdate()
    {
        // 【核心修改】因为脚本一旦不处于游荡状态就会被禁用，
        // 所以 FixedUpdate 不再需要检查 Idle 状态。
        // 只要这个方法在运行，就说明一定在游荡。

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
                _currentState = WanderState.Wandering;
                Debug.Log($"{name} 到达指定区域，开始慢速游荡。");
            }
            _targetPosition = GetRandomPointInBounds();
        }

        Vector2 direction = (_targetPosition - (Vector2)transform.position).normalized;
        _rb.velocity = direction * currentSpeed;
    }

    public void StartWandering(Bounds bounds, float initialSpeed, float wanderSpeed, Collider2D destructionZone)
    {
        // 如果脚本被禁用了，可以通过重新激活来开始（如果需要这种逻辑）
        this.enabled = true; 
        
        Debug.Log($"{name} 开始冲向目标区域！");
        _wanderBounds = bounds;
        _initialMoveSpeed = initialSpeed;
        _wanderSpeed = wanderSpeed;
        _destructionZone = destructionZone;
        _targetPosition = GetRandomPointInBounds();
        _currentState = WanderState.MovingToInitialPoint;
        
        if (_draggable != null)
        {
            _draggable.IsDraggable = true;
        }
    }
    
    public void StopWandering()
    {
        // 如果组件已经被禁用了，就没必要再执行了
        if (!this.enabled) return;
        
        
        // 停止移动
        _rb.velocity = Vector2.zero;
        _destructionZone = null;
        
        // 【核心修复】禁用此组件，这样它的 FixedUpdate 就不会再运行了
        this.enabled = false;
    }

    private Vector2 GetRandomPointInBounds()
    {
        float randomX = Random.Range(_wanderBounds.min.x, _wanderBounds.max.x);
        float randomY = Random.Range(_wanderBounds.min.y, _wanderBounds.max.y);
        return new Vector2(randomX, randomY);
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other == _destructionZone)
        {
            Debug.Log($"{name} 已超出边界，销毁！");
            Destroy(gameObject);
        }
    }
}