using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

// --- 辅助类（无变化） ---
[System.Serializable]
public class ItemDropInfo
{
    public GameObject itemPrefab;
    [Range(0f, 1f)]
    public float probability;
}

[System.Serializable]
public class StageDropPool
{
    public string stageName;
    public List<ItemDropInfo> itemPool;

    public void ValidateProbabilities()
    {
        float total = 0;
        foreach (var item in itemPool) { total += item.probability; }
        if (total > 0)
        {
            foreach (var item in itemPool) { item.probability /= total; }
        }
    }
}


// --- 主控制器（最终·完全体） ---
public class ItemDropController : MonoBehaviour
{
    [Header("阶段与道具池配置")]
    public List<StageDropPool> stagePools;
    public int currentStageIndex = 0;

    // --- 【最终修改】生成和目标区域都使用列表 ---
    [Header("区域配置")]
    [Tooltip("道具将从这些区域中的随机一点开始下落")]
    public List<BoxCollider2D> spawnZones;
    [Tooltip("道具将掉落到这些区域中的随机一点")]
    public List<BoxCollider2D> targetZones;

    [Header("掉落动画参数")]
    [Tooltip("每次掉落的间隔时间（秒）")]
    public Vector2 dropInterval = new Vector2(1f, 3f);
    [Tooltip("道具从生成到落地需要的时间范围")]
    public Vector2 dropDurationRange = new Vector2(1.2f, 1.8f);
    [Tooltip("抛物线的曲率（高度）范围")]
    public Vector2 parabolaHeightRange = new Vector2(3f, 5f);
    [Tooltip("旋转速度（每秒圈数）范围")]
    public Vector2 rotationSpeedRange = new Vector2(0.5f, 2f);
    
    void Start()
    {
        // 检查配置是否完整
        if (spawnZones == null || spawnZones.Count == 0)
        {
            Debug.LogError("错误：没有配置任何生成区域 (Spawn Zones)！请添加至少一个BoxCollider2D。", this);
            this.enabled = false; // 禁用此脚本以防报错
            return;
        }
        if (targetZones == null || targetZones.Count == 0)
        {
            Debug.LogError("错误：没有配置任何目标掉落区域 (Target Zones)！请添加至少一个BoxCollider2D。", this);
            this.enabled = false;
            return;
        }

        foreach (var pool in stagePools)
        {
            pool.ValidateProbabilities();
        }

        StartCoroutine(DropItemRoutine());
    }

    private System.Collections.IEnumerator DropItemRoutine()
    {
        while (true)
        {
            float waitTime = Random.Range(dropInterval.x, dropInterval.y);
            yield return new WaitForSeconds(waitTime);
            PerformDrop();
        }
    }

    private void PerformDrop()
    {
        if (stagePools.Count == 0 || currentStageIndex >= stagePools.Count) return;

        GameObject prefabToDrop = GetRandomItemFromCurrentPool();
        if (prefabToDrop == null) return;
        
        // --- 从多个生成区域和目标区域中获取随机点 ---
        Vector3 startPos = GetRandomPositionFromZoneList(spawnZones);
        Vector3 endPos = GetRandomPositionFromZoneList(targetZones);

        GameObject itemInstance = Instantiate(prefabToDrop, startPos, Quaternion.identity);
        AnimateDrop(itemInstance, endPos);
    }

    // --- 逻辑统一：从指定的区域列表中随机选择一个区域，并在其中获取一个随机点 ---
    private Vector3 GetRandomPositionFromZoneList(List<BoxCollider2D> zones)
    {
        // 1. 随机选择一个区域
        int randomZoneIndex = Random.Range(0, zones.Count);
        BoxCollider2D selectedZone = zones[randomZoneIndex];

        // 2. 在该区域的包围盒内随机取一个点
        Bounds zoneBounds = selectedZone.bounds;
        float randomX = Random.Range(zoneBounds.min.x, zoneBounds.max.x);
        float randomY = Random.Range(zoneBounds.min.y, zoneBounds.max.y);
        
        return new Vector3(randomX, randomY, selectedZone.transform.position.z);
    }
    
    private void AnimateDrop(GameObject item, Vector3 endPosition)
    {
        float randomDuration = Random.Range(dropDurationRange.x, dropDurationRange.y);
        
        Sequence dropSequence = DOTween.Sequence();

        // 路径动画
        Vector3 startPosition = item.transform.position;
        float parabolaHeight = Random.Range(parabolaHeightRange.x, parabolaHeightRange.y);
        Vector3 controlPoint = (startPosition + endPosition) / 2f;
        controlPoint.y += parabolaHeight;
        Vector3[] path = new Vector3[] { controlPoint, endPosition };
        
        dropSequence.Append(
            item.transform.DOPath(path, randomDuration, PathType.CatmullRom)
                  .SetEase(Ease.InQuad)
        );

        // 旋转动画
        float rotationSpeed = Random.Range(rotationSpeedRange.x, rotationSpeedRange.y);
        float totalRotation = 360 * rotationSpeed * randomDuration;
        
        dropSequence.Join(
            item.transform.DORotate(new Vector3(0, 0, totalRotation), randomDuration, RotateMode.FastBeyond360)
                  .SetEase(Ease.Linear)
        );

        // 动画结束回调
        dropSequence.OnComplete(() =>
        {
            item.transform.rotation = Quaternion.identity;
            var rb = item.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                // rb.angularVelocity = 0f;
            }
        });
    }
    
    // 以下方法无变化
    private GameObject GetRandomItemFromCurrentPool()
    {
        StageDropPool currentPool = stagePools[currentStageIndex];
        if (currentPool.itemPool.Count == 0) return null;
        float randomValue = Random.value;
        float cumulativeProbability = 0f;
        foreach (var itemInfo in currentPool.itemPool)
        {
            cumulativeProbability += itemInfo.probability;
            if (randomValue <= cumulativeProbability)
            {
                return itemInfo.itemPrefab;
            }
        }
        return currentPool.itemPool[currentPool.itemPool.Count - 1].itemPrefab;
    }
    
    public void SetStage(int newStageIndex)
    {
        if (newStageIndex >= 0 && newStageIndex < stagePools.Count)
        {
            currentStageIndex = newStageIndex;
            Debug.Log($"游戏阶段已切换到: {stagePools[currentStageIndex].stageName}");
        }
        else
        {
            Debug.LogError("无效的阶段索引！");
        }
    }
}