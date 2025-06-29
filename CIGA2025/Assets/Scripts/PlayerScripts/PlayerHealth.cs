// PlayerHealth.cs (修改后)
using UnityEngine;
using Cysharp.Threading.Tasks;
using System;

/// <summary>
/// 管理玩家的血量，并处理受伤、无敌帧逻辑。死亡和护盾逻辑与PlayerStatusController交互。
/// 【修改】现在还负责在受伤时触发音效和屏幕抖动。
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("血量设置")] 
    [SerializeField] private int maxHealth = 3;
    private int currentHealth;

    [Header("受伤后设置")] 
    [SerializeField] private float invincibilityDuration = 1.5f;
    private bool isInvincible = false;
    private bool _isShielded = false;

    // --- 【新增】效果关联 ---
    [Header("效果关联 (Audio & Camera)")]
    [Tooltip("玩家受到伤害时播放的音效")]
    [SerializeField] private AudioConfigSO damageSound;
    [Tooltip("玩家恢复生命时播放的音效")]
    [SerializeField] private AudioConfigSO healSound;
    [Tooltip("每次受伤时给摄像机增加的创伤值(建议0.1-1.0)")]
    [SerializeField] private float cameraShakeTrauma = 0.4f;
    // --- 新增结束 ---

    public event Action<int> OnHealthChanged;
    public event Action OnPlayerDied;
    public event Action OnDamageInterceptedByShield;

    void Start()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth);
    }

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

        if (_isShielded)
        {
            Debug.Log("护盾抵挡了一次伤害！");
            OnDamageInterceptedByShield?.Invoke();
            return;
        }

        currentHealth -= damageUnits;
        currentHealth = Mathf.Max(currentHealth, 0);

        Debug.Log($"玩家受到 {damageUnits} 点伤害，剩余血量: {currentHealth}");
        
        // --- 【新增】播放受伤效果 ---
        TriggerDamageEffects();
        // --- 新增结束 ---

        OnHealthChanged?.Invoke(currentHealth);

        if (currentHealth > 0)
        {
            BecomeTemporarilyInvincible().Forget();
        }
        else
        {
            OnPlayerDied?.Invoke();
        }
    }

    public void Heal(int amount)
    {
        if (currentHealth <= 0) return;

        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);

        Debug.Log($"玩家恢复了 {amount} 点生命，当前血量: {currentHealth}");
        
        // --- 【新增】播放恢复音效 ---
        if (AudioManager.Instance != null && healSound != null)
        {
            AudioManager.Instance.Play(healSound);
        }
        // --- 新增结束 ---
        
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

    /// <summary>
    /// 【新增】一个专门用于触发所有受伤效果（音效、震动等）的方法
    /// </summary>
    private void TriggerDamageEffects()
    {
        // 1. 播放受伤音效
        if (AudioManager.Instance != null && damageSound != null)
        {
            AudioManager.Instance.Play(damageSound);
        }

        // 2. 调用相机抖动
        if (CameraManager.Instance != null)
        {
            CameraManager.Instance.AddTrauma(cameraShakeTrauma);
        }
    }
}