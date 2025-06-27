// DraggableObject.cs
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))] 
[RequireComponent(typeof(Collider2D))] 
public class DraggableObject : MonoBehaviour, IDraggable
{
    [SerializeField] private float dragForceMultiplier = 10f; // 拖拽力的乘数，可调整
    [SerializeField] private Color dragStartColor = Color.yellow; // 被拖拽时的颜色

    private Rigidbody2D rb;
    private Color originalColor;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if(spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    public void OnDragStart()
    {
        if(spriteRenderer != null)
        {
            spriteRenderer.color = dragStartColor;
        }
        Debug.Log($"{name} 开始被拖拽!");
    }

    public void OnDrag(Vector3 mouseWorldPosition)
    {
        // 计算从物体中心指向鼠标的向量
        Vector2 direction = (Vector2)mouseWorldPosition - rb.position;
        // 距离越远，力越大
        float distance = direction.magnitude;
        Vector2 force = direction.normalized * distance * dragForceMultiplier;

        // 施加一个力，让物体向鼠标位置移动
        rb.AddForce(force);
    }

    public void OnDragEnd()
    {
        if(spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
        Debug.Log($"{name} 拖拽结束!");
    }
}