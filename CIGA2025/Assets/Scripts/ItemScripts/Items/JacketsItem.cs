using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JacketsItem : ItemBase
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
    }

    protected void OnTriggerEnter2D(Collider2D  collision)
    {
        if(!isOnActive) return;
        
        if (!collision.gameObject.CompareTag("Player")) return;
        
        var playerStatus = collision.gameObject.GetComponent<PlayerStatusController>();

        if (playerStatus != null)
        {
            playerStatus.ActivateShield();
                
            _currentHitCount++;
                
            if (_currentHitCount < maxHitCount) return;

            Destroy(gameObject); 
        }
    }
}
