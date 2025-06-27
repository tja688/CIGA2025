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
        // 如果不是拍照模式，则直接取消选择并隐藏框选框
        if (!PhotoModeManager.Instance || !PhotoModeManager.Instance.IsPhotoMode)
        {
            if (_currentSelectedObject != null)
            {
                _currentSelectedObject = null;
                _selectionBoxInstance.SetVisible(false);
            }
            return;
        }

        // --- 核心逻辑重构 ---

        // 第1步：每帧都检测鼠标下方是否有可选择的对象
        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        ISelectable objectUnderMouse = FindClosestSelectable(mouseWorldPos);

        // 第2步：将检测到的对象直接赋值为当前选择的对象
        // 注意：这里的 _currentSelectedObject 现在代表“鼠标正悬停的对象”
        _currentSelectedObject = objectUnderMouse;

        // 第3步：根据当前是否选中对象，来决定框选框的状态
        // 这段逻辑现在每帧都会执行
        if (_currentSelectedObject != null)
        {
            // 如果有对象被选中：
            // a. 持续更新框选框的位置，使其平滑跟随对象
            _selectionBoxInstance.transform.position = _currentSelectedObject.SelectionBounds.center;
        
            // b. 持续更新框选框的大小（以防对象大小也发生变化）
            _selectionBoxInstance.UpdateBounds(_currentSelectedObject.SelectionBounds);

            // c. 确保框选框是可见的
            _selectionBoxInstance.SetVisible(true);
        }
        else
        {
            // 如果没有对象被选中，则确保框选框是不可见的
            _selectionBoxInstance.SetVisible(false);
        }
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
    

    #endregion
}