using UnityEngine;
using UnityEngine.Tilemaps;

public class TileArray : MonoBehaviour
{
    public Transform forMin;      // 左下角世界坐标参考点
    public Transform forMax;      // 右上角世界坐标参考点
    public Tilemap tilemap;       // 主地图层
    public Tilemap obstacle;      // 障碍物地图层

    private Vector3Int cellMin;   // 对应的格子左下角坐标
    private Vector3Int cellMax;   // 对应的格子右上角坐标

    private int lengthX;
    private int lengthY;

    void Start()
    {
        Vector3 worldMin = forMin.position;
        Vector3 worldMax = forMax.position;

        cellMin = tilemap.WorldToCell(worldMin);
        cellMax = tilemap.WorldToCell(worldMax);

        Vector3Int bottomLeft = Vector3Int.Min(cellMin, cellMax);
        Vector3Int topRight   = Vector3Int.Max(cellMin, cellMax);
        cellMin = bottomLeft;
        cellMax = topRight;

        lengthX = cellMax.x - cellMin.x + 1;
        lengthY = cellMax.y - cellMin.y + 1;

        Debug.Log($"格子范围：左下 {cellMin}，右上 {cellMax}");
        Debug.Log($"大小：{lengthX} x {lengthY}");
    }

    // 将逻辑坐标 (0-based) 映射到实际格子坐标
    private Vector3Int MapToCell(int x, int y)
    {
        return new Vector3Int(cellMin.x + x, cellMin.y + y, 0);
    }

    // 设置 tile（主层）
    public void SetTile(int x, int y, TileBase tile)
    {
        Vector3Int cell = MapToCell(x, y);
        tilemap.SetTile(cell, tile);
    }

    // 获取 tile（主层）
    public TileBase GetTile(int x, int y)
    {
        Vector3Int cell = MapToCell(x, y);
        return tilemap.GetTile(cell);
    }

    // 清除 tile（主层）
    public void ClearTile(int x, int y)
    {
        Vector3Int cell = MapToCell(x, y);
        tilemap.SetTile(cell, null);
    }

    // 是否可以使用该格子（障碍层是否为空）
    public bool Useable(int x, int y)
    {
        Vector3Int cell = MapToCell(x, y);
        return obstacle.GetTile(cell) == null; // 没有障碍物则可用
    }

    // 获取逻辑地图尺寸
    public int Width => lengthX;
    public int Height => lengthY;
}
