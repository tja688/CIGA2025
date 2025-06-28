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
    private Animator animator;
     void Awake() { animator = GetComponent<Animator>(); }
        private void SetAnimationTrigger(string triggerName)
    {
        if (animator != null)
        {
            animator.SetTrigger(triggerName);
            Debug.Log($"[Animation] Triggered: {triggerName}"); // 仍然保留日志，方便调试
        }
        else
        {
            Debug.LogWarning($"Attempted to play animation '{triggerName}', but Animator is null.");
        }
    }
    // 直接使用字符串字面量调用 SetAnimationTrigger
    public void PlayIdle() => SetAnimationTrigger("PlayIdle");
    public void PlayStunned() => SetAnimationTrigger("PlayStunned");
    public void PlayTakeDamage() => SetAnimationTrigger("PlayTakeDamage");
    public void PlayPhaseTransition() => SetAnimationTrigger("PlayPhaseTransition");
    public void PlayDeath() => SetAnimationTrigger("PlayDeath");

    // === 技能动画 ===
    public void PlayFrostBeamCast() => SetAnimationTrigger("PlayFrostBeamCast");
    public void PlayBlizzardCast() => SetAnimationTrigger("PlayBlizzardCast");
    public void PlayAlternatingFlamesCast() => SetAnimationTrigger("PlayAlternatingFlamesCast");
    public void PlayFlameWaveCast() => SetAnimationTrigger("PlayFlameWaveCast");
    public void PlayGroundSpikesCast() => SetAnimationTrigger("PlayGroundSpikesCast");
    public void PlayUltimateCharge() => SetAnimationTrigger("PlayUltimateCharge");
    public void PlayUltimateFire() => SetAnimationTrigger("PlayUltimateFire");
}