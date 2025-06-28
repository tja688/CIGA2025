using UnityEngine;
using UnityEngine.Tilemaps; 

public class PlayerController : MonoBehaviour
{
    public Tilemap groundTilemap; // 从 Inspector 拖入你的 Tilemap
    public float moveSpeed = 5f; // 每秒移动的单位距离

    private Vector3Int currentCellPos; // 角色当前所在的格子坐标
    private Vector3 targetWorldPos;    // 角色要移动到的世界坐标目标点
    private bool isMoving = false;     // 是否正在移动

    // 动画相关
    public Animator animator;
    private static readonly int IsMoving = Animator.StringToHash("IsMoving");
    private static readonly int MoveX = Animator.StringToHash("MoveX");
    private static readonly int MoveY = Animator.StringToHash("MoveY");

    void Start()
    {
        // 初始化角色位置到它当前所在格子的中心
        if (groundTilemap == null)
        {
            Debug.LogError("Ground Tilemap is not assigned to PlayerController!");
            enabled = false; // 禁用脚本
            return;
        }

        currentCellPos = GetCellPosition(transform.position);
        targetWorldPos = GetWorldCenterPosition(currentCellPos);
        transform.position = targetWorldPos; // 确保角色初始位置对齐格子中心
    }

    void Update()
    {
        HandleMovement();
        UpdateAnimation();
        Debug.Log("IsMoving 参数值：" + animator.GetBool("IsMoving"));
    }

    void HandleMovement()
    {
        if (isMoving)
        {
            // 如果正在移动，向目标点靠近
            transform.position = Vector3.MoveTowards(transform.position, targetWorldPos, moveSpeed * Time.deltaTime);

            // 检查是否到达目标点
            if (Vector3.Distance(transform.position, targetWorldPos) < 0.001f)
            {
                transform.position = targetWorldPos; // 精确对齐
                isMoving = false;
                animator.SetBool(IsMoving, false); // 停止移动动画
            }
        }
        else // 不在移动时，接收输入
        {
            Vector3Int moveDirection = Vector3Int.zero;

            if (Input.GetKeyDown(KeyCode.W)) // 上
            {
                moveDirection = Vector3Int.up;
                Debug.Log("wwww");
            }
            else if (Input.GetKeyDown(KeyCode.S)) // 下
            {
                moveDirection = Vector3Int.down;
            }
            else if (Input.GetKeyDown(KeyCode.A)) // 左
            {
                moveDirection = Vector3Int.left;
            }
            else if (Input.GetKeyDown(KeyCode.D)) // 右
            {
                moveDirection = Vector3Int.right;
            }

            if (moveDirection != Vector3Int.zero)
            {
                Vector3Int nextCellPos = currentCellPos + moveDirection;

                // 检查下一个格子是否在Tilemap范围内并且可通行
                if (IsCellWalkable(nextCellPos))
                {
                    currentCellPos = nextCellPos;
                    targetWorldPos = GetWorldCenterPosition(currentCellPos);
                    isMoving = true;
                    animator.SetBool(IsMoving, true); // 开始移动动画
                    animator.SetFloat(MoveX, moveDirection.x);
                    animator.SetFloat(MoveY, moveDirection.y);
                }
            }
        }
    }

    // 辅助方法 (可以从上面的 GridManager 中复制过来，或者让这个脚本引用 GridManager 的单例)
    private Vector3Int GetCellPosition(Vector3 worldPosition)
    {
        return groundTilemap.WorldToCell(worldPosition);
    }

    private Vector3 GetWorldCenterPosition(Vector3Int cellPosition)
    {
        Vector3 worldPos = groundTilemap.CellToWorld(cellPosition);
        Vector3 cellSize = groundTilemap.cellSize;
        return worldPos + new Vector3(cellSize.x / 2, cellSize.y / 2, 0);
    }

    private bool IsCellWalkable(Vector3Int cellPosition)
    {
        // 简单示例：没有Tile的格子就是可通行
        return groundTilemap.GetTile(cellPosition) != null;
    }

    void UpdateAnimation()
    {
        // 动画参数在 HandleMovement 中设置
    }
}