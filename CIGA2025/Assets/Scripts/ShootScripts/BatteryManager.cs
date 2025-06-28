using UnityEngine;
using UnityEngine.UI; // 引入UI命名空间，用于操作Image组件

/// <summary>
/// 全局单例电量管理器
/// </summary>
public class BatteryManager : MonoBehaviour
{
    // 1. 单例模式的核心
    private static BatteryManager _instance;

    // 公共静态属性，用于从任何地方访问该单例
    public static BatteryManager Instance
    {
        get
        {
            // 如果实例不存在，则在场景中查找
            if (_instance == null)
            {
                _instance = FindObjectOfType<BatteryManager>();
                // 如果场景中没有，则创建一个新的GameObject并挂载该脚本
                if (_instance == null)
                {
                    GameObject singletonObject = new GameObject("BatteryManager");
                    _instance = singletonObject.AddComponent<BatteryManager>();
                }
            }
            return _instance;
        }
    }

    // 2. 电池数据和UI引用
    [Header("电池状态精灵图 (共7个)")]
    [Tooltip("请按照电量从 0 到 6 的顺序拖入精灵图")]
    public Sprite[] batterySprites; // 存放0-6电量的7个精灵图

    [Header("UI设置")]
    [Tooltip("需要更新显示的电池UI按钮或图片")]
    public Image batteryImageUI; // 用于显示电池状态的UI Image组件

    private int _currentBatteryCount; // 当前玩家持有的电池数量

    // 3. 核心功能：属性和方法

    /// <summary>
    /// 获取或设置当前电池数量，并在设置时自动更新UI
    /// </summary>
    public int CurrentBatteryCount
    {
        get { return _currentBatteryCount; }
        set
        {
            // 使用Mathf.Clamp确保数值在0到6之间
            _currentBatteryCount = Mathf.Clamp(value, 0, 6);
            Debug.Log("电池数量已更新为: " + _currentBatteryCount);
            UpdateBatterySprite(); // 更新UI显示
        }
    }

    /// <summary>
    /// Awake在脚本实例被加载时调用
    /// </summary>
    private void Awake()
    {
        // 实现单例模式的持久化
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject); // 切换场景时保留该对象
        }
        else if (_instance != this)
        {
            // 如果已有实例存在，则销毁当前这个，保证只有一个
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 在游戏开始时设置初始状态
    /// </summary>
    private void Start()
    {
        // 假设游戏开始时有满电量 (6)
        // 你可以根据游戏设计在这里设置初始值
        CurrentBatteryCount = 6;
    }

    /// <summary>
    /// 根据当前的电池数量更新UI精灵图
    /// </summary>
    private void UpdateBatterySprite()
    {
        if (batteryImageUI == null)
        {
            Debug.LogError("错误：BatteryImageUI 未在Inspector中指定！");
            return;
        }

        if (batterySprites == null || batterySprites.Length != 7)
        {
            Debug.LogError("错误：batterySprites数组未正确配置，需要7个精灵图！");
            return;
        }

        // 确保索引不会越界
        if (_currentBatteryCount >= 0 && _currentBatteryCount < batterySprites.Length)
        {
            batteryImageUI.sprite = batterySprites[_currentBatteryCount];
        }
        else
        {
            Debug.LogWarning("警告：当前的电池数量 " + _currentBatteryCount + " 是一个无效的索引。");
        }
    }

    // --- 以下是供其他脚本调用的公共方法示例 ---

    /// <summary>
    /// 增加电池数量
    /// </summary>
    /// <param name="amount">增加的数量</param>
    public void AddBattery(int amount)
    {
        CurrentBatteryCount += amount;
    }

    /// <summary>
    /// 减少电池数量
    /// </summary>
    /// <param name="amount">减少的数量</param>
    public void UseBattery(int amount)
    {
        CurrentBatteryCount -= amount;
    }
}