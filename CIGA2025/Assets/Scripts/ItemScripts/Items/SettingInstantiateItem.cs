using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SettingInstantiateItem : ItemBase
{
    void Start()
    {
        isOnActive = true;
    }

    /// <summary>
    /// Unity物理引擎在发生碰撞时自动调用的方法 (用于3D物理)。
    /// </summary>
    /// <param name="collision">包含碰撞信息的对象</param>
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 1. 检查碰撞对象的标签是否为 "Boss"
        // 使用 CompareTag 比直接用 tag == "Boss" 效率更高
        if (collision.gameObject.CompareTag("Boss"))
        {

            BossHealth bossHealth = collision.gameObject.GetComponent<BossHealth>();

            // 3. 如果成功获取到 BossHealth 组件
            if (bossHealth != null)
            {
                bossHealth.TakeDamage(1);

                Destroy(gameObject);
            }
            else
            {
                // 如果对方有 "Boss" 标签但没有 BossHealth 脚本，给出提示
                Debug.LogWarning(collision.gameObject.name + " 标签为Boss，但未找到 BossHealth 脚本！");
            }
        }
    }

}
