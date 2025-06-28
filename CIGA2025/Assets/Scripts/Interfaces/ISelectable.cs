// ISelectable.cs
using UnityEngine;

public interface ISelectable
{
    Bounds SelectionBounds { get; }
    
    /// <summary>
    /// 【新增】一个属性，用于判断该对象当前是否应被选择。
    /// </summary>
    bool IsSelectionEnabled { get; }

    void OnActivate();
}