// PlayerHealth.cs (修改后)
using UnityEngine;
using Cysharp.Threading.Tasks;
using System;

/// <summary>
/// 管理玩家的血量，并处理受伤、无敌帧逻辑。死亡和护盾逻辑与PlayerStatusController交互。
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("血量设置")] [SerializeField] private int maxHealth = 3;
    private int currentHealth;

    [Header("受伤后设置")] [SerializeField] private float invincibilityDuration = 1.5f;
    private bool isInvincible = false;
    private bool _isShielded = false; // 【新增】护盾状态

    // 【修改】定义更具体的事件
    public event Action<int> OnHealthChanged;
    public event Action OnPlayerDied; // 【新增】专门用于通知死亡的事件
    public event Action OnDamageInterceptedByShield; // 【新增】伤害被护盾抵挡时触发

    void Start()
    {
        currentHealth = maxHealth;
        // 初始时通过事件更新UI
        OnHealthChanged?.Invoke(currentHealth);
    }

    /// <summary>
    /// 【新增】由PlayerStatusController调用，设置护盾状态
    /// </summary>
    public void SetShieldStatus(bool shielded)
    {
        _isShielded = shielded;
    }

    public void TakeDamage(int damageUnits)
    {
        if (isInvincible || currentHealth <= 0)
        {
            return;
        }

        // 【新增】检查护盾
        if (_isShielded)
        {
            Debug.Log("护盾抵挡了一次伤害！");
            OnDamageInterceptedByShield?.Invoke(); // 通知护盾被消耗
            return; // 伤害被完全抵挡，直接返回
        }

        currentHealth -= damageUnits;
        currentHealth = Mathf.Max(currentHealth, 0);

        Debug.Log($"玩家受到 {damageUnits} 点伤害，剩余血量: {currentHealth}");
        OnHealthChanged?.Invoke(currentHealth);

        if (currentHealth > 0)
        {
            BecomeTemporarilyInvincible().Forget();
        }
        else
        {
            // 【修改】不再直接调用Die()，而是触发死亡事件
            OnPlayerDied?.Invoke();
        }
    }

    /// <summary>
    /// 【新增】治疗方法
    /// </summary>
    public void Heal(int amount)
    {
        if (currentHealth <= 0) return; // 死亡后无法治疗

        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth); // 血量不能超过上限

        Debug.Log($"玩家恢复了 {amount} 点生命，当前血量: {currentHealth}");
        OnHealthChanged?.Invoke(currentHealth);
    }


    private async UniTask BecomeTemporarilyInvincible()
    {
        isInvincible = true;
        Debug.Log("玩家进入无敌状态！");
        await UniTask.Delay(TimeSpan.FromSeconds(invincibilityDuration));
        isInvincible = false;
        Debug.Log("玩家无敌状态结束。");
    }
}