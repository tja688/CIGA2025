// BossAI.cs
using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using System.Collections.Generic;

/// <summary>
/// [最终版本] Boss 的核心AI，使用FSM（有限状态机）控制行为。
/// 每次施放技能时，会从一个音效池中随机播放一个音效。
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
    
    // --- 【修改】施法音效设置 ---
    [Header("施法音效设置")]
    [Tooltip("Boss每次施放技能时，会从这个列表中随机挑选一个音效播放（例如：吼叫、咏唱声）")]
    public List<AudioConfigSO> skillCastSoundEffects;
    // --- 【删除】不再需要时间间隔变量 ---

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
        
        UpdateAnimationPhase(newPhase);

        if (newPhase == BossPhase.Phase4_Ultimate)
        {
            GameFlowManager.Instance.UpdateGameState(GameFlowManager.GameState.GameOver);
            return;
        }

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
                break;
        }
    }

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
            case BossPhase.Phase3:
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
        // 【删除】不再需要启动并行的音效循环
        while (!cancellationToken.IsCancellationRequested)
        {
            // --- 技能 1: 冰霜射线 ---
            PlayRandomSkillSfx(); // 【修改】在施法前播放随机音效
            _animationController.PlayFrostBeamCast();
            await _skillCaster.CastFrostBeam(cancellationToken);
            await UniTask.Delay(TimeSpan.FromSeconds(3), cancellationToken: cancellationToken);

            // --- 技能 2: 暴风雪 ---
            PlayRandomSkillSfx(); // 【修改】在施法前播放随机音效
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
            // --- 技能 1: 交替烈焰 ---
            PlayRandomSkillSfx(); // 【修改】在施法前播放随机音效
            _animationController.PlayAlternatingFlamesCast();
            await _skillCaster.CastAlternatingFlames(cancellationToken);
            await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: cancellationToken);
            
            // --- 技能 2: 烈焰波 ---
            PlayRandomSkillSfx(); // 【修改】在施法前播放随机音效
            _animationController.PlayFlameWaveCast();
            await _skillCaster.CastFlameWave(cancellationToken);
            await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: cancellationToken);

            // --- 技能 3: 地刺 ---
            PlayRandomSkillSfx(); // 【修改】在施法前播放随机音效
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
             // --- 技能 1: 冰霜射线 ---
             PlayRandomSkillSfx(); // 【修改】在施法前播放随机音效
             _animationController.PlayFrostBeamCast();
             await _skillCaster.CastFrostBeam(cancellationToken);
             await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: cancellationToken);

             // --- 技能 2: 烈焰波 ---
             PlayRandomSkillSfx(); // 【修改】在施法前播放随机音效
             _animationController.PlayFlameWaveCast();
             await _skillCaster.CastFlameWave(cancellationToken);
             await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: cancellationToken);

             // --- 技能 3: 地刺 ---
             PlayRandomSkillSfx(); // 【修改】在施法前播放随机音效
             _animationController.PlayGroundSpikesCast();
             await _skillCaster.CastGroundSpikes(cancellationToken);
             
             Debug.Log("狂暴模式一轮攻击结束，休息3秒。");
             await UniTask.Delay(TimeSpan.FromSeconds(3), cancellationToken: cancellationToken);
        }
    }

    #endregion

    // --- 【新增】播放随机施法音效的辅助方法 ---
    private void PlayRandomSkillSfx()
    {
        // 检查音效列表是否有效
        if (skillCastSoundEffects == null || skillCastSoundEffects.Count == 0)
        {
            return; // 如果列表为空，则不执行任何操作
        }

        // 从列表中随机选择一个音效
        int randomIndex = UnityEngine.Random.Range(0, skillCastSoundEffects.Count);
        AudioConfigSO sfxToPlay = skillCastSoundEffects[randomIndex];

        // 播放选中的音效（如果音效不为空）
        if (sfxToPlay != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.Play(sfxToPlay);
        }
    }
    // --- 【删除】旧的 PeriodicSfxLoop 方法 ---

    public async UniTask GetStunned(float duration)
    {
        if (_isStunned || _currentPhase == BossPhase.Defeated) return;

        _isStunned = true;
        Debug.LogWarning($"Boss 被眩晕 {duration} 秒！攻击被打断！");

        _cancellationTokenSource.Cancel();
        _cancellationTokenSource = new CancellationTokenSource();
        
        _animationController.PlayStunned();
        // 【建议】在被眩晕时也可以播放一个特定的随机音效
        PlayRandomSkillSfx(); 

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
        
        if(GameFlowManager.Instance.CurrentState != GameFlowManager.GameState.GameOver)
        {
            GameFlowManager.Instance.UpdateGameState(GameFlowManager.GameState.GameOver);
        }
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