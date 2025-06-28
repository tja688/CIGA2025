
using UnityEngine;

public class TestFireItem : ItemBase
{
    [Header("道具效果设置")]
    [Tooltip("对Boss造成的固定伤害值")]
    public float damageAmount = 80f;

    /// <summary>
    /// 这个方法在你希望通过代码主动激活道具时调用。
    /// 例如：从物品栏点击使用。
    /// </summary>
    public override void OnActivate()
    {
        base.OnActivate();
        Debug.Log("OnActivate: " + this.name);
        // 你可以在这里添加主动使用时的逻辑，比如朝前方发射此道具
    }
    
    
    /// <param name="collision">包含碰撞信息的对象</param>
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 1. 检查碰撞对象的标签是否为 "Boss"
        // 使用 CompareTag 比直接用 tag == "Boss" 效率更高
        if (collision.gameObject.CompareTag("Boss"))
        {
            Debug.Log(this.name + " 碰撞到了 " + collision.gameObject.name);

            // 2. 尝试从Boss对象上获取 BossHealth 组件
            BossHealth bossHealth = collision.gameObject.GetComponent<BossHealth>();

            // 3. 如果成功获取到 BossHealth 组件
            if (bossHealth != null)
            {
                // 调用其伤害方法，造成伤害
                Debug.Log("对Boss造成 " + damageAmount + " 点伤害！");
                bossHealth.ApplyBurnOverTime(damageAmount,2f);

                // 4. 伤害造成后，销毁道具自身
                Debug.Log("销毁道具: " + this.name);
                // Destroy(gameObject);
            }
            else
            {
                // 如果对方有 "Boss" 标签但没有 BossHealth 脚本，给出提示
                Debug.LogWarning(collision.gameObject.name + " 标签为Boss，但未找到 BossHealth 脚本！");
            }
        }
    }
}
