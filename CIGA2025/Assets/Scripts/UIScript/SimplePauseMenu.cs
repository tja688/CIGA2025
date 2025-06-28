using UnityEngine;
using TMPro; // 使用 TextMeshPro 需要引用此命名空间

/// <summary>
/// 挂载在主Canvas上，用于管理游戏暂停逻辑和UI交互。
/// </summary>
public class SimplePauseMenu : MonoBehaviour
{
    [Header("UI 组件引用")]
    [Tooltip("关联“继续游戏”或类似功能的按钮文本，将在游戏进行时显示特定内容。")]
    public TextMeshProUGUI continueButtonText;
    
    private bool isGamePausedByThisScript = false;

    private bool _isGameStoped = false;

    void FixedUpdate()
    {
        UpdateContinueButtonText();
    }

    /// <summary>
    /// 根据UI管理器的状态来处理游戏的暂停和恢复。
    /// </summary>
    public void StopGame()
    {
        _isGameStoped = true;
        
        Time.timeScale = 0;
    }
    
    public void ContinueGame()
    {
        _isGameStoped = false;
        
        Time.timeScale = 1;
    }

    /// <summary>
    /// 根据GameFlowManager的状态更新UI文本。
    /// </summary>
    private void UpdateContinueButtonText()
    {
        // 确保文本对象已在Inspector中关联
        if (continueButtonText == null) return;
        
        // 确保GameFlowManager的单例已准备好
        if (GameFlowManager.Instance == null) return;

        // 3. 当IsGaming为true时变更其文字内容为（继续游戏）
        if (GameFlowManager.Instance.IsGaming)
        {
            continueButtonText.text = "继续游戏";
        }

    }
}