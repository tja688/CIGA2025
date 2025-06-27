// ArrowView.cs
using UnityEngine;

public class ArrowView : MonoBehaviour
{
    [SerializeField] private Transform arrowHead;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private float arrowHeadOffset = 0.5f; // 可根据箭头头部大小调整

    // 【重要】更新方法现在必须接收起点和终点
    public void UpdateArrow(Vector3 worldStartPosition, Vector3 worldEndPosition)
    {
        // 1. 让尾巴跟随：将整个视觉容器移动到起点
        transform.position = worldStartPosition;

        // 2. 计算方向和距离（在世界空间中）
        Vector3 direction = worldEndPosition - worldStartPosition;
        float distance = direction.magnitude;

        // 如果距离太近，就不用显示了，避免计算错误
        if (distance < 0.1f) 
        {
            lineRenderer.enabled = false;
            arrowHead.gameObject.SetActive(false);
            return;
        }
        lineRenderer.enabled = true;
        arrowHead.gameObject.SetActive(true);


        // 3. 更新LineRenderer
        // 它的坐标是本地的，所以终点就是方向向量本身
        lineRenderer.SetPosition(0, Vector3.zero);
        lineRenderer.SetPosition(1, transform.InverseTransformDirection(direction.normalized) * (distance - arrowHeadOffset));

        // 4. 更新箭头头部
        // 头部的位置就是世界终点，所以我们需要计算它的本地位置
        arrowHead.position = worldEndPosition;
        
        // 5. 【核心】使用Atan2进行旋转
        // 这个计算依赖于你的箭头精灵图片默认是朝向右方（X轴正方向）的
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        arrowHead.rotation = Quaternion.Euler(0, 0, angle);
    }
}