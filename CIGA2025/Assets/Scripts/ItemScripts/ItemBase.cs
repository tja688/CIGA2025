// ItemBase.cs

using UnityEngine;

public abstract class ItemBase : MonoBehaviour, ISelectable
{
    // 用于缓存自身的碰撞体组件，避免每次都调用 GetComponent，性能更好
    private BoxCollider2D _boxCollider;

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

    // 【删除】不再需要在 Start() 中进行一次性的赋值
    // private void Start()
    // {
    //     SelectionBounds =  GetComponent<BoxCollider2D>().bounds;
    // }
}