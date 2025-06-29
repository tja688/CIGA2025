// BossAnimationController.cs
using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

/// <summary>
/// [最终版本] Boss动画控制器。
/// 根据BossAI传递的当前阶段（Phase），自动播放对应主题的动画（冰/火）。
/// 管理所有独立的Live2D动画预制体，通过激活/失活来切换，确保动画唯一性。
/// </summary>
public class BossAnimationController : MonoBehaviour
{
    // 定义Boss的动画阶段，由BossAI在切换时设置
    public enum AnimPhase { General, Ice, Fire, Ultimate }

    // 用于在Inspector中清晰地配置动画
    [System.Serializable]
    public struct AnimationMapping
    {
        [Tooltip("动画的唯一名称，请严格按照配置清单填写 (例如: Ice_Idle, Fire_TakeDamage, Death)")]
        public string name;
        [Tooltip("对应的动画预制体")]
        public GameObject animationPrefab;
    }

    [Header("动画预制体列表")]
    [SerializeField] private List<AnimationMapping> animationMappings;

    // 当前Boss所处的动画阶段
    private AnimPhase _currentPhase = AnimPhase.Ice; // 默认为冰阶段

    private Dictionary<string, GameObject> _animationDict;
    private GameObject _currentActiveAnimation = null;

    void Awake()
    {
        _animationDict = new Dictionary<string, GameObject>();
        foreach (var mapping in animationMappings)
        {
            if (mapping.animationPrefab != null && !string.IsNullOrEmpty(mapping.name))
            {
                _animationDict.Add(mapping.name, mapping.animationPrefab);
                // 初始化时全部失活，避免场景加载时显示
                mapping.animationPrefab.SetActive(false); 
            }
        }
    }

    /// <summary>
    /// 由BossAI调用，用于设置当前的动画阶段主题
    /// </summary>
    public void SetAnimationPhase(AnimPhase phase)
    {
        Debug.Log($"[Animation] 动画阶段切换到: {phase}");
        _currentPhase = phase;
    }

    // 核心的动画切换方法，现在会根据阶段智能拼接动画名
    private void SwitchToAnimation(string baseName)
    {
        // 1. 根据当前阶段和基础动画名，生成最终要查找的动画名 (例如 "Idle" -> "Fire_Idle")
        string targetAnimationName = $"{_currentPhase}_{baseName}";

        // 2. 如果拼接后的名字（如 "Ice_Death"）不存在，则尝试查找通用动画（如 "Death"）
        if (!_animationDict.ContainsKey(targetAnimationName))
        {
            targetAnimationName = baseName; // 回退到通用动画名
        }

        // 3. 如果最终还是找不到，报错并安全返回，不改变当前状态
        if (!_animationDict.ContainsKey(targetAnimationName))
        {
            Debug.LogError($"[Animation] 找不到名为 '{_currentPhase}_{baseName}' 或 '{baseName}' 的动画预制体！请检查配置。");
            return;
        }

        GameObject nextAnimation = _animationDict[targetAnimationName];

        // 4. 确保唯一性：如果请求的动画不是当前动画，则隐藏当前动画
        if (_currentActiveAnimation != null && _currentActiveAnimation != nextAnimation)
        {
            _currentActiveAnimation.SetActive(false);
        }
        
        // 5. 激活新的动画并设为当前动画
        if (_currentActiveAnimation != nextAnimation)
        {
            nextAnimation.SetActive(true);
            _currentActiveAnimation = nextAnimation;
        }

        Debug.Log($"[Animation] Switched to: {targetAnimationName}");
    }

    // 处理“一次性”动画（如受击），播放后自动返回当前阶段的待机状态
    private async void HandleOneShotAnimation(string baseName, float duration)
    {
        SwitchToAnimation(baseName);
        
        // 确定刚才播放的一次性动画的全名，用于后续比较
        string oneShotFullName = _animationDict.ContainsKey($"{_currentPhase}_{baseName}") ? $"{_currentPhase}_{baseName}" : baseName;

        await UniTask.Delay(System.TimeSpan.FromSeconds(duration));
        
        // 安全检查：如果在等待期间动画状态被其他逻辑（如眩晕）改变了，则不执行返回待机的操作
        if(_currentActiveAnimation == _animationDict[oneShotFullName])
        {
            PlayIdle();
        }
    }

    // --- 公开接口保持不变，调用方式完全兼容 ---
    // 外部脚本（BossAI, BossHealth）无需关心内部的冰火状态切换逻辑

    public void PlayIdle() => SwitchToAnimation("Idle");
    public void PlayStunned() => SwitchToAnimation("Stunned");
    // 受击动画的持续时间可以在这里统一调整
    public void PlayTakeDamage() => HandleOneShotAnimation("TakeDamage", 0.5f); 
    public void PlayPhaseTransition() => SwitchToAnimation("PhaseTransition");
    public void PlayDeath() => SwitchToAnimation("Death");

    // === 技能动画 ===
    public void PlayFrostBeamCast() => SwitchToAnimation("FrostBeam");
    public void PlayBlizzardCast() => SwitchToAnimation("Blizzard");
    public void PlayAlternatingFlamesCast() => SwitchToAnimation("AlternatingFlames");
    public void PlayFlameWaveCast() => SwitchToAnimation("FlameWave");
    public void PlayGroundSpikesCast() => SwitchToAnimation("GroundSpikes");
    public void PlayUltimateCharge() => SwitchToAnimation("UltimateCharge");
    public void PlayUltimateFire() => SwitchToAnimation("UltimateFire");
}