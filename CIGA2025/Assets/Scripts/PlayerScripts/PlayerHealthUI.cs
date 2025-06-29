using UnityEngine;
using System.Collections.Generic; // 需要引入此命名空间以使用 List

public class PlayerHealthUI : MonoBehaviour
{
    [Header("组件关联")]
    [Tooltip("请拖入场景中带有 PlayerHealth 脚本的玩家对象")]
    [SerializeField] private PlayerHealth playerHealth;

    [Header("血量UI元素")]
    [Tooltip("请按顺序拖入代表血量的“遮挡物”或“伤害覆盖层”对象，第一个元素代表第一滴血的遮挡，以此类推。")]
    [SerializeField] private List<GameObject> healthMasks;

    void Start()
    {
        // 检查是否在Inspector中正确关联了 playerHealth
        if (playerHealth == null)
        {
            Debug.LogError("PlayerHealthUI 脚本未关联 PlayerHealth 组件！请在Inspector中拖入玩家对象。", this);
            return;
        }

        // 核心：订阅PlayerHealth的OnHealthChanged事件。
        // 每当血量变化时，PlayerHealth会“广播”这个事件，我们的UpdateHealthUI方法就会被自动调用。
        playerHealth.OnHealthChanged += UpdateHealthUI;

        // 【可选】如果你想在游戏一开始就根据玩家的初始血量设置一次UI，可以取消下面的注释。
        // 不过，在你提供的PlayerHealth脚本中，Start方法已经调用了OnHealthChanged事件，所以这一步通常是自动完成的。
        // UpdateHealthUI(playerHealth.maxHealth); // 假设初始是满血
    }

    private void OnDestroy()
    {
        // 当此UI对象被销毁时，为了防止内存泄漏，必须取消订阅事件。
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= UpdateHealthUI;
        }
    }

    /// <summary>
    /// 更新血量UI的核心方法。
    /// 根据当前血量，决定哪些遮挡物应该被激活。
    /// </summary>
    /// <param name="currentHealth">由PlayerHealth事件传来的当前血量值</param>
    private void UpdateHealthUI(int currentHealth)
    {
        // 遍历我们所有的血量遮挡物
        for (int i = 0; i < healthMasks.Count; i++)
        {
            // 这是一个巧妙的判断：
            // 假设我们有3个遮挡物（索引0, 1, 2）。
            // 如果 currentHealth = 2，那么：
            //   - 对于遮挡物0 (i=0), 0 >= 2 为 false, SetActive(false) -> 显示第1颗心
            //   - 对于遮挡物1 (i=1), 1 >= 2 为 false, SetActive(false) -> 显示第2颗心
            //   - 对于遮挡物2 (i=2), 2 >= 2 为 true,  SetActive(true)  -> 隐藏第3颗心
            // 这个逻辑完美匹配“当前血量为几，就显示几颗心”的需求。
            if (healthMasks[i] != null)
            {
                healthMasks[i].SetActive(i >= currentHealth);
            }
        }
    }
}