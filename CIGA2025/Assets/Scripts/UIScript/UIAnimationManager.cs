using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 管理UI面板组的动画，并维护当前是否有任何面板处于可见状态。
/// </summary>
public class UIAnimationManager : MonoBehaviour
{
    // --- 单例模式实现 ---
    private static UIAnimationManager _instance;
    public static UIAnimationManager Instance
    {
        get
        {
            if (_instance) return _instance;
            _instance = FindObjectOfType<UIAnimationManager>();
            if (_instance) return _instance;
            var singletonObject = new GameObject(nameof(UIAnimationManager));
            _instance = singletonObject.AddComponent<UIAnimationManager>();
            return _instance;
        }
    }

    [Header("UI面板分组 (编辑器配置)")]
    [Tooltip("在此处配置UI面板组及其包含的面板。组名应唯一。")]
    public List<UIPanelGroup> inspectorDefinedGroups = new List<UIPanelGroup>();

    // ▼▼▼ 【核心修改】新增状态追踪部分 ▼▼▼
    [Header("运行时状态")]
    [Tooltip("当前所有被指令显示（正在飞入或已在屏幕上）的UI面板。")]
    private readonly HashSet<UIFlyInOut> visiblePanels = new HashSet<UIFlyInOut>();

    /// <summary>
    /// 公开属性，用于判断当前是否有任何UI面板处于可见状态。
    /// true 表示至少有一个面板被指令显示。
    /// </summary>
    public bool IsAnyPanelVisible => visiblePanels.Count > 0;
    // ▲▲▲ 【核心修改】新增状态追踪部分 ▲▲▲

    // 运行时用于高效管理组的字典
    private readonly Dictionary<string, List<UIFlyInOut>> managedGroups = new Dictionary<string, List<UIFlyInOut>>();

    private void Awake()
    {
        // 标准的单例模式 Awake 处理
        if (!_instance)
        {
            _instance = this;
            // 可选：如果希望此管理器在加载新场景时不被销毁
            // DontDestroyOnLoad(this.gameObject);
        }
        else if (_instance != this)
        {
            Debug.LogWarning($"UIAnimationManager: 场景中已存在实例 '{_instance.gameObject.name}'。正在销毁此重复实例 '{this.gameObject.name}'。");
            Destroy(this.gameObject);
            return; // 如果销毁此实例，则提前返回
        }

        InitializeGroupsFromInspector();
    }

    /// <summary>
    /// 从 Inspector 配置初始化运行时管理的组。
    /// </summary>
    private void InitializeGroupsFromInspector()
    {
        managedGroups.Clear();
        foreach (var groupSetup in inspectorDefinedGroups)
        {
            if (string.IsNullOrEmpty(groupSetup.groupName))
            {
                Debug.LogWarning("UIAnimationManager: 编辑器配置中发现一个未命名的组，已跳过。");
                continue;
            }

            if (!managedGroups.ContainsKey(groupSetup.groupName))
            {
                managedGroups[groupSetup.groupName] = new List<UIFlyInOut>();
            }
            else
            {
                Debug.LogWarning($"UIAnimationManager: 编辑器配置中发现重复的组名 '{groupSetup.groupName}'。面板将被添加到已存在的同名组中。");
            }

            if (groupSetup.panelsInGroup == null) continue;
            for (var index = 0; index < groupSetup.panelsInGroup.Count; index++)
            {
                var panel = groupSetup.panelsInGroup[index];
                if (panel)
                {
                    AddPanelToRuntimeGroupList(panel, groupSetup.groupName, false);
                }
            }
        }
    }

    /// <summary>
    /// 内部辅助方法：将面板添加到运行时组列表，并确保在单个组内不重复。
    /// </summary>
    private void AddPanelToRuntimeGroupList(UIFlyInOut panel, string groupName, bool logOnDuplicate = true)
    {
        if (!managedGroups.ContainsKey(groupName))
        {
            managedGroups[groupName] = new List<UIFlyInOut>();
        }

        var groupList = managedGroups[groupName];
        if (!groupList.Contains(panel))
        {
            groupList.Add(panel);
        }
        else if (logOnDuplicate)
        {
            Debug.LogWarning($"UIAnimationManager: 面板 '{panel.name}' 已经存在于组 '{groupName}' 中了。");
        }
    }

    // --- 公开的组管理 API ---

    /// <summary>
    /// 动态创建一个新的空组。
    /// </summary>
    public void CreateGroup(string groupName)
    {
        if (string.IsNullOrEmpty(groupName))
        {
            Debug.LogWarning("UIAnimationManager: 尝试创建一个未命名的组。");
            return;
        }
        if (!managedGroups.ContainsKey(groupName))
        {
            managedGroups[groupName] = new List<UIFlyInOut>();
        }
        else
        {
            Debug.LogWarning($"UIAnimationManager: 组 '{groupName}' 已经存在，无法重复创建。");
        }
    }

    /// <summary>
    /// 动态将一个UI面板注册到指定的组。
    /// </summary>
    public void RegisterPanelToGroup(UIFlyInOut panel, string groupName)
    {
        if (!panel)
        {
            Debug.LogWarning("UIAnimationManager: 尝试向组注册一个空的UI面板引用。");
            return;
        }
        if (string.IsNullOrEmpty(groupName))
        {
            Debug.LogWarning($"UIAnimationManager: 尝试为面板 '{panel.name}' 注册到一个未命名的组。");
            return;
        }

        AddPanelToRuntimeGroupList(panel, groupName);
    }

    /// <summary>
    /// 从指定的组中注销一个UI面板。
    /// </summary>
    public void UnregisterPanelFromGroup(UIFlyInOut panel, string groupName)
    {
        if (!panel || string.IsNullOrEmpty(groupName))
        {
            if (!panel) Debug.LogWarning("UIAnimationManager: 尝试注销一个空的UI面板引用。");
            if (string.IsNullOrEmpty(groupName)) Debug.LogWarning("UIAnimationManager: 尝试从一个未命名的组注销面板。");
            return;
        }

        if (managedGroups.TryGetValue(groupName, out var groupList))
        {
            groupList.Remove(panel);
        }
        else
        {
            Debug.LogWarning($"UIAnimationManager: 尝试从不存在的组 '{groupName}' 注销面板 '{panel.name}'。");
        }
    }

    /// <summary>
    /// 从所有管理的组中注销一个UI面板。
    /// </summary>
    public void UnregisterPanelFromAllGroups(UIFlyInOut panel)
    {
        if (!panel)
        {
            Debug.LogWarning("UIAnimationManager: 尝试从所有组注销一个空的UI面板引用。");
            return;
        }

        foreach (var groupPair in managedGroups)
        {
            groupPair.Value.Remove(panel);
        }
    }

    // --- 公开的组动画控制 API ---

    /// <summary>
    /// 统一激活指定组内所有UI面板的入场动画。
    /// </summary>
    public void ShowGroup(string groupName)
    {
        if (string.IsNullOrEmpty(groupName))
        {
            Debug.LogWarning("UIAnimationManager: ShowGroup 调用时未指定有效的组名。");
            return;
        }

        if (managedGroups.TryGetValue(groupName, out List<UIFlyInOut> groupList))
        {
            for (var i = groupList.Count - 1; i >= 0; i--)
            {
                var panel = groupList[i];
                if (panel)
                {
                    panel.Show();
                    // 追踪状态：将此面板添加到可见集合中
                    if (!visiblePanels.Contains(panel))
                    {
                        visiblePanels.Add(panel);
                    }
                }
                else
                {
                    Debug.LogWarning($"UIAnimationManager: 组 '{groupName}' 的面板列表中索引 {i} 处为一个空引用，已自动将其移除。");
                    groupList.RemoveAt(i);
                }
            }
        }
        else
        {
            Debug.LogWarning($"UIAnimationManager: 尝试显示一个不存在的组 '{groupName}'。");
        }
    }

    /// <summary>
    /// 统一激活指定组内所有UI面板的退场动画。
    /// </summary>
    public void HideGroup(string groupName)
    {
        if (string.IsNullOrEmpty(groupName))
        {
            Debug.LogWarning("UIAnimationManager: HideGroup 调用时未指定有效的组名。");
            return;
        }

        if (managedGroups.TryGetValue(groupName, out List<UIFlyInOut> groupList))
        {
            for (var i = groupList.Count - 1; i >= 0; i--)
            {
                var panel = groupList[i];
                if (panel)
                {
                    panel.Hide();
                    // 追踪状态：从此面板从可见集合中移除
                    visiblePanels.Remove(panel);
                }
                else
                {
                    Debug.LogWarning($"UIAnimationManager: 组 '{groupName}' 的面板列表中索引 {i} 处为一个空引用，已自动将其移除。");
                    groupList.RemoveAt(i);
                }
            }
        }
        else
        {
            Debug.LogWarning($"UIAnimationManager: 尝试隐藏一个不存在的组 '{groupName}'。");
        }
    }

    /// <summary>
    /// 清理指定组或所有组中的空引用项（如果面板GameObject被意外销毁）。
    /// </summary>
    public void CleanUpNullReferencesInGroups(string specificGroupName = null)
    {
        // 顺便清理追踪集合中的空引用，确保状态的绝对可靠
        visiblePanels.RemoveWhere(item => item == null);

        if (string.IsNullOrEmpty(specificGroupName)) // 清理所有组
        {
            foreach (var groupPair in managedGroups)
            {
                groupPair.Value.RemoveAll(item => item == null);
            }
        }
        else // 清理指定组
        {
            if (managedGroups.TryGetValue(specificGroupName, out List<UIFlyInOut> groupList))
            {
                groupList.RemoveAll(item => item == null);
            }
            else
            {
                Debug.LogWarning($"UIAnimationManager: 尝试清理不存在的组 '{specificGroupName}' 中的空引用。");
            }
        }
    }
}
