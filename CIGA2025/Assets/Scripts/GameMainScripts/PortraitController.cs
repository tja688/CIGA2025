using UnityEngine;
using UnityEngine.UI; // 如果你用的是UI.Image
using System.Collections.Generic;

/// <summary>
/// 管理游戏中角色立绘的显示和隐藏。
/// </summary>
public class PortraitController : MonoBehaviour
{
    public static PortraitController Instance { get; private set; }

    [Tooltip("将你的立绘游戏对象（例如UI Image）拖到这里")]
    [SerializeField] private List<GameObject> portraits;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        // 游戏开始时，默认隐藏所有立绘
        HideAllPortraits();
    }

    /// <summary>
    /// 显示指定索引的立绘。
    /// </summary>
    /// <param name="index">立绘在列表中的索引</param>
    public void ShowPortrait(int index)
    {
        if (index < 0 || index >= portraits.Count)
        {
            Debug.LogError($"[PortraitController] 无效的立绘索引: {index}");
            return;
        }

        // 为了确保只有一个立绘显示，可以先隐藏所有其他的
        // HideAllPortraits();

        portraits[index].SetActive(true);
        Debug.Log($"[PortraitController] 显示立绘: {portraits[index].name}");
    }

    /// <summary>
    /// 隐藏所有立绘。
    /// </summary>
    public void HideAllPortraits()
    {
        foreach (var portrait in portraits)
        {
            if(portrait != null)
            {
                portrait.SetActive(false);
            }
        }
    }
}