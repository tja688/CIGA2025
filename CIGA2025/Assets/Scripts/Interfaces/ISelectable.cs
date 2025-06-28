// ISelectable.cs

using UnityEngine;

/// <summary>
/// 定义一个“可被选择”对象的契约。
/// 任何实现此接口的组件都必须提供一个用于框选的边界（Bounds），
/// 并且提供一个被激活时调用的方法。
/// </summary>
public interface ISelectable
{
    /// <summary>
    /// 获取用于定义选择框视觉效果的边界。
    /// 这是一个只读属性，动态返回当前的边界。
    /// </summary>
    Bounds SelectionBounds { get; }

    /// <summary>
    /// 【新增】当对象在拍照模式下被选中并被点击时调用此方法。
    /// </summary>
    void OnActivate();
}