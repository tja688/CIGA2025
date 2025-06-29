using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class UIPanelGroup
{
    [Tooltip("分组的名称，用于代码中调用")]
    public string groupName = "新分组";

    [Tooltip("属于这个分组的UI面板列表 (已挂载UIFlyInOut脚本)")]
    public List<UIFlyInOut> panelsInGroup = new List<UIFlyInOut>();
}