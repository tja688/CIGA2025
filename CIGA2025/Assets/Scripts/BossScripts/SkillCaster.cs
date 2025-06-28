// SkillCaster.cs
using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using System.Collections.Generic;

/// <summary>
/// 独立处理所有技能的预警、释放和效果。
/// BossAI脚本会调用此类中的方法来执行攻击。
/// 此版本已包含所有技能预制件的实例化逻辑。
/// </summary>
public class SkillCaster : MonoBehaviour
{
    [Header("技能资源 Prefabs")]
    [Tooltip("用于轨道攻击的线条状预警特效")]
    public GameObject warningLinePrefab;
    [Tooltip("用于区域攻击的方块状预警特效")]
    public GameObject warningAreaPrefab;
    [Tooltip("第一阶段：寒冰射线")]
    public GameObject frostBeamPrefab;
    [Tooltip("第一阶段：暴风雪冰柱")]
    public GameObject blizzardPillarPrefab;
    [Tooltip("第二阶段：烈焰攻击")]
    public GameObject flamePrefab;
    [Tooltip("第二阶段：火焰冲击波")]
    public GameObject flameWavePrefab;
    [Tooltip("第二阶段：地面尖刺")]
    public GameObject groundSpikePrefab;

    // 假设轨道Y轴位置在0.1f以避免穿地
    private Vector3[] _lanePositions = new Vector3[5]
    {
        new Vector3(-4, 0.1f, 0), // 轨道1
        new Vector3(-2, 0.1f, 0), // 轨道2
        new Vector3(0, 0.1f, 0),  // 轨道3
        new Vector3(2, 0.1f, 0),  // 轨道4
        new Vector3(4, 0.1f, 0)   // 轨道5
    };

    #region 辅助方法

    /// <summary>
    /// 在指定的多个轨道上生成特效，并在一定时间后销毁它们。
    /// </summary>
    private void SpawnEffectsOnLanes(GameObject prefab, int[] laneIndices, float lifeTime)
    {
        if (prefab == null) return; // 如果预制件为空，则不执行任何操作

        foreach (int index in laneIndices)
        {
            if (index >= 0 && index < _lanePositions.Length)
            {
                GameObject instance = Instantiate(prefab, _lanePositions[index], prefab.transform.rotation);
                Destroy(instance, lifeTime);
            }
        }
    }

    #endregion

    #region 阶段一技能：冰霜

    public async UniTask CastFrostBeam(CancellationToken cancellationToken)
    {
        Debug.Log("技能: 寒冰射线 - 开始");
        // 预警：轨道1、3、5
        SpawnEffectsOnLanes(warningLinePrefab, new int[] { 0, 2, 4 }, 1f);
        await UniTask.Delay(TimeSpan.FromSeconds(1), ignoreTimeScale: false, cancellationToken: cancellationToken);

        // 执行
        SpawnEffectsOnLanes(frostBeamPrefab, new int[] { 0, 2, 4 }, 3f);
        await UniTask.Delay(TimeSpan.FromSeconds(3f), ignoreTimeScale: false, cancellationToken: cancellationToken); // 持续时间

        Debug.Log("技能: 寒冰射线 - 结束");
    }

    public async UniTask CastBlizzard(CancellationToken cancellationToken)
    {
        Debug.Log("技能: 暴风雪 - 开始");
        // 预警：轨道2、3、4
        SpawnEffectsOnLanes(warningLinePrefab, new int[] { 1, 2, 3 }, 1f);
        await UniTask.Delay(TimeSpan.FromSeconds(1), ignoreTimeScale: false, cancellationToken: cancellationToken);

        // 执行
        SpawnEffectsOnLanes(blizzardPillarPrefab, new int[] { 1, 2, 3 }, 2f);
        await UniTask.Delay(TimeSpan.FromSeconds(2f), ignoreTimeScale: false, cancellationToken: cancellationToken); // 持续时间

        Debug.Log("技能: 暴风雪 - 结束");
    }

    #endregion

    #region 阶段二技能：烈火

    public async UniTask CastAlternatingFlames(CancellationToken cancellationToken)
    {
        Debug.Log("技能: 交替烈焰 - 开始");
        // 轮次1：轨道1、3、5
        SpawnEffectsOnLanes(flamePrefab, new int[] { 0, 2, 4 }, 1.5f);
        await UniTask.Delay(TimeSpan.FromSeconds(1f), ignoreTimeScale: false, cancellationToken: cancellationToken); // 等待1秒间隔

        // 轮次2：轨道2、4
        SpawnEffectsOnLanes(flamePrefab, new int[] { 1, 3 }, 1.5f);
        await UniTask.Delay(TimeSpan.FromSeconds(1.5f), ignoreTimeScale: false, cancellationToken: cancellationToken); // 等待技能动画播放完毕

        Debug.Log("技能: 交替烈焰 - 结束");
    }

    public async UniTask CastFlameWave(CancellationToken cancellationToken)
    {
        Debug.Log("技能: 火焰冲击波 - 开始");
        // 预警并从左到右缓慢执行
        for (int i = 0; i < _lanePositions.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested(); // 每次循环前检查是否需要取消

            // 在当前轨道生成预警
            SpawnEffectsOnLanes(warningAreaPrefab, new int[] { i }, 0.5f);
            await UniTask.Delay(TimeSpan.FromSeconds(0.2f), ignoreTimeScale: false, cancellationToken: cancellationToken);

            // 在当前轨道生成火焰
            SpawnEffectsOnLanes(flameWavePrefab, new int[] { i }, 2f);
            await UniTask.Delay(TimeSpan.FromSeconds(0.6f), ignoreTimeScale: false, cancellationToken: cancellationToken); // 火焰扫过的间隔
        }
        Debug.Log("技能: 火焰冲击波 - 结束");
    }

     public async UniTask CastGroundSpikes(CancellationToken cancellationToken)
    {
        Debug.Log("技能: 地面尖刺 - 开始");
        // 轮次1：轨道1、2、3
        SpawnEffectsOnLanes(groundSpikePrefab, new int[] { 0, 1, 2 }, 1.8f);
        await UniTask.Delay(TimeSpan.FromSeconds(2f), ignoreTimeScale: false, cancellationToken: cancellationToken);

        // 轮次2：轨道4、5
        SpawnEffectsOnLanes(groundSpikePrefab, new int[] { 3, 4 }, 1.8f);
        await UniTask.Delay(TimeSpan.FromSeconds(2f), ignoreTimeScale: false, cancellationToken: cancellationToken);

        Debug.Log("技能: 地面尖刺 - 结束");
    }

    #endregion

    #region 阶段四技能：终焉

    public async UniTask CastUltimateAttack(Action onPlayerFail, Action onPlayerSucceed)
    {
        Debug.LogWarning("终焉模式：开始蓄力终极攻击！");
        // 这里通常是播放全屏特效、UI警告、Boss蓄力动画等演出
        
        float chargeTime = 5f;
        float timer = 0f;
        bool playerSucceeded = false;
        
        // 在这里实现检测玩家是否成功规避的逻辑
        // 例如，在5秒内检测玩家是否进入了某个特定的安全区域触发器
        Debug.Log("玩家有5秒时间来寻找特殊方式规避此攻击...");
        
        while(timer < chargeTime)
        {
            // if (PlayerIsInSafeZone()) {
            //     playerSucceeded = true;
            //     break;
            // }
            timer += Time.deltaTime;
            await UniTask.Yield(); // 等待下一帧
        }

        if (playerSucceeded)
        {
            Debug.Log("玩家成功规避！Boss攻击失败，进入虚弱状态！");
            onPlayerSucceed?.Invoke();
        }
        else
        {
            Debug.LogError("玩家规避失败！受到致命伤害！");
            // 在这里可以触发一个全屏的爆炸特效
            onPlayerFail?.Invoke();
        }
    }
    
    #endregion
}