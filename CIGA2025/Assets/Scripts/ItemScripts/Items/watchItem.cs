// [新增] 需要引入UniTask命名空间，因为我们要调用一个返回UniTask的异步方法
using Cysharp.Threading.Tasks;
using UnityEngine;

public class watchItem : ItemBase
{
    private static readonly int Live = Animator.StringToHash("Live");

    private Animator _animator;

    // [新增] 在Inspector中可以配置此物品造成的眩晕时长
    [Header("眩晕效果")]
    [Tooltip("此物品对Boss造成的眩晕时间（秒）")]
    public float stunDuration = 5f;

    protected override void Awake()
    {
        base.Awake();
        _animator = GetComponent<Animator>();
    }

    public override void OnActivate()
    {
        base.OnActivate();
        if (_animator != null)
        {
            _animator.SetTrigger(Live);
        }
        else
        {
            Debug.LogWarning("无法播放激活动画：在 " + this.name + " 上没有找到 Animator 组件！");
        }
    }

    // [重要修改] 将原有的OnCollisionEnter2D方法替换成下面的内容。
    // 我们将同时获取BossAI和BossHealth组件。
    protected override void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isOnActive) return;
        
        // 1. 检查碰撞对象是否是 "Boss"
        if (!collision.gameObject.CompareTag("Boss")) return;
        
        // 2. [核心修改] 尝试从碰撞对象上获取 BossAI 组件
        var bossAI = collision.gameObject.GetComponent<BossAI>();

        // 3. 如果成功获取到 BossAI 组件
        if (bossAI != null)
        {
            Debug.Log($"[watchItem] 成功击中Boss，使其眩晕 {stunDuration} 秒！");
            
            // [核心修改] 调用Boss的异步眩晕方法。
            // 因为OnCollisionEnter2D是同步方法，我们不能在此处'await'，
            // 所以使用 .Forget() 来“发射后不管”，让眩晕逻辑在后台执行。
            bossAI.GetStunned(stunDuration).Forget();
                
            // (可选) 如果你还想在眩晕的同时造成伤害，可以保留这部分逻辑
            var bossHealth = collision.gameObject.GetComponent<BossHealth>();
            if(bossHealth != null)
            {
                bossHealth.TakeDamage(itemDamage);
            }

            // 4. 触发效果后，无论如何都销毁物品
            Destroy(gameObject); 
        }
        else
        {
            // 如果只找到了BossHealth但没找到BossAI，说明对象状态可能不完整
            Debug.LogWarning(collision.gameObject.name + " 标签为Boss，但未找到 BossAI 脚本！无法执行眩晕。");
        }
    }

}