// ItemBase.cs
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))] 
public abstract class ItemBase : MonoBehaviour, ISelectable
{
    [Header("道具核心配置")]
    [Tooltip("道具对Boss造成的伤害值")]
    [SerializeField] protected int itemDamage = 50;

    [Tooltip("道具在销毁前可以承受的碰撞次数")]
    [SerializeField] protected int maxHitCount = 1;

    [Tooltip("道具的物理质量")]
    [SerializeField] protected float itemMass = 1.0f;

    // --- 内部状态变量 ---
    protected bool isOnActive = false; 
    private int _currentHitCount = 0; // 用于追踪当前碰撞次数

    // --- 组件引用 ---
    private BoxCollider2D _boxCollider;
    protected Rigidbody2D _rigidbody; // 改为 protected，方便子类可能访问

    #region 接口实现
    public Bounds SelectionBounds
    {
        get
        {
            return _boxCollider.bounds;
        }
    }
    
    public bool IsSelectionEnabled => !isOnActive;
    #endregion

    /// <summary>
    /// Awake 在对象实例化后立即执行，适合用于初始化和组件获取
    /// </summary>
    protected virtual void Awake()
    {
        _boxCollider = GetComponent<BoxCollider2D>();
        _rigidbody = GetComponent<Rigidbody2D>();

        // 应用配置的质量值
        if (_rigidbody != null)
        {
            _rigidbody.mass = itemMass;
        }
        else
        {
            Debug.LogError("错误： " + gameObject.name + " 未能找到 Rigidbody2D 组件！");
        }
    }

    /// <summary>
    /// 激活道具的通用逻辑
    /// </summary>
    public virtual void OnActivate()
    {
        if (isOnActive) return;

        if (ActivationManager.Instance)
        {
            ActivationManager.Instance.Activate(this);
        }
        else
        {
            Debug.LogError("无法激活：场景中未找到 ActivationManager！");
        }
        
        isOnActive = true;
    }

    /// <summary>
    /// 通用的碰撞检测逻辑。
    /// 设置为 virtual，允许子类在需要时进行扩展(override)。
    /// </summary>
    /// <param name="collision">碰撞信息</param>
    protected virtual void OnCollisionEnter2D(Collision2D collision)
    {
        if(!isOnActive) return;
        // 1. 检查碰撞对象的标签是否为 "Boss"
        if (!collision.gameObject.CompareTag("Boss")) return;
        
        Debug.Log(this.name + " 碰撞到了 " + collision.gameObject.name);

        // 2. 尝试从Boss对象上获取 BossHealth 组件
        BossHealth bossHealth = collision.gameObject.GetComponent<BossHealth>();

        if (bossHealth != null)
        {
            // 使用配置的伤害值对Boss造成伤害
            bossHealth.TakeDamage(itemDamage);
                
            // 3. 累计碰撞次数
            _currentHitCount++;
                
            // 4. 检查是否达到最大碰撞次数
            if (_currentHitCount >= maxHitCount)
            {
                Debug.Log(this.name + " 已达到最大碰撞次数，将被销毁。");
                Destroy(gameObject); // 达到次数后销毁自身
            }
        }
        else
        {
            Debug.LogWarning(collision.gameObject.name + " 标签为Boss，但未找到 BossHealth 脚本！");
        }
    }
}