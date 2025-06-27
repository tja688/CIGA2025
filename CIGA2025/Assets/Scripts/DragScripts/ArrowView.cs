// ArrowView.cs (修正坐标系Bug版本)
using UnityEngine;

public class ArrowView : MonoBehaviour
{
    [SerializeField] private Transform arrowHead;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private float arrowHeadOffset = 0.5f;
    
    public void Setup(Vector3 startPos)
    {
        // 将整个预制体（作为容器）放置在世界坐标的起点
        transform.position = startPos;
        // 初始化时，让箭头指向自己，避免闪烁
        UpdateArrow(startPos);
    }

    // 每帧更新箭头指向和长度（传入世界坐标的终点）
    public void UpdateArrow(Vector3 worldEndPosition)
    {
        // --- 关键修正 ---
        // 将世界坐标的鼠标位置，转换为此脚本所在对象的本地坐标
        Vector3 localEndPosition = transform.InverseTransformPoint(worldEndPosition);

        // 因为起点是(0,0,0)，所以方向向量就是本地终点坐标
        Vector3 direction = localEndPosition; 

        // 设置线条的终点（在本地坐标系中）
        lineRenderer.SetPosition(0, Vector3.zero); // 本地起点永远是(0,0,0)
        // 使用本地方向和距离来计算线条终点
        lineRenderer.SetPosition(1, direction.normalized * (direction.magnitude - arrowHeadOffset));

        // 设置箭头头部的本地位置
        arrowHead.localPosition = localEndPosition;

        // 让箭头头部的"右方"(x轴)朝向目标方向（在本地坐标系中）
        if (direction != Vector3.zero)
        {
            // 在本地坐标系中，我们希望头部的“up”方向是(0,0,1)，"right"方向是我们的direction
            arrowHead.localRotation = Quaternion.LookRotation(Vector3.forward, direction);
        }
    }
}