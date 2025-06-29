// BossAI.cs
using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;

/// <summary>
/// Boss 的核心AI，使用FSM（有限状态机）控制行为。
/// 通过UniTask管理异步的攻击循环，并可被外部调用打断。
/// </summary>
[RequireComponent(typeof(BossHealth), typeof(SkillCaster), typeof(BossAnimationController))]
public class BossAI : MonoBehaviour
{
    private enum BossPhase { Phase1, Phase2, Phase3, Phase4_Ultimate, Stunned, Defeated }
    private BossPhase _currentPhase;

    // 外部组件引用
    private BossHealth _bossHealth;
    private SkillCaster _skillCaster;
    private BossAnimationController _animationController;

    // 任务取消控制
    private CancellationTokenSource _cancellationTokenSource;
    private bool _isStunned = false;

    // 【新增】一个标志位，防止重复启动主循环
    private bool _isLoopRunning = false;
    
    
    // 【修改】使用 OnEnable/OnDisable 来管理事件订阅
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
        // 获取所有必要的组件
        _bossHealth = GetComponent<BossHealth>();
        _skillCaster = GetComponent<SkillCaster>();
        _animationController = GetComponent<BossAnimationController>();

        // 订阅死亡事件
        _bossHealth.OnDeath += HandleDeath;

        // 初始化取消令牌
        _cancellationTokenSource = new CancellationTokenSource();
        
    }
    
    // 【新增】处理游戏状态变化
    private void HandleGameStateChanged(GameFlowManager.GameState newState)
    {
        if (newState == GameFlowManager.GameState.Gameplay && !_isLoopRunning)
        {
            // 如果是游戏状态且循环未运行，则启动Boss AI
            Debug.Log("[BossAI] 游戏开始，启动Boss主循环！");
            _isLoopRunning = true;
            MainLoop().Forget();
        }
        else if (newState != GameFlowManager.GameState.Gameplay && _isLoopRunning)
        {
            // 如果不是游戏状态（例如返回主菜单），则停止Boss的一切活动
            Debug.Log("[BossAI] 游戏暂停或结束，停止Boss活动。");
            _cancellationTokenSource.Cancel();
            _isLoopRunning = false;
        }
    }

    private async UniTask MainLoop()
    {
        // 开场，进入第一阶段
        await TransitionToPhase(BossPhase.Phase1);

        while (_currentPhase != BossPhase.Defeated && _isLoopRunning)
        {
            if (_isStunned)
            {
                await UniTask.WaitUntil(() => !_isStunned);
            }

            // 根据血量检测是否需要切换阶段
            BossPhase nextPhase = GetPhaseFromHealth();
            if (nextPhase != _currentPhase)
            {
                await TransitionToPhase(nextPhase);
            }

            await UniTask.Yield(); // 每帧检查一次
        }
    }

    private async UniTask TransitionToPhase(BossPhase newPhase)
    {
        if (_currentPhase == newPhase && newPhase != BossPhase.Phase1) return; // 避免重复进入同一阶段

        // 在切换到新阶段前，取消当前正在进行的所有攻击任务
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();

        _currentPhase = newPhase;
        Debug.LogWarning($"=============== Boss 进入新阶段: {newPhase} ===============");
        _animationController.PlayPhaseTransition();
        // 给一个小的过渡延迟
        await UniTask.Delay(TimeSpan.FromSeconds(2), cancellationToken: _cancellationTokenSource.Token);

        // 根据新阶段启动对应的攻击循环
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

    #region Attack Cycles

    // 第一阶段：冰霜
    private async UniTask Phase1_AttackCycle(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            // 攻击循环开始
            _animationController.PlayFrostBeamCast();
            await _skillCaster.CastFrostBeam(cancellationToken);
            await UniTask.Delay(TimeSpan.FromSeconds(3), cancellationToken: cancellationToken); // 间隔休息

            _animationController.PlayBlizzardCast();
            await _skillCaster.CastBlizzard(cancellationToken);
            
            _animationController.PlayStunned(); // Boss冻结自己
            Debug.Log("Boss冻结自己，进入5秒休息窗口。");
            await UniTask.Delay(TimeSpan.FromSeconds(5), cancellationToken: cancellationToken); // 全场休息

             _animationController.PlayIdle();
        }
    }

    // 第二阶段：烈火
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

    // 第三阶段：狂暴
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

    // 第四阶段：终焉
    private async UniTask Phase4_UltimateAttack()
    {
         _animationController.PlayUltimateCharge();
         await _skillCaster.CastUltimateAttack(
            onPlayerFail: () => {
                // 玩家失败逻辑，例如游戏结束
                Debug.Log("GAME OVER");
            },
            onPlayerSucceed: () => {
                // 玩家成功，Boss进入可被处决状态
                Debug.Log("玩家获得最后一击的机会！");
                _animationController.PlayStunned();
                // 在这里可以设置一个标志，允许玩家进行终结技
            }
         );
    }

    #endregion

    /// <summary>
    /// 外部调用，使Boss眩晕并打断当前技能。
    /// </summary>
    /// <param name="duration">眩晕持续时间（秒）</param>
    public async UniTask GetStunned(float duration)
    {
        if (_isStunned || _currentPhase == BossPhase.Defeated) return;

        _isStunned = true;
        Debug.LogWarning($"Boss 被眩晕 {duration} 秒！攻击被打断！");

        // 取消当前所有正在进行的攻击任务
        _cancellationTokenSource.Cancel();
        // _cancellationTokenSource.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();
        
        _animationController.PlayStunned();

        await UniTask.Delay(TimeSpan.FromSeconds(duration));

        Debug.Log("Boss 眩晕结束，恢复行动。");
        _isStunned = false;
        
        // 眩晕结束后，让主循环自动根据当前血量决定下一个行为
        // 如果不想让它立即攻击，可以再加一个短暂的延迟
        await UniTask.Delay(TimeSpan.FromSeconds(1));
        
        // 重新启动对应阶段的攻击循环
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
        // 取消所有任务
        _cancellationTokenSource.Cancel();
        // _cancellationTokenSource.Dispose();
        _animationController.PlayDeath();
        // 可以在这里停止所有活动，播放死亡动画等
        
        this.gameObject.SetActive(false);
        
    }

    private void OnDestroy()
    {
        // [修改] 在对象最终销g毁时，安全地释放资源
        // 这样可以确保Dispose只被调用一次，避免报错
        if (_cancellationTokenSource != null)
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null; // 显式设为null，好习惯
        }
    }
}