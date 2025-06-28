// BossAnimationController.cs
using UnityEngine;

/// <summary>
/// Boss动画控制器（占位符）。
/// BossAI将通过这个脚本调用所有动画。
/// 你只需要在这个脚本中填充实际的动画播放代码即可。
/// </summary>
public class BossAnimationController : MonoBehaviour
{
    // 你可以在这里获取Animator组件
    // private Animator animator;
    // void Awake() { animator = GetComponent<Animator>(); }

    public void PlayIdle() => Debug.Log("[Animation] 播放: 站立待机动画");
    public void PlayStunned() => Debug.Log("[Animation] 播放: 眩晕动画");
    public void PlayTakeDamage() => Debug.Log("[Animation] 播放: 受击动画");
    public void PlayPhaseTransition() => Debug.Log("[Animation] 播放: 阶段转换/怒吼动画");
    public void PlayDeath() => Debug.Log("[Animation] 播放: 死亡动画");

    // === 技能动画 ===
    public void PlayFrostBeamCast() => Debug.Log("[Animation] 播放: 寒冰射线施法动画");
    public void PlayBlizzardCast() => Debug.Log("[Animation] 播放: 暴风雪施法动画");
    public void PlayAlternatingFlamesCast() => Debug.Log("[Animation] 播放: 交替烈焰施法动画");
    public void PlayFlameWaveCast() => Debug.Log("[Animation] 播放: 火焰冲击波施法动画");
    public void PlayGroundSpikesCast() => Debug.Log("[Animation] 播放: 地面尖刺施法/捶胸口动画");
    public void PlayUltimateCharge() => Debug.Log("[Animation] 播放: 终极技能蓄力动画");
    public void PlayUltimateFire() => Debug.Log("[Animation] 播放: 终极技能释放动画");
}