using UnityEngine;

[RequireComponent(typeof(Animator))]
public class lighterItem : ItemBase
{
    private static readonly int Live = Animator.StringToHash("Live");

    private Animator _animator;
    
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
    
    protected override void OnCollisionEnter2D(Collision2D collision)
    {
        if(!isOnActive) return;
        
        if (!collision.gameObject.CompareTag("Boss")) return;
        
        var bossHealth = collision.gameObject.GetComponent<BossHealth>();

        if (bossHealth != null)
        {
            bossHealth.ApplyBurnOverTime(itemDamage,2f);
                
            _currentHitCount++;
                
            if (_currentHitCount < maxHitCount) return;

            Destroy(gameObject); 
        }
        else
        {
            Debug.LogWarning(collision.gameObject.name + " 标签为Boss，但未找到 BossHealth 脚本！");
        }
    }
}