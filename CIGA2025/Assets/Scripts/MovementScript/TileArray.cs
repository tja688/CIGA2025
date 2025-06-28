using UnityEngine;
using UnityEngine.Tilemaps;

public class TileArray : MonoBehaviour
{
    public Transform forMin;      // 左下角参考点
    public Transform forMax;      // 右上角参考点
    public Tilemap tilemap;       // 主图层
    public Tilemap obstacle;      // 障碍层（可选）

    private Vector3Int cellMin;
    private Vector3Int cellMax;

    private int lengthX;
    private int lengthY;

    void Start()
    {
        Vector3 worldMin = forMin.position;
        Vector3 worldMax = forMax.position;

        cellMin = tilemap.WorldToCell(worldMin);
        cellMax = tilemap.WorldToCell(worldMax);

        Vector3Int bottomLeft = Vector3Int.Min(cellMin, cellMax);
        Vector3Int topRight = Vector3Int.Max(cellMin, cellMax);
        cellMin = bottomLeft;
        cellMax = topRight;

        lengthX = cellMax.x - cellMin.x + 1;
        lengthY = cellMax.y - cellMin.y + 1;

        Debug.Log($"格子范围：左下 {cellMin}，右上 {cellMax}");
        Debug.Log($"地图大小：{lengthX} x {lengthY}");
    }

    // 将逻辑坐标映射到实际 Cell 坐标
    private Vector3Int MapToCell(int x, int y)
    {
        return new Vector3Int(cellMin.x + x, cellMin.y + y, 0);
    }

    public bool IsInBounds(int x, int y)
    {
        return x >= 0 && x < lengthX && y >= 0 && y < lengthY;
    }

    public bool Useable(int x, int y)
    {
        if (!IsInBounds(x, y)) return false;
        Vector3Int cell = MapToCell(x, y);
        return obstacle == null || obstacle.GetTile(cell) == null;
    }

    public void SetTile(int x, int y, TileBase tile)
    {
        if (!IsInBounds(x, y)) return;
        Vector3Int cell = MapToCell(x, y);
        tilemap.SetTile(cell, tile);
    }

    public TileBase GetTile(int x, int y)
    {
        if (!IsInBounds(x, y)) return null;
        Vector3Int cell = MapToCell(x, y);
        return tilemap.GetTile(cell);
    }

    public void ClearTile(int x, int y)
    {
        if (!IsInBounds(x, y)) return;
        Vector3Int cell = MapToCell(x, y);
        tilemap.SetTile(cell, null);
    }

    // ✅ 设置一整列（固定 x，遍历 y）
    public void SetColumn(int x, TileBase tile)
    {
        for (int y = 0; y < lengthY; y++)
        {
            if (Useable(x, y))
                SetTile(x, y, tile);
        }
    }

    // ✅ 设置一整行（固定 y，遍历 x）
    public void SetRow(int y, TileBase tile)
    {
        for (int x = 0; x < lengthX; x++)
        {
            if (Useable(x, y))
                SetTile(x, y, tile);
        }
    }

    // ✅ 可选：清除一整行
    public void ClearRow(int y)
    {
        for (int x = 0; x < lengthX; x++)
            ClearTile(x, y);
    }

    // ✅ 可选：清除一整列
    public void ClearColumn(int x)
    {
        for (int y = 0; y < lengthY; y++)
            ClearTile(x, y);
    }

    // 获取逻辑尺寸
    public int Width => lengthX;
    public int Height => lengthY;
}
