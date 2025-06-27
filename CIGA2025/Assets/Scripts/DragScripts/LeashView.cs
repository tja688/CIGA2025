// LeashView.cs
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LeashView : MonoBehaviour
{
    private LineRenderer lineRenderer;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    // 更新牵引线的位置
    public void UpdateLeash(Vector3 startPoint, Vector3 endPoint)
    {
        lineRenderer.SetPosition(0, startPoint);
        lineRenderer.SetPosition(1, endPoint);
    }
}