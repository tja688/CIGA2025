using UnityEngine;

public class SimplePauseMenu : MonoBehaviour
{
    [Header("要控制的UI面板")]
    [Tooltip("将你的暂停菜单面板（Panel）拖到这里")]
    [SerializeField] private GameObject pauseMenuPanel;

    private bool isPaused = false;

    void Start()
    {
        pauseMenuPanel.SetActive(false);
        Time.timeScale = 1f;
    }
    
    /// <summary>
    /// 暂停游戏。可以被一个“设置”或“暂停”按钮调用。
    /// </summary>
    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f; // 暂停游戏时间
        pauseMenuPanel.SetActive(true); // 显示暂停菜单
    }

    /// <summary>
    /// 继续游戏。需要绑定到暂停菜单中的“继续”按钮上。
    /// </summary>
    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f; // 恢复游戏时间
        pauseMenuPanel.SetActive(false); // 隐藏暂停菜单
    }
}