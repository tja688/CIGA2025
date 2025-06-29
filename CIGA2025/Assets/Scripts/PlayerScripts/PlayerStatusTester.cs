using UnityEngine;

/// <summary>
/// 一个用于开发和调试的测试脚本。
/// 通过按键来触发 PlayerStatusController 上的各种效果。
/// </summary>
public class PlayerStatusTester : MonoBehaviour
{
    [Header("要控制的玩家")]
    [Tooltip("将场景中的玩家对象拖到这里")]
    public PlayerStatusController playerStatusController;

    [Header("测试参数")]
    [Tooltip("按下'7'键时，玩家被冻结的秒数")]
    public float freezeDuration = 3.0f;

    [Tooltip("按下'8'键时，玩家恢复的生命值")]
    public int healAmount = 1;
    
    [Tooltip("按下'0'键时，对玩家造成的伤害值")]
    public int damageAmount = 1;

    // 为了方便测试受伤，我们直接获取玩家的Health组件
    private PlayerHealth _playerHealth;

    void Start()
    {
        // 确保已经关联了玩家
        if (playerStatusController == null)
        {
            Debug.LogError("请在 PlayerStatusTester 组件上指定玩家对象！");
            // 尝试自动查找，如果场景中只有一个玩家的话
            playerStatusController = FindObjectOfType<PlayerStatusController>();
            if (playerStatusController == null)
            {
                 enabled = false; // 如果找不到，就禁用此脚本
                 return;
            }
        }

        // 获取关联玩家的PlayerHealth组件
        _playerHealth = playerStatusController.GetComponent<PlayerHealth>();
    }

    void Update()
    {
        // 如果没有正确设置，则不执行任何操作
        if (playerStatusController == null || _playerHealth == null)
        {
            return;
        }

        // --- 按键测试 ---

        // 按下 '7' 键：测试冻结效果
        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            Debug.Log("[测试] 按下 7 键，尝试冻结玩家。");
            playerStatusController.FreezePlayer(freezeDuration);
        }

        // 按下 '8' 键：测试回血效果
        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            Debug.Log("[测试] 按下 8 键，尝试为玩家回血。");
            playerStatusController.HealPlayer(healAmount);
        }

        // 按下 '9' 键：测试激活护盾
        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            Debug.Log("[测试] 按下 9 键，尝试激活护盾。");
            playerStatusController.ActivateShield();
        }

        // 按下 '0' 键：测试受伤（用于验证护盾和无敌帧）
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            Debug.Log($"[测试] 按下 0 键，尝试对玩家造成 {damageAmount} 点伤害。");
            _playerHealth.TakeDamage(damageAmount);
        }
    }
}