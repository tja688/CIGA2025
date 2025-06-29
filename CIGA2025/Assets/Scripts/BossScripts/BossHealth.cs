// BossHealth.cs
using UnityEngine;
using System;
using Cysharp.Threading.Tasks;
using DamageNumbersPro;
using DamageNumbersPro.Demo; // [新增] 引入DamageNumbersPro插件的命名空间

/// <summary>
/// 管理Boss的生命值，提供受伤和持续伤害（灼烧）效果的接口。
/// 已集成DamageNumbersPro插件以显示伤害数字。
/// </summary>
public class BossHealth : MonoBehaviour
{
    [Header("生命值设定")]
    [SerializeField] private float _maxHealth = 1000f;
    private float _currentHealth;

    // [新增] 用于在编辑器中拖入伤害数字的预制件
    [Header("视觉效果")]
    [Tooltip("从DamageNumbersPro插件中选择一个伤害数字预制件")]
    [SerializeField] private DamageNumber damageNumberPrefab;

    public float CurrentHealth => _currentHealth;
    public float MaxHealth => _maxHealth;

    public event Action<float, float> OnHealthChanged;
    public event Action OnDeath;

    private BossAnimationController _animationController;

    // [新增] 用于缓存Boss的碰撞体
    private Collider2D bossCollider; 

    private void Awake()
    {
        _animationController = GetComponent<BossAnimationController>();
        _currentHealth = _maxHealth;
        
        // [新增] 在开始时获取并缓存自身的碰撞体
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

        // [修改] 为了显示整数伤害，我们对伤害值取整
        int damageToShow = Mathf.RoundToInt(damageAmount);
        if (damageToShow <= 0) damageToShow = 1; // 至少显示1点伤害

        _currentHealth -= damageAmount;
        _currentHealth = Mathf.Max(_currentHealth, 0);
        
        // [新增] 在Boss位置生成伤害数字
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
        float damagePerTick = totalDamage / (duration * 10); // 每0.1秒造成一次伤害
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            if (_currentHealth <= 0) break;

            // 调用TakeDamage会自动处理伤害数字的显示，无需在此处额外操作
            TakeDamage(damagePerTick);
            await UniTask.Delay(TimeSpan.FromSeconds(0.1), ignoreTimeScale: false);
            elapsedTime += 0.1f;
        }
    }

    /// <summary>
    /// [修改] 生成伤害数字的核心方法
    /// </summary>
    private void SpawnDamageNumber(float damageAmount)
    {
        if (damageNumberPrefab == null) return;

        Vector3 spawnPosition;

        // [修改] 如果成功获取了碰撞体，则在其包围盒内生成随机位置
        if (bossCollider != null)
        {
            Bounds bounds = bossCollider.bounds;
            spawnPosition = new Vector3(
                UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
                UnityEngine.Random.Range(bounds.min.y, bounds.max.y),
                UnityEngine.Random.Range(bounds.min.z, bounds.max.z)
            );
        }
        else // 如果没有碰撞体，则使用旧的、固定的后备位置
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