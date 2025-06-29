// SettingItem.cs
using UnityEngine;
using System.Collections;
using UnityEngine.UI; // 引入UI命名空间

/// <summary>
/// 一个特殊的道具“召唤台”。
/// 它本身是一个位于世界空间的UI图标。被激活时，它不会移动自己，
/// 而是会生成一个真正的、可投掷的道具预制体，并让其进入备战区。
/// 之后，此图标会进入冷却状态，并在冷却结束后才允许再次激活。
/// </summary>
public class SettingItem : ItemBase
{
    [Header("召唤设置")]
    [Tooltip("要生成的道具预制体。这个预制体上应挂载 TestItem, WanderController, Rigidbody2D 等组件")]
    [SerializeField] private GameObject projectilePrefab;

    [Tooltip("道具生成的位置和朝向，如果不设置，则默认在当前对象的位置生成")]
    [SerializeField] private Transform spawnPoint;

    [Header("冷却设置")]
    [Tooltip("激活后的冷却时间（秒）")]
    [SerializeField] private float cooldownDuration = 5.0f;

    [Header("冷却视觉效果")]
    [Tooltip("用于显示冷却状态的Image组件")]
    [SerializeField] private Image uiImage;

    [Tooltip("冷却时的颜色（例如：灰色）")]
    [SerializeField] private Color cooldownColor = Color.gray;

    [Tooltip("准备就绪时的颜色（例如：白色）")]
    [SerializeField] private Color readyColor = Color.white;
    

    private void Start()
    {
        // 如果没有在Inspector中指定UI Image，尝试自动获取
        if (uiImage == null)
        {
            uiImage = GetComponent<Image>();
        }

        // 确保游戏开始时图标处于“准备就绪”的颜色
        if (uiImage != null)
        {
            uiImage.color = readyColor;
        }
        else
        {
            Debug.LogWarning("SettingItem: 未找到Image组件，冷却的视觉效果将无法显示。", this);
        }

        // 确保生成点有效，若无效则使用自身位置
        if (spawnPoint == null)
        {
            spawnPoint = this.transform;
        }
    }

    /// <summary>
    /// 重写基类的激活方法。
    /// </summary>
    public override void OnActivate()
    {
        // 基类中的 isOnActive 状态现在被我们用作“是否在冷却中”的判断依据
        // 如果 IsSelectionEnabled 为 false (即 isOnActive 为 true)，说明正在冷却，直接返回
        if (!IsSelectionEnabled)
        {
            Debug.Log("正在冷却中，无法激活...");
            return;
        }

        // --- 核心逻辑 ---
        // 1. 生成道具
        SpawnProjectile();

        // 2. 开始冷却流程
        StartCoroutine(CooldownRoutine());
    }

    /// <summary>
    /// 生成并激活道具预制体
    /// </summary>
    private void SpawnProjectile()
    {
        if (projectilePrefab == null)
        {
            Debug.LogError("错误：道具预制体 (ProjectilePrefab) 未设置！", this);
            return;
        }

        // 在指定位置生成道具实例
        GameObject projectileInstance = Instantiate(projectilePrefab, spawnPoint.position, spawnPoint.rotation);
        Debug.Log($"已生成道具：{projectileInstance.name}");

        // 从新生成的实例上获取ItemBase组件（它应该是TestItem）
        ItemBase newItem = projectileInstance.GetComponent<ItemBase>();

        if (newItem != null && ActivationManager.Instance != null)
        {
            // 注意：这里我们调用基类的 OnActivate 方法，
            // 但目标是新生成的 newItem，而不是 this（UI图标自身）！
            // newItem.OnActivate() 将会调用 ActivationManager 来让其移动。
            newItem.OnActivate();
        }
        else
        {
            if (newItem == null)
            {
                Debug.LogError($"生成的预制体 {projectilePrefab.name} 上没有找到 ItemBase 的派生类脚本（如 TestItem）！", projectileInstance);
            }
            if (ActivationManager.Instance == null)
            {
                Debug.LogError("无法激活生成的道具：场景中未找到 ActivationManager！");
            }
        }
    }

    /// <summary>
    /// 处理冷却逻辑和视觉效果的协程
    /// </summary>
    private IEnumerator CooldownRoutine()
    {
        // 1. 进入冷却状态
        // 直接调用基类的 OnActivate() 来将 isOnActive 设置为 true。
        // 但为了避免递归调用，我们这里直接设置状态，或者确保基类方法可控。
        // 为了安全和清晰，我们重写了OnActivate，所以不再调用 base.OnActivate()。
        // 我们需要一种方式来设置基类的 isOnActive 状态，但它是私有的。
        // 啊，等等，在你的 ItemBase 里，OnActivate 内部会设置 isOnActive = true。
        // 但我们不能调用它，因为它会调用 ActivationManager。
        // 解决方案：修改 ItemBase，将 isOnActive 改为 protected。
        // **(假设你已经将 ItemBase 中的 'isOnActive' 修改为 'protected bool isOnActive;')**
        
        // 在ItemBase中，OnActivate方法调用了ActivationManager，然后设置isOnActive = true;
        // 我们的SettingItem的OnActivate不需要移动自己，所以我们不能调用base.OnActivate()
        // 因此，我们需要一种方式来设置基类的isOnActive状态。
        // 最好的方法是修改ItemBase，将 `private bool isOnActive` 改为 `protected bool isOnActive`
        
        // --------------------------------------------------------------------------
        // **重要修改建议：**
        // 请打开 `ItemBase.cs` 文件，将 `private bool isOnActive = false;`
        // 修改为 `protected bool isOnActive = false;`
        // 这样派生类就可以直接访问和修改它了。
        // --------------------------------------------------------------------------
        
        isOnActive = true; // 设置为“冷却中”状态 (这将使 IsSelectionEnabled 返回 false)

        // 2. 更新视觉效果
        if (uiImage != null)
        {
            float elapsedTime = 0f;
            while (elapsedTime < cooldownDuration)
            {
                // 使用Lerp（线性插值）平滑地将颜色从“冷却色”变到“就绪色”
                uiImage.color = Color.Lerp(cooldownColor, readyColor, elapsedTime / cooldownDuration);
                elapsedTime += Time.deltaTime;
                yield return null; // 等待下一帧
            }
            // 确保冷却结束后颜色精确地设置为“就绪色”
            uiImage.color = readyColor;
        }
        else
        {
            // 如果没有UI Image，就只等待时间
            yield return new WaitForSeconds(cooldownDuration);
        }
        
        // 3. 冷却结束
        isOnActive = false; // 恢复“可激活”状态
        Debug.Log("冷却结束，可以再次激活！");
    }
}