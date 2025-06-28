using UnityEngine;
using System.Collections.Generic;
using System.Linq; // 用于 RemoveAll (如果需要)

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

            // 确保组名在字典中存在（即使它最初为空列表）
            if (!managedGroups.ContainsKey(groupSetup.groupName))
            {
                managedGroups[groupSetup.groupName] = new List<UIFlyInOut>();
            }
            else
            {
                // 如果在Inspector中定义了同名组，这里的逻辑是后续的同名组会尝试合并面板
                // 但通常建议在Inspector中保持组名唯一
                Debug.LogWarning($"UIAnimationManager: 编辑器配置中发现重复的组名 '{groupSetup.groupName}'。面板将被添加到已存在的同名组中。");
            }

            // 将 Inspector 中配置的面板添加到对应的运行时组列表中
            if (groupSetup.panelsInGroup == null) continue;
            for (var index = 0; index < groupSetup.panelsInGroup.Count; index++)
            {
                var panel = groupSetup.panelsInGroup[index];
                if (panel)
                {
                    // 使用内部方法添加，避免在同一个组内重复添加
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
            // Debug.Log($"UIAnimationManager: 组 '{groupName}' 已成功创建。");
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
            if(!panel) Debug.LogWarning("UIAnimationManager: 尝试注销一个空的UI面板引用。");
            if(string.IsNullOrEmpty(groupName)) Debug.LogWarning("UIAnimationManager: 尝试从一个未命名的组注销面板。");
            return;
        }

        if (managedGroups.TryGetValue(groupName, out var groupList))
        {
            if (groupList.Remove(panel))
            {
                // Debug.Log($"UIAnimationManager: 面板 '{panel.name}' 已成功从组 '{groupName}' 注销。");
                // 可选: 如果组列表为空，是否移除该组?
                // if (groupList.Count == 0) { managedGroups.Remove(groupName); }
            }
            else
            {
                Debug.LogWarning($"UIAnimationManager: 面板 '{panel.name}' 在组 '{groupName}' 中未找到，无法注销。");
            }
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

        // string panelNameForLog = panel.name; // 避免panel在过程中被销毁导致name访问问题
        // bool removed = false;
        foreach (var groupPair in managedGroups.Where(groupPair => groupPair.Value.Remove(panel)))
        {
            // removed = true;
            // Debug.Log($"UIAnimationManager: 面板 '{panelNameForLog}' 已从组 '{groupPair.Key}' 注销。");
        }
        // if(!removed) Debug.LogWarning($"UIAnimationManager: 面板 '{panelNameForLog}' 未在任何管理的组中找到。");
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
            // Debug.Log($"UIAnimationManager: 准备显示组 '{groupName}' 中的 {groupList.Count} 个面板。");
            // 从后往前遍历，以便在找到null时安全地从列表中移除
            for (var i = groupList.Count - 1; i >= 0; i--)
            {
                var panel = groupList[i];
                if (panel)
                {
                    panel.Show();
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
            // Debug.Log($"UIAnimationManager: 准备隐藏组 '{groupName}' 中的 {groupList.Count} 个面板。");
            for (var i = groupList.Count - 1; i >= 0; i--)
            {
                var panel = groupList[i];
                if (panel)
                {
                    panel.Hide();
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
    /// (可选) 清理指定组或所有组中的空引用项（如果面板GameObject被意外销毁）。
    /// </summary>
    public void CleanUpNullReferencesInGroups(string specificGroupName = null)
    {
        if (string.IsNullOrEmpty(specificGroupName)) // 清理所有组
        {
            foreach (var groupPair in managedGroups)
            {
                groupPair.Value.RemoveAll(item => item == null);
            }
            // Debug.Log("UIAnimationManager: 已清理所有组中的空引用。");
        }
        else // 清理指定组
        {
            if (managedGroups.TryGetValue(specificGroupName, out List<UIFlyInOut> groupList))
            {
                groupList.RemoveAll(item => item == null);
                // Debug.Log($"UIAnimationManager: 已清理组 '{specificGroupName}' 中的空引用。");
            }
            else
            {
                Debug.LogWarning($"UIAnimationManager: 尝试清理不存在的组 '{specificGroupName}' 中的空引用。");
            }
        }
    }
}