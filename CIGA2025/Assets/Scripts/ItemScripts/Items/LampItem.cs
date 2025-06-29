using UnityEngine;

// [RequireComponent] 是一个很好的实践，它可以确保此脚本所在的游戏对象
// 必须拥有一个 Animator 组件，如果没有，Unity 会在添加此脚本时自动创建一个。
[RequireComponent(typeof(Animator))]
public class LampItem : ItemBase
{
    private static readonly int Live = Animator.StringToHash("Live");

    // 缓存 Animator 组件的引用，避免每次都调用 GetComponent，效率更高
    private Animator _animator;

    /// <summary>
    /// 重写基类的 Awake 方法，用于获取 LampItem 特有的组件。
    /// </summary>
    protected override void Awake()
    {
        // 【重要】必须先调用基类的 Awake() 方法！
        // 这样可以确保 ItemBase 中的所有初始化逻辑（如获取刚体、设置质量等）都能被正确执行。
        base.Awake();

        // 获取挂载在同一个游戏对象上的 Animator 组件
        _animator = GetComponent<Animator>();
    }

    /// <summary>
    /// 重写基类的 OnActivate 方法，来添加 LampItem 专属的激活行为。
    /// </summary>
    public override void OnActivate()
    {
        // 【重要】同样，先调用基类的 OnActivate() 方法。
        // 这会执行基类中的通用激活逻辑（比如通知 ActivationManager）。
        base.OnActivate();
        
        // --- 从这里开始，是 LampItem 的专属激活逻辑 ---
        
        // 检查是否成功获取了 Animator 组件
        if (_animator != null)
        {
            // 触发名为 "Live" 的动画触发器
            _animator.SetTrigger(Live);
            Debug.Log(this.name + " 已被激活，触发 'Live' 动画。");
        }
        else
        {
            // 如果在 Awake 中没有找到 Animator，这里会给出一个警告
            Debug.LogWarning("无法播放激活动画：在 " + this.name + " 上没有找到 Animator 组件！");
        }
    }
}