// ItemBase.cs

using Unity.VisualScripting;
using UnityEngine;

public abstract class ItemBase : MonoBehaviour, ISelectable
{
    // 用于缓存自身的碰撞体组件，避免每次都调用 GetComponent，性能更好
    private BoxCollider2D _boxCollider;
    
    private bool isOnActive = false; 

    // 【修改】将 SelectionBounds 改为计算属性
    public Bounds SelectionBounds
    {
        get
        {
            // 每次访问这个属性时，都会实时获取碰撞体当前的边界并返回
            // 如果 _boxCollider 为空，则先获取它
            if (_boxCollider == null)
            {
                _boxCollider = GetComponent<BoxCollider2D>();
            }
            return _boxCollider.bounds;
        }
    }

    // Unity 生命周期方法，用于初始化
    private void Awake()
    {
        // 在对象创建时就获取并缓存碰撞体组件的引用
        _boxCollider = GetComponent<BoxCollider2D>();
    }

    /// <summary>
    /// 【新增】实现 ISelectable 的 OnActivate 方法。
    /// 定义为抽象方法，强制子类提供具体的激活逻辑。
    /// </summary>
    public virtual void OnActivate()
    {
        if (ActivationManager.Instance)
        {
            ActivationManager.Instance.Activate(this);
        }
        else
        {
            Debug.LogError("无法激活：场景中未找到 ActivationManager！");
        }
        
        isOnActive =  true;
    }
}