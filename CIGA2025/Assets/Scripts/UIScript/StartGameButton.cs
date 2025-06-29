using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 一个一次性的开始游戏按钮。
/// 第一次点击时，通知GameFlowManager开始开场演出。
/// 之后按钮的功能会改变（例如变成“继续游戏”），不再触发初始流程。
/// </summary>
[RequireComponent(typeof(Button))]
public class StartGameButton : MonoBehaviour
{
    private Button _button;
    private bool _hasBeenClicked = false;

    void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnButtonClicked);
    }

    private void OnButtonClicked()
    {
        // 如果已经点击过，则不执行任何操作
        if (_hasBeenClicked)
        {
            Debug.Log("[StartGameButton] 按钮已被点击过，不再触发游戏流程。");
            return;
        }

        // 标记为已点击
        _hasBeenClicked = true;

        // 检查 GameFlowManager 是否存在
        if (GameFlowManager.Instance != null)
        {
            Debug.Log("[StartGameButton] 开始游戏按钮被点击，请求进入开场演出状态。");
            // 【核心】通知游戏流程管理器，进入开场演出状态
            GameFlowManager.Instance.UpdateGameState(GameFlowManager.GameState.OpeningCutscene);
        }
        else
        {
            Debug.LogError("[StartGameButton] 无法找到 GameFlowManager 的实例！");
        }

        // 在这里你可以改变按钮的文本，例如：
        // var buttonText = GetComponentInChildren<TMPro.TextMeshProUGUI>();
        // if(buttonText != null) buttonText.text = "继续游戏";
    }

    private void OnDestroy()
    {
        // 移除监听，好习惯
        if(_button != null)
        {
            _button.onClick.RemoveListener(OnButtonClicked);
        }
    }
}