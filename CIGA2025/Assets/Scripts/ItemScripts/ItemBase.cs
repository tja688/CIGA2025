// ItemBase.cs
using UnityEngine;

public abstract class ItemBase : MonoBehaviour, ISelectable
{
    private BoxCollider2D _boxCollider;
    protected bool isOnActive = false; 

    public Bounds SelectionBounds
    {
        get
        {
            if (_boxCollider == null) _boxCollider = GetComponent<BoxCollider2D>();
            return _boxCollider.bounds;
        }
    }
    
    /// <summary>
    /// 【新增】实现接口的新属性。
    /// 如果物体未被激活 (isOnActive is false)，则允许被选择。
    /// </summary>
    public bool IsSelectionEnabled => !isOnActive;

    private void Awake()
    {
        _boxCollider = GetComponent<BoxCollider2D>();
    }

    public virtual void OnActivate()
    {
        // 如果已经被激活，直接返回，防止重复激活
        if (isOnActive) return;

        if (ActivationManager.Instance)
        {
            ActivationManager.Instance.Activate(this);
        }
        else
        {
            Debug.LogError("无法激活：场景中未找到 ActivationManager！");
        }
        
        // 在最后将状态设置为已激活
        isOnActive = true;
    }
}