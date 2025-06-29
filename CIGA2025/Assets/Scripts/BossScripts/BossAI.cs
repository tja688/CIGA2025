// BossAI.cs
using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;

/// <summary>
/// [最终版本] Boss 的核心AI，使用FSM（有限状态机）控制行为。
/// 通过UniTask管理异步的攻击循环，并可被外部调用打断。
/// </summary>
[RequireComponent(typeof(BossHealth), typeof(SkillCaster), typeof(BossAnimationController))]
public class BossAI : MonoBehaviour
{
    private enum BossPhase { Phase1, Phase2, Phase3, Phase4_Ultimate, Stunned, Defeated }
    private BossPhase _currentPhase;

    private BossHealth _bossHealth;
    private SkillCaster _skillCaster;
    private BossAnimationController _animationController;

    private CancellationTokenSource _cancellationTokenSource;
    private bool _isStunned = false;
    private bool _isLoopRunning = false;
    
    void OnEnable()
    {
        GameFlowManager.OnGameStateChanged += HandleGameStateChanged;
    }

    void OnDisable()
    {
        GameFlowManager.OnGameStateChanged -= HandleGameStateChanged;
    }
    
    void Start()
    {
        _bossHealth = GetComponent<BossHealth>();
        _skillCaster = GetComponent<SkillCaster>();
        _animationController = GetComponent<BossAnimationController>();
        _bossHealth.OnDeath += HandleDeath;
        _cancellationTokenSource = new CancellationTokenSource();
    }
    
    private void HandleGameStateChanged(GameFlowManager.GameState newState)
    {
        if (newState == GameFlowManager.GameState.Gameplay && !_isLoopRunning)
        {
            Debug.Log("[BossAI] 游戏开始，启动Boss主循环！");
            _isLoopRunning = true;
            MainLoop().Forget();
        }
        else if (newState != GameFlowManager.GameState.Gameplay && _isLoopRunning)
        {
            Debug.Log("[BossAI] 游戏暂停或结束，停止Boss活动。");
            _cancellationTokenSource.Cancel();
            _isLoopRunning = false;
        }
    }

    private async UniTask MainLoop()
    {
        await TransitionToPhase(BossPhase.Phase1);

        while (_currentPhase != BossPhase.Defeated && _isLoopRunning)
        {
            if (_isStunned)
            {
                await UniTask.WaitUntil(() => !_isStunned);
            }

            BossPhase nextPhase = GetPhaseFromHealth();
            if (nextPhase != _currentPhase)
            {
                await TransitionToPhase(nextPhase);
            }

            await UniTask.Yield();
        }
    }

    private async UniTask TransitionToPhase(BossPhase newPhase)
    {
        if (_currentPhase == newPhase && newPhase != BossPhase.Phase1) return;

        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();

        _currentPhase = newPhase;
        Debug.LogWarning($"=============== Boss 进入新阶段: {newPhase} ===============");
        
        // [新增] 通知动画控制器更新当前的动画阶段主题！
        UpdateAnimationPhase(newPhase);

        // [已修改] 根据你的要求，如果暂无转场动画，可以注释掉此行。
        // _animationController.PlayPhaseTransition();
        
        // 这个延迟可以作为转场动画的播放时间，或者一个硬直的停顿
        await UniTask.Delay(TimeSpan.FromSeconds(2), cancellationToken: _cancellationTokenSource.Token);

        switch (newPhase)
        {
            case BossPhase.Phase1:
                Phase1_AttackCycle(_cancellationTokenSource.Token).Forget();
                break;
            case BossPhase.Phase2:
                Phase2_AttackCycle(_cancellationTokenSource.Token).Forget();
                break;
            case BossPhase.Phase3:
                Phase3_AttackCycle(_cancellationTokenSource.Token).Forget();
                break;
            case BossPhase.Phase4_Ultimate:
                Phase4_UltimateAttack().Forget();
                break;
        }
    }

    // [新增] 一个辅助方法，用来将Boss的逻辑阶段映射到动画阶段
    private void UpdateAnimationPhase(BossPhase bossPhase)
    {
        BossAnimationController.AnimPhase animPhase;
        switch (bossPhase)
        {
            case BossPhase.Phase1:
                animPhase = BossAnimationController.AnimPhase.Ice;
                break;
            case BossPhase.Phase2:
                animPhase = BossAnimationController.AnimPhase.Fire;
                break;
            case BossPhase.Phase3: // P3狂暴模式复用火阶段动画
                animPhase = BossAnimationController.AnimPhase.Fire;
                break;
            case BossPhase.Phase4_Ultimate:
                animPhase = BossAnimationController.AnimPhase.Ultimate;
                break;
            default:
                animPhase = BossAnimationController.AnimPhase.General;
                break;
        }
        _animationController.SetAnimationPhase(animPhase);
    }
    
    #region Attack Cycles

    private async UniTask Phase1_AttackCycle(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            _animationController.PlayFrostBeamCast();
            await _skillCaster.CastFrostBeam(cancellationToken);
            await UniTask.Delay(TimeSpan.FromSeconds(3), cancellationToken: cancellationToken);

            _animationController.PlayBlizzardCast();
            await _skillCaster.CastBlizzard(cancellationToken);
            
            _animationController.PlayStunned();
            Debug.Log("Boss冻结自己，进入5秒休息窗口。");
            await UniTask.Delay(TimeSpan.FromSeconds(5), cancellationToken: cancellationToken);

             _animationController.PlayIdle();
        }
    }

    private async UniTask Phase2_AttackCycle(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            _animationController.PlayAlternatingFlamesCast();
            await _skillCaster.CastAlternatingFlames(cancellationToken);
            await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: cancellationToken);
            
            _animationController.PlayFlameWaveCast();
            await _skillCaster.CastFlameWave(cancellationToken);
            await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: cancellationToken);

            _animationController.PlayGroundSpikesCast();
            await _skillCaster.CastGroundSpikes(cancellationToken);

            _animationController.PlayStunned();
            Debug.Log("Boss过热，进入4秒冷却，受到伤害增加50%。");
            await UniTask.Delay(TimeSpan.FromSeconds(4), cancellationToken: cancellationToken);
            
            _animationController.PlayIdle();
        }
    }

    private async UniTask Phase3_AttackCycle(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
             _animationController.PlayFrostBeamCast();
             await _skillCaster.CastFrostBeam(cancellationToken);
             await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: cancellationToken);

             _animationController.PlayFlameWaveCast();
             await _skillCaster.CastFlameWave(cancellationToken);
             await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: cancellationToken);

             _animationController.PlayGroundSpikesCast();
             await _skillCaster.CastGroundSpikes(cancellationToken);
             
             Debug.Log("狂暴模式一轮攻击结束，休息3秒。");
             await UniTask.Delay(TimeSpan.FromSeconds(3), cancellationToken: cancellationToken);
        }
    }

    private async UniTask Phase4_UltimateAttack()
    {
         _animationController.PlayUltimateCharge();
         await _skillCaster.CastUltimateAttack(
            onPlayerFail: () => {
                Debug.Log("GAME OVER");
            },
            onPlayerSucceed: () => {
                Debug.Log("玩家获得最后一击的机会！");
                _animationController.PlayStunned();
            }
         );
    }

    #endregion

    public async UniTask GetStunned(float duration)
    {
        if (_isStunned || _currentPhase == BossPhase.Defeated) return;

        _isStunned = true;
        Debug.LogWarning($"Boss 被眩晕 {duration} 秒！攻击被打断！");

        _cancellationTokenSource.Cancel();
        _cancellationTokenSource = new CancellationTokenSource();
        
        _animationController.PlayStunned();

        await UniTask.Delay(TimeSpan.FromSeconds(duration));

        Debug.Log("Boss 眩晕结束，恢复行动。");
        _isStunned = false;
        
        await UniTask.Delay(TimeSpan.FromSeconds(1));
        
        await TransitionToPhase(GetPhaseFromHealth());
    }

    private BossPhase GetPhaseFromHealth()
    {
        float hp = _bossHealth.CurrentHealth;
        if (hp > 750) return BossPhase.Phase1;
        if (hp > 500) return BossPhase.Phase2;
        if (hp > 100) return BossPhase.Phase3;
        if (hp > 0) return BossPhase.Phase4_Ultimate;
        return BossPhase.Defeated;
    }

    private void HandleDeath()
    {
        Debug.LogWarning("Boss已被击败！");
        _currentPhase = BossPhase.Defeated;
        _cancellationTokenSource.Cancel();
        _animationController.PlayDeath();
        
        // 死亡后可以禁用gameObject或者执行其他销毁逻辑
        // this.gameObject.SetActive(false); 
    }

    private void OnDestroy()
    {
        if (_cancellationTokenSource != null)
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
        }
    }
}