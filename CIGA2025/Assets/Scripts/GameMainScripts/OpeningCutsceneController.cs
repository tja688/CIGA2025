using UnityEngine;
using Cysharp.Threading.Tasks;
using System;

/// <summary>
/// 控制开场演出的总控制器。
/// 监听游戏状态，在 OpeningCutscene 状态时启动演出流程。
/// </summary>
public class OpeningCutsceneController : MonoBehaviour
{
    [Header("场景引用和配置")]
    [Tooltip("摄像机移动到的第一个点位")]
    [SerializeField] private Transform cameraStartPoint;
    [Tooltip("摄像机移动到的Boss点位")]
    [SerializeField] private Transform cameraBossPoint;
    [Tooltip("玩家对象，用于聚焦")]
    [SerializeField] private Transform playerTransform;

    [Header("运镜参数")]
    [Tooltip("镜头移动的默认速度")]
    [SerializeField] private float moveDuration = 2.0f;
    [Tooltip("镜头拉近后的尺寸")]
    [SerializeField] private float zoomedInSize = 3.5f;
    [Tooltip("摄像机左看右看时，相比中心点的偏移量")]
    [SerializeField] private Vector3 lookAroundOffset = new Vector3(2, 0, 0);

    [Header("对话内容")]
    [TextArea(3, 5)]
    [SerializeField] private string[] dialogue1_Player;
    [TextArea(3, 5)]
    [SerializeField] private string[] dialogue2_Player;
    [TextArea(3, 5)]
    [SerializeField] private string[] dialogue3_Boss;

    [Header("立绘索引")]
    [SerializeField] private int playerPortraitIndex = 0;
    [SerializeField] private int bossPortraitIndex = 1;
    [SerializeField] private int cameraPortraitIndex = 2;
    
    private bool _hasPlayed = false;

    private void OnEnable()
    {
        GameFlowManager.OnGameStateChanged += HandleGameStateChanged;
    }

    private void OnDisable()
    {
        GameFlowManager.OnGameStateChanged -= HandleGameStateChanged;
    }

    private void HandleGameStateChanged(GameFlowManager.GameState newState)
    {
        if (newState == GameFlowManager.GameState.OpeningCutscene && !_hasPlayed)
        {
            _hasPlayed = true;
            PlayCutsceneAsync().Forget();
        }
    }

    private async UniTask PlayCutsceneAsync()
    {
        Debug.Log("[Cutscene] 开场演出开始！");

        var cameraManager = CameraManager.Instance;
        var dialogueManager = DialogueManager.Instance;
        var portraitController = PortraitController.Instance;
        
        // 【第1步】摄像机移动到开局点位并放大
        Debug.Log("[Cutscene] 1. 摄像机移动并放大...");
        // PanAsync 是我猜的你可能有的方法，如果你的CameraManager里叫别的名字，请替换
        // 从 CameraManager.cs 中我们得知有 PanAsync 和 ZoomAsync 方法，可以直接使用
        await cameraManager.PanAsync(cameraStartPoint.position, moveDuration, this.GetCancellationTokenOnDestroy());
        await cameraManager.ZoomAsync(zoomedInSize, moveDuration / 2, this.GetCancellationTokenOnDestroy());

        // 【第2步】玩家入画，显示立绘，播放对话1
        Debug.Log("[Cutscene] 2. 玩家对话1...");
        portraitController.ShowPortrait(playerPortraitIndex);
        portraitController.ShowPortrait(cameraPortraitIndex);
        await dialogueManager.ShowDialogue(dialogue1_Player); //
        portraitController.HideAllPortraits();

        // 【第3步】摄像机左看右看
        Debug.Log("[Cutscene] 3. 摄像机左看右看...");
        Vector3 centerPos = cameraManager.transform.position; // 应该是_basePosition，但我们直接获取当前位置
        await cameraManager.PanAsync(centerPos - lookAroundOffset, 0.8f, this.GetCancellationTokenOnDestroy());
        await UniTask.Delay(TimeSpan.FromSeconds(0.2f));
        await cameraManager.PanAsync(centerPos + lookAroundOffset, 1.2f, this.GetCancellationTokenOnDestroy());
        await UniTask.Delay(TimeSpan.FromSeconds(0.2f));

        // 【第4步】回到玩家聚焦机位，播放对话2
        Debug.Log("[Cutscene] 4. 摄像机聚焦玩家，对话2...");
        await cameraManager.FocusOnTargetAsync(playerTransform, zoomedInSize, moveDuration); //
        portraitController.ShowPortrait(cameraPortraitIndex);
        portraitController.ShowPortrait(playerPortraitIndex);
        await dialogueManager.ShowDialogue(dialogue2_Player); //
        portraitController.HideAllPortraits();

        // 【第5步】运镜到Boss处，播放对话3
        Debug.Log("[Cutscene] 5. 摄像机聚焦Boss，对话3...");
        await cameraManager.PanAsync(cameraBossPoint.position, moveDuration, this.GetCancellationTokenOnDestroy());
        portraitController.ShowPortrait(bossPortraitIndex);
        portraitController.ShowPortrait(playerPortraitIndex);
        await dialogueManager.ShowDialogue(dialogue3_Boss); //
        portraitController.HideAllPortraits();

        // 【第6步】摄像机复位,出现立绘，准备开战
        Debug.Log("[Cutscene] 6. 摄像机复位...");
        await cameraManager.ResetCameraAsync(); // 你需要在CameraManager里添加这个方法
        portraitController.ShowPortrait(playerPortraitIndex);
        
        // 【最后一步】通知游戏管理器，演出结束，开始战斗！
        Debug.Log("[Cutscene] 开场演出结束！进入战斗状态！");
        GameFlowManager.Instance.UpdateGameState(GameFlowManager.GameState.Gameplay);
    }
}