using UnityEngine;
using DamageNumbersPro; // 引入DamageNumbersPro插件的命名空间

namespace DamageNumbersPro.Demo
{
    /// <summary>
    /// 该脚本用于附加在任何希望在被2D物理碰撞时显示伤害数字的游戏对象上。
    /// 请确保该对象同时拥有 Rigidbody2D 和 Collider2D 组件。
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class DamageReceiver2D : MonoBehaviour
    {
        [Header("伤害设置")]
        [Tooltip("每次碰撞受到的固定伤害值。")]
        public float fixedDamageAmount = 25f;

        public GameObject DamagePrefab;
        
        /// <summary>
        /// 当一个带有Collider2D的对象与此对象发生碰撞时，Unity会自动调用此方法。
        /// </summary>
        /// <param name="collision">包含本次碰撞的详细信息，例如碰撞点。</param>
        private void OnCollisionEnter2D(Collision2D collision)
        {
            // 1. 获取伤害数字的预制件 (Prefab)
            // 我们通过DNP_DemoManager的单例来获取当前在Demo中选择的伤害数字样式。
            // 这种方式与您提供的DNP_2DDemo.cs中的做法保持一致。
            DamageNumber damagePrefab = DamagePrefab.GetComponent<DamageNumber>();

            // 安全检查：如果获取预制件失败，则在控制台输出错误信息并中止执行。
            if (damagePrefab == null)
            {
                Debug.LogError("无法从DNP_DemoManager获取伤害数字预制件！请确保场景中存在该管理器并已正确配置。");
                return;
            }

            // 2. 确定伤害数字的生成位置
            // 我们使用`collision.contacts[0].point`来获取碰撞发生的精确世界坐标。
            // 这是最理想的伤害数字弹出位置。
            Vector2 spawnPosition = collision.contacts[0].point;

            // 3. 生成伤害数字
            // 调用预制件的Spawn方法，在指定位置生成一个显示指定数值的伤害数字实例。
            DamageNumber newDamageNumber = damagePrefab.Spawn(spawnPosition, fixedDamageAmount);
            
            // 4. (可选) 应用与Demo一致的动态效果
            // 为了让效果和Demo中的完全一样（例如，特殊的移动、缩放动画等），
            // 我们可以获取并应用DNP_PrefabSettings。
            DNP_PrefabSettings prefabSettings = DamagePrefab.GetComponent<DNP_PrefabSettings>();
            if (prefabSettings != null)
            {
                prefabSettings.Apply(newDamageNumber);
            }

            // 您可以在此处添加额外的游戏逻辑，例如：
            // - 减少该对象的生命值
            // - 播放受击音效
            // - 触发短暂的无敌状态
            // Debug.Log(gameObject.name + " 被 " + collision.gameObject.name + " 撞击，受到了 " + fixedDamageAmount + " 点伤害！");
        }
    }
}