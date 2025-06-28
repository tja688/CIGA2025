using UnityEngine;

public class ActivationManager : MonoBehaviour
{
    #region 单例模式
    public static ActivationManager Instance { get; private set; }
    #endregion

    [Header("激活区域")]
    [Tooltip("激活后物体会移动到这两个区域之一")]
    [SerializeField] private BoxCollider2D activationZone1;
    [SerializeField] private BoxCollider2D activationZone2;

    [Header("销毁边界")]
    [Tooltip("物体离开此触发器区域后将被销毁")]
    [SerializeField] private BoxCollider2D destructionZone;

    [Header("游荡速度配置")]
    [Tooltip("冲向目标区域的初始速度")]
    [SerializeField] private float initialMoveSpeed = 5f; // 【新增】初始移动速度
    [SerializeField] private float minWanderSpeed = 0.5f;
    [SerializeField] private float maxWanderSpeed = 1.5f;
    
    // 公开属性，以便WanderController可以访问
    public Collider2D DestructionZoneCollider => destructionZone;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 安全检查
        if (activationZone1 == null || activationZone2 == null || destructionZone == null)
        {
            Debug.LogError("ActivationManager: 请在Inspector中配置所有区域！", this);
        }
        else
        {
            if (!destructionZone.isTrigger)
            {
                Debug.LogWarning("ActivationManager: Destruction Zone 应该是一个触发器 (Is Trigger = true)！", destructionZone);
            }
        }
    }

    /// <summary>
    /// 激活一个物体，使其开始游荡
    /// </summary>
    /// <param name="itemToActivate">需要被激活的物体</param>
    public void Activate(ItemBase itemToActivate)
    {
        WanderController wanderer = itemToActivate.GetComponent<WanderController>();
        if (wanderer == null)
        {
            Debug.LogWarning($"物体 {itemToActivate.name} 被尝试激活，但它没有 WanderController 组件。", itemToActivate);
            return;
        }

        // 1. 随机挑选一个激活区域
        BoxCollider2D chosenZone = Random.Range(0, 2) == 0 ? activationZone1 : activationZone2;

        // 2. 随机生成一个固定的游荡速度
        float randomSpeed = Random.Range(minWanderSpeed, maxWanderSpeed);

        // 3. 命令物体的 WanderController 开始游荡
        wanderer.StartWandering(chosenZone.bounds, initialMoveSpeed, randomSpeed, destructionZone);
    }
}