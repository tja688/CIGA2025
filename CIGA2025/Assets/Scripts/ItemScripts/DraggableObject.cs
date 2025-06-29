// DraggableObject.cs

using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))] 
[RequireComponent(typeof(Collider2D))] 
public class DraggableObject : MonoBehaviour, IDraggable
{
    [SerializeField] private float dragForceMultiplier = 1f;
    [SerializeField] private Color dragStartColor = Color.yellow;

    private Rigidbody2D rb;
    private Color originalColor;
    private SpriteRenderer spriteRenderer;
    private WanderController _wanderController; // 【新增】缓存 WanderController 的引用
    
    // 【新增】实现接口定义的事件
    public event Action OnWillBeDestroyed;
    
    /// <summary>
    /// 【新增】控制此物体当前是否可被抓取。
    /// </summary>
    public bool IsDraggable { get; set; } = false; // 默认不可抓取

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if(spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        _wanderController = GetComponent<WanderController>();
    }

    public void OnDragStart()
    {
        // 【新增】如果当前不可抓取，则直接返回
        if (!IsDraggable)
        {
            Debug.Log("Can't drag object");
            
            return;

        }

        if (_wanderController != null)
        {
            _wanderController.StopWandering();
        }

        if(spriteRenderer != null)
        {
            spriteRenderer.color = dragStartColor;
        }
        Debug.Log($"{name} 开始被拖拽!");
    }

    public void OnDrag(Vector3 mouseWorldPosition)
    {
        // 【新增】如果当前不可抓取，则直接返回
        if (!IsDraggable) return;
        
        Vector2 direction = (Vector2)mouseWorldPosition - rb.position;
        float distance = direction.magnitude;
        Vector2 force = direction.normalized * (distance * dragForceMultiplier);
        rb.AddForce(force);
    }

    public void OnDragEnd()
    {
        // 【新增】如果当前不可抓取，则直接返回
        if (!IsDraggable) return;
        
        if(spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
        // 当拖拽结束后，我们不自动恢复游荡。激活需要通过点击重新触发。
        Debug.Log($"{name} 拖拽结束!");
    }

    private void OnDestroy()
    {
        OnWillBeDestroyed?.Invoke();
    }
}