// File: SelectionBoxController.cs
using UnityEngine;

public class SelectionBoxController : MonoBehaviour
{
    [Header("四个角的Transform引用")]
    [SerializeField] private Transform topLeftCorner;
    [SerializeField] private Transform topRightCorner;
    [SerializeField] private Transform bottomLeftCorner;
    [SerializeField] private Transform bottomRightCorner;

    [SerializeField] private float padding = 0.1f;

    /// <summary>
    /// 根据目标物体的边界信息来更新四个角的位置
    /// </summary>
    /// <param name="targetBounds">目标物体的Bounds</param>
    public void UpdateBounds(Bounds targetBounds)
    {
        this.transform.position = targetBounds.center;

        float width = targetBounds.size.x + padding;
        float height = targetBounds.size.y + padding;
        
        topLeftCorner.localPosition = new Vector3(-width / 2, height / 2, 0);
        topRightCorner.localPosition = new Vector3(width / 2, height / 2, 0);
        bottomLeftCorner.localPosition = new Vector3(-width / 2, -height / 2, 0);
        bottomRightCorner.localPosition = new Vector3(width / 2, -height / 2, 0);
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }
}