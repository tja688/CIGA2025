// DamageZone.cs
using UnityEngine;

/// <summary>
/// 一个通用的伤害区域脚本。
/// 将此脚本附加到技能预制件上，当玩家进入其触发器时，会对玩家造成伤害。
/// </summary>
[RequireComponent(typeof(Collider2D))] // 确保对象上有碰撞体
public class DamageZone : MonoBehaviour
{
    [Header("伤害设置")]
    [Tooltip("此技能区域造成的伤害格数")]
    [SerializeField] private int damageAmount = 1;

    /// <summary>
    /// Unity物理引擎在有其他Collider进入此对象的触发器时自动调用此方法。
    /// </summary>
    /// <param name="other">进入触发器的另一个对象的Collider</param>
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 检查进入的是否是玩家 (通过标签)
        if (other.CompareTag("Player"))
        {
            // 尝试从玩家对象上获取PlayerHealth组件
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                // 如果成功获取，就调用其受伤方法
                playerHealth.TakeDamage(damageAmount);
            }
        }
    }
}