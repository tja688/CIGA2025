using UnityEngine;

/// <summary>
/// 负责处理游戏中“可选择对象”的框选逻辑。
/// 它会根据鼠标位置，在指定的层上查找实现了 ISelectable 接口的对象。
/// 此管理器的核心逻辑仅在 PhotoModeManager 的拍照模式下激活。
/// </summary>
public class SelectionManager : MonoBehaviour
{
    #region 单例模式 (Singleton)
    public static SelectionManager Instance { get; private set; }
    #endregion

    #region Inspector 设置
    [Header("核心配置")]
    [Tooltip("带控制器脚本的框选框预制体")]
    [SerializeField] private SelectionBoxController selectionBoxPrefab;
    [Tooltip("鼠标检测范围")]
    [SerializeField] private float selectionRadius = 1f;
    #endregion


    private SelectionBoxController _selectionBoxInstance;
    private ISelectable _currentSelectedObject;


    #region Unity 生命周期
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (selectionBoxPrefab != null)
        {
            _selectionBoxInstance = Instantiate(selectionBoxPrefab);
            _selectionBoxInstance.name = "SelectionBox_Instance";
            _selectionBoxInstance.SetVisible(false);
        }
        else
        {
            Debug.LogError("SelectionManager: 未配置 selectionBoxPrefab！框选功能将无法工作。");
        }
    }

    private void Update()
    {
        if (!PhotoModeManager.Instance || !PhotoModeManager.Instance.IsPhotoMode)
        {
            if (_currentSelectedObject != null)
            {
                _currentSelectedObject = null;
                _selectionBoxInstance.SetVisible(false);
            }
            return;
        }


        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        ISelectable closestObject = FindClosestSelectable(mouseWorldPos);
        
        UpdateSelection(closestObject);
    }
    #endregion

    
    #region 私有辅助方法
    /// <summary>
    /// 在指定位置附近查找最近的、实现了 ISelectable 接口的对象。
    /// </summary>
    /// <param name="searchPosition">搜索的中心点</param>
    /// <returns>找到的 ISelectable 对象，如果没找到则返回 null</returns>
    private ISelectable FindClosestSelectable(Vector2 searchPosition)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(searchPosition, selectionRadius);
        ISelectable closest = null;
        float minDistance = float.MaxValue;

        foreach (var hit in hits)
        {
            ISelectable selectable = hit.GetComponent<ISelectable>();

            if (selectable != null)
            {
                float distance = Vector2.Distance(searchPosition, hit.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closest = selectable;
                }
            }
        }
        return closest;
    }

    /// <summary>
    /// 更新当前的选择对象，并相应地更新视觉表现（框选框）。
    /// </summary>
    /// <param name="newSelection">新的选择对象，可以为 null</param>
    private void UpdateSelection(ISelectable newSelection)
    {
        if (_currentSelectedObject != newSelection)
        {
            _currentSelectedObject = newSelection;

            if (_currentSelectedObject != null)
            {
                _selectionBoxInstance.UpdateBounds(_currentSelectedObject.SelectionBounds);
                _selectionBoxInstance.SetVisible(true);
            }
            else
            {
                _selectionBoxInstance.SetVisible(false);
            }
        }
    }
    #endregion
}