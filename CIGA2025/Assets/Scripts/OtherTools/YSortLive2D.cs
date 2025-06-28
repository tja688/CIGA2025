using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class Live2DYSortAndLayerSetup : MonoBehaviour
{
    [Header("排序图层设置")]
    [Tooltip("要为该模型设置的固定排序图层名称")]
    [SerializeField] private string sortingLayerName = "GameObject";

    [Header("Y轴动态排序设置")]
    [Tooltip("Y轴坐标到排序顺序的转换系数")]
    [SerializeField] private float positionToOrderFactor = 100f;

    [Tooltip("基础排序偏移量，用于微调")]
    [SerializeField] private int sortingOrderOffset = 0;

    // 存储每个渲染器及其初始相对排序值的字典
    private Dictionary<MeshRenderer, int> initialSortingOrders;

    void Start()
    {
        initialSortingOrders = new Dictionary<MeshRenderer, int>();
        
        // 获取所有子对象中的 MeshRenderer 组件
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>(true);

        if (renderers.Length == 0)
        {
            Debug.LogWarning("在 " + gameObject.name + " 的子对象中没有找到任何 MeshRenderer 组件。", this);
            this.enabled = false;
            return;
        }

        // --- 新增功能：设置 Sorting Layer ---
        // 检查指定的 Sorting Layer 是否存在，以防拼写错误
        if (SortingLayer.NameToID(sortingLayerName) == 0 && sortingLayerName != "Default") {
             Debug.LogError($"错误：名为 '{sortingLayerName}' 的 Sorting Layer 不存在！请检查拼写或前往 'Project Settings -> Tags and Layers' 中添加。", this);
             this.enabled = false; // 发生错误则禁用脚本
             return;
        }

        foreach (var renderer in renderers)
        {
            // 1. 设置 Sorting Layer
            renderer.sortingLayerName = sortingLayerName;

            // 2. 记录初始的 Order in Layer 以便后续计算
            initialSortingOrders[renderer] = renderer.sortingOrder;
        }

        Debug.Log($"已将 {gameObject.name} 的所有网格渲染器 Sorting Layer 设置为 '{sortingLayerName}'。");
    }

    void LateUpdate()
    {
        if (initialSortingOrders == null || initialSortingOrders.Count == 0)
        {
            return; // 如果初始化失败则不执行更新
        }
        
        // 根据根对象的 Y 轴位置计算新的基础排序值
        int baseSortingOrder = -(int)(transform.position.y * positionToOrderFactor) + sortingOrderOffset;

        // 更新每一个 MeshRenderer 的 sortingOrder
        foreach (var rendererPair in initialSortingOrders)
        {
            MeshRenderer renderer = rendererPair.Key;
            int initialRelativeOrder = rendererPair.Value;

            if (renderer != null)
            {
                // 新的顺序 = 基础顺序 + 初始的相对顺序
                renderer.sortingOrder = baseSortingOrder + initialRelativeOrder;
            }
        }
    }
}