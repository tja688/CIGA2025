// BossHealth.cs
using UnityEngine;
using System;
using Cysharp.Threading.Tasks;
using DamageNumbersPro;
using DamageNumbersPro.Demo;

/// <summary>
/// 管理Boss的生命值，提供受伤和持续伤害（灼烧）效果的接口。
/// 已集成DamageNumbersPro插件以显示伤害数字，并能根据伤害值播放不同音效。
/// </summary>
public class BossHealth : MonoBehaviour
{
    [Header("生命值设定")]
    [SerializeField] private float _maxHealth = 1000f;
    private float _currentHealth;

    [Header("视觉效果")]
    [Tooltip("从DamageNumbersPro插件中选择一个伤害数字预制件")]
    [SerializeField] private DamageNumber damageNumberPrefab;

    // 【新增】音效设置
    [Header("音效设置")]
    [Tooltip("单次受伤超过 50 时播放的音效")]
    [SerializeField] private AudioConfigSO heavyDamageSound;
    [Tooltip("单次受伤超过 30 时播放的音效")]
    [SerializeField] private AudioConfigSO mediumDamageSound;


    public float CurrentHealth => _currentHealth;
    public float MaxHealth => _maxHealth;

    public event Action<float, float> OnHealthChanged;
    public event Action OnDeath;

    private BossAnimationController _animationController;
    private Collider2D bossCollider; 

    private void Awake()
    {
        _animationController = GetComponent<BossAnimationController>();
        _currentHealth = _maxHealth;
        
        bossCollider = GetComponent<Collider2D>();
        if(bossCollider == null)
        {
            Debug.LogError("Boss身上没有找到Collider组件，伤害数字将出现在默认位置！");
        }
    }


    /// <summary>
    /// 对Boss造成一次性伤害。
    /// </summary>
    /// <param name="damageAmount">伤害数值</param>
    public void TakeDamage(float damageAmount)
    {
        if (_currentHealth <= 0) return;

        // 【修改】根据伤害值播放对应的音效
        PlayDamageSound(damageAmount);

        // 为了显示整数伤害，我们对伤害值取整
        int damageToShow = Mathf.RoundToInt(damageAmount);
        if (damageToShow <= 0) damageToShow = 1; // 至少显示1点伤害

        _currentHealth -= damageAmount;
        _currentHealth = Mathf.Max(_currentHealth, 0);
        
        // 在Boss位置生成伤害数字
        SpawnDamageNumber(damageToShow);

        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        _animationController.PlayTakeDamage();

        if (_currentHealth <= 0)
        {
            OnDeath?.Invoke();
        }
    }

    /// <summary>
    /// 对Boss施加一个持续的灼烧效果。
    /// </summary>
    /// <param name="totalDamage">总伤害</param>
    /// <param name="duration">持续时间（秒）</param>
    public async UniTask ApplyBurnOverTime(float totalDamage, float duration)
    {
        // 灼烧伤害的每一跳会分别触发TakeDamage，如果单跳伤害超过阈值也会触发音效
        float damagePerTick = totalDamage / (duration * 10); // 每0.1秒造成一次伤害
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            if (_currentHealth <= 0) break;

            TakeDamage(damagePerTick);
            await UniTask.Delay(TimeSpan.FromSeconds(0.1), ignoreTimeScale: false);
            elapsedTime += 0.1f;
        }
    }

    /// <summary>
    /// 【新增】根据伤害值播放音效的核心逻辑
    /// </summary>
    private void PlayDamageSound(float damageAmount)
    {
        // 优先检查高伤害
        if (damageAmount > 50 && heavyDamageSound != null)
        {
            AudioManager.Instance.Play(heavyDamageSound);
        }
        // 再检查中等伤害
        else if (damageAmount > 30 && mediumDamageSound != null)
        {
            AudioManager.Instance.Play(mediumDamageSound);
        }
    }


    /// <summary>
    /// 生成伤害数字的核心方法
    /// </summary>
    private void SpawnDamageNumber(float damageAmount)
    {
        if (damageNumberPrefab == null) return;

        Vector3 spawnPosition;

        if (bossCollider != null)
        {
            Bounds bounds = bossCollider.bounds;
            spawnPosition = new Vector3(
                UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
                UnityEngine.Random.Range(bounds.min.y, bounds.max.y),
                UnityEngine.Random.Range(bounds.min.z, bounds.max.z)
            );
        }
        else
        {
            spawnPosition = transform.position + Vector3.up * 1.0f;
        }

        DamageNumber newDamageNumber = damageNumberPrefab.Spawn(spawnPosition, damageAmount);
        
        DNP_PrefabSettings prefabSettings = damageNumberPrefab.GetComponent<DNP_PrefabSettings>();
        if (prefabSettings != null)
        {
            prefabSettings.Apply(newDamageNumber);
        }
    }

    [ContextMenu("Test Apply 5s Burn (100 dmg)")]
    private void TestBurn()
    {
        ApplyBurnOverTime(100, 5f).Forget();
    }
}