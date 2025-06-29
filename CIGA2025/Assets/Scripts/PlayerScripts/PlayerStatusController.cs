using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Threading; // 引入线程命名空间以使用 CancellationTokenSource

/// <summary>
/// 统一管理玩家的状态效果和核心逻辑（如死亡）。
/// 提供供外部调用的接口，如冻结、治疗、激活护盾等。
/// </summary>
[RequireComponent(typeof(PlayerHealth), typeof(PlayerController))]
public class PlayerStatusController : MonoBehaviour
{
    private PlayerHealth _playerHealth;
    private PlayerController _playerController;

    [Header("护盾设置")]
    [Tooltip("用于在激活护盾时实例化的视觉效果对象")]
    public GameObject shieldVisualPrefab; 
    private bool _isShieldActive = false;
    private GameObject _currentShieldVisual;

    // 用于管理和取消冻结任务的CancellationTokenSource
    private CancellationTokenSource _freezeCts;

    void Awake()
    {
        _playerHealth = GetComponent<PlayerHealth>();
        _playerController = GetComponent<PlayerController>();
    }

    private void OnEnable()
    {
        if (_playerHealth != null)
        {
            _playerHealth.OnPlayerDied += HandlePlayerDeath;
            _playerHealth.OnDamageInterceptedByShield += DeactivateShield;
        }
    }

    private void OnDisable()
    {
        // 取消订阅，防止内存泄漏
        if (_playerHealth != null)
        {
            _playerHealth.OnPlayerDied -= HandlePlayerDeath;
            _playerHealth.OnDamageInterceptedByShield -= DeactivateShield;
        }
        
        // 在对象禁用时，取消正在进行的冻结任务，防止异常
        _freezeCts?.Cancel();
        _freezeCts?.Dispose();
    }
    
    /// <summary>
    /// ① 使玩家在指定时间内无法移动（健壮版）。
    /// </summary>
    /// <param name="duration">冻结的持续时间（秒）</param>
    public void FreezePlayer(float duration)
    {
        if (duration <= 0) return;

        // 如果已有冻结任务在进行，先取消它以刷新计时
        _freezeCts?.Cancel();
        _freezeCts?.Dispose();

        // 创建一个新的 CancellationTokenSource 来控制本次冻结任务
        _freezeCts = new CancellationTokenSource();
        
        // 启动新的异步冻结任务，并把 CancellationToken 传进去
        FreezePlayerAsync(duration, _freezeCts.Token).Forget();
    }

    private async UniTaskVoid FreezePlayerAsync(float duration, CancellationToken token)
    {
        try
        {
            // --- 冻结开始 ---
            Debug.Log($"玩家被冻结 {duration} 秒！输入已切换到UI模式。");
            
            // 1. 彻底禁用玩家移动输入
            PlayerInputController.Instance.ActivateUIControls();
            
            // 2. 作为双重保险，也更新移动脚本的状态
            _playerController.SetMovementEnabled(false);

            // 等待指定时间，此操作可被token取消
            await UniTask.Delay(TimeSpan.FromSeconds(duration), cancellationToken: token);

            // --- 冻结正常结束 ---
            Debug.Log("冻结时间到，恢复玩家控制。");
            
            // 检查游戏状态是否允许移动，如果允许，则恢复玩家输入
            if (GameFlowManager.Instance.CurrentState == GameFlowManager.GameState.Gameplay)
            {
                PlayerInputController.Instance.ActivatePlayerControls();
                _playerController.SetMovementEnabled(true);
            }
        }
        catch (OperationCanceledException)
        {
            // 当任务被取消时（例如被新的冻结效果覆盖），会捕获到这个异常
            Debug.Log("上一个冻结效果被新的冻结效果覆盖刷新。");
        }
        finally
        {
            // 确保CancellationTokenSource被释放
            if (token == _freezeCts?.Token)
            {
               _freezeCts?.Dispose();
               _freezeCts = null;
            }
        }
    }

    /// <summary>
    /// ② 为玩家恢复指定的生命值。
    /// </summary>
    /// <param name="amountToHeal">要恢复的血量值</param>
    public void HealPlayer(int amountToHeal)
    {
        if (_playerHealth != null)
        {
            _playerHealth.Heal(amountToHeal);
        }
    }

    /// <summary>
    /// ③ 激活一个可以抵御下一次伤害的护盾。
    /// </summary>
    public void ActivateShield()
    {
        if (_isShieldActive)
        {
            Debug.Log("护盾已经处于激活状态。");
            return;
        }

        _isShieldActive = true;
        _playerHealth.SetShieldStatus(true); // 通知PlayerHealth护盾已激活
        
        // 激活外部关联的护罩对象
        if (shieldVisualPrefab != null)
        {
            _currentShieldVisual = Instantiate(shieldVisualPrefab, transform.position, Quaternion.identity, transform);
            Debug.Log("护盾已激活！可以抵御下一次伤害。");
        }
    }
    
    /// <summary>
    /// 当伤害被护盾抵挡时，由事件调用此方法。
    /// </summary>
    private void DeactivateShield()
    {
        _isShieldActive = false;
        // PlayerHealth中的SetShieldStatus(false)已在TakeDamage中调用，这里无需重复
        _playerHealth.SetShieldStatus(false);

        if (_currentShieldVisual != null)
        {
            Destroy(_currentShieldVisual);
            _currentShieldVisual = null;
        }
        Debug.Log("护盾已破碎！");
    }

    /// <summary>
    /// [核心逻辑] 处理玩家死亡，由 PlayerHealth 的 OnPlayerDied 事件触发。
    /// </summary>
    private void HandlePlayerDeath()
    {
        Debug.LogError("--- 玩家已死亡！游戏结束。--- (由 PlayerStatusController 管理)");

        // 确保玩家不能再进行任何操作
        _playerController.SetMovementEnabled(false);
        PlayerInputController.Instance.ActivateUIControls(); // 切换到UI输入，禁止游戏操作
        
        // 禁用此脚本，防止进一步的状态改变
        this.enabled = false; 

        // 在这里可以添加更多死亡逻辑，例如：
        // 1. 播放死亡动画
        // 2. 延迟几秒后显示游戏结束UI
        // 3. 通知 GameFlowManager 改变游戏状态
        // GameFlowManager.Instance.ChangeGameState(GameFlowManager.GameState.GameOver);
    }
}