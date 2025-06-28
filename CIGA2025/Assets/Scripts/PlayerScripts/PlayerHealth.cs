// PlayerHealth.cs
using UnityEngine;
using Cysharp.Threading.Tasks;
using System;

/// <summary>
/// 管理玩家的血量，并处理受伤、无敌帧和死亡逻辑。
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("血量设置")]
    [SerializeField] private int maxHealth = 5;
    private int currentHealth;

    [Header("受伤后设置")]
    [SerializeField] private float invincibilityDuration = 1.5f;
    private bool isInvincible = false;

    public event Action<int> OnHealthChanged;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damageUnits)
    {
        if (isInvincible || currentHealth <= 0)
        {
            return;
        }

        currentHealth -= damageUnits;
        currentHealth = Mathf.Max(currentHealth, 0);

        Debug.Log($"玩家受到 {damageUnits} 格伤害，剩余血量: {currentHealth}");
        OnHealthChanged?.Invoke(currentHealth);

        if (currentHealth > 0)
        {
            BecomeTemporarilyInvincible().Forget();
        }
        else
        {
            Die();
        }
    }

    private async UniTask BecomeTemporarilyInvincible()
    {
        isInvincible = true;
        Debug.Log("玩家进入无敌状态！");
        await UniTask.Delay(TimeSpan.FromSeconds(invincibilityDuration));
        isInvincible = false;
        Debug.Log("玩家无敌状态结束。");
    }

    private void Die()
    {
        Debug.LogError("--- 玩家已死亡！游戏结束。 ---");
    }

    #region [新增] GUI 血量显示

    private void OnGUI()
    {
        // === UI布局参数定义 ===
        // 血格的大小
        int heartSize = 30;
        // 血格之间的间距
        int heartSpacing = 8;
        // 血条距离屏幕左上角的边距
        int leftOffset = 15;
        int topOffset = 15;
        
        for (int i = 0; i < maxHealth; i++)
        {
            // 计算当前要绘制的这个血格在屏幕上的位置和大小
            Rect heartRect = new Rect(
                leftOffset + i * (heartSize + heartSpacing), // X坐标
                topOffset,                                   // Y坐标
                heartSize,                                   // 宽度
                heartSize                                    // 高度
            );

            // 判断这个血格是应该显示为“满血”还是“空血”
            if (i < currentHealth)
            {
                // 如果索引小于当前血量，说明这是有效的血格，画成红色
                DrawQuad(heartRect, Color.red);
            }
            else
            {
                // 否则，说明这是已失去的血格，画成深灰色
                DrawQuad(heartRect, new Color(0.25f, 0.25f, 0.25f)); // 深灰色
            }
        }
    }

    /// <summary>
    /// 一个简单的辅助方法，用于在指定的Rect区域绘制一个纯色方块。
    /// </summary>
    private void DrawQuad(Rect position, Color color)
    {
        // 创建一个1x1的纯色纹理
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        
        // 保存当前的GUI皮肤设置
        GUIStyle background = GUI.skin.box;
        GUI.skin.box.normal.background = texture;
        // 绘制一个没有文字的盒子，它会填充整个Rect区域
        GUI.Box(position, GUIContent.none);
        // 恢复原始的GUI皮肤设置，以免影响其他GUI元素
        GUI.skin.box = background;
    }

    #endregion
}