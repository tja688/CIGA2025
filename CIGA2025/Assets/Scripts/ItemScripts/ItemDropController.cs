using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

// --- 辅助类（这部分保持不变）---
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


// --- 主控制器（已修改） ---
public class ItemDropController : MonoBehaviour
{
    [Header("阶段与道具池配置")]
    public List<StageDropPool> stagePools;
    public int currentStageIndex = 0;

    [Header("生成区域配置 (屏幕上方)")]
    public Transform spawnAreaStart;
    public Transform spawnAreaEnd;

    // --- 【核心修改】 ---
    // 不再使用两个点，而是使用一个BoxCollider2D列表来定义所有可掉落的矩形区域
    [Header("目标掉落区域配置 (异形场地)")]
    [Tooltip("将定义了可掉落区域的BoxCollider2D拖到这里")]
    public List<BoxCollider2D> targetZones;

    [Header("掉落动画参数")]
    public Vector2 dropInterval = new Vector2(1f, 3f);
    public float dropDuration = 1.5f;
    public Vector2 parabolaHeightRange = new Vector2(3f, 5f);
    public Vector2 rotationSpeedRange = new Vector2(0.5f, 2f);
    
    void Start()
    {
        foreach (var pool in stagePools)
        {
            pool.ValidateProbabilities();
        }

        if (targetZones == null || targetZones.Count == 0)
        {
            Debug.LogError("错误：没有配置任何目标掉落区域 (Target Zones)！请添加至少一个BoxCollider2D。");
            return; // 如果没有配置区域，则停止运行以防报错
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

        // 起点逻辑不变
        Vector3 startPos = GetRandomPositionInRect(spawnAreaStart.position, spawnAreaEnd.position);
        
        // --- 【核心修改】 ---
        // 终点逻辑改为从多个区域中随机选择
        Vector3 endPos = GetRandomPositionInZones();

        GameObject itemInstance = Instantiate(prefabToDrop, startPos, Quaternion.identity);
        AnimateDrop(itemInstance, endPos);
    }

    // --- 【新增方法】 ---
    // 在配置的所有区域中随机挑选一个点
    private Vector3 GetRandomPositionInZones()
    {
        // 1. 随机选择一个区域
        int randomZoneIndex = Random.Range(0, targetZones.Count);
        BoxCollider2D selectedZone = targetZones[randomZoneIndex];

        // 2. 在该区域的包围盒内随机取一个点
        Bounds zoneBounds = selectedZone.bounds;
        float randomX = Random.Range(zoneBounds.min.x, zoneBounds.max.x);
        float randomY = Random.Range(zoneBounds.min.y, zoneBounds.max.y);

        // 2D游戏的Z轴通常是固定的，可以取第一个区域的Z值作为参考
        return new Vector3(randomX, randomY, selectedZone.transform.position.z);
    }

    // 将原有的GetRandomPosition重命名，以明确其功能
    private Vector3 GetRandomPositionInRect(Vector3 start, Vector3 end)
    {
        return new Vector3(
            Random.Range(start.x, end.x),
            Random.Range(start.y, end.y),
            start.z
        );
    }
    
    // 以下方法保持不变
    private GameObject GetRandomItemFromCurrentPool()
    {
        // ... (代码同前)
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

    private void AnimateDrop(GameObject item, Vector3 endPosition)
    {
        // ... (代码同前)
        Sequence dropSequence = DOTween.Sequence();
        Vector3 startPosition = item.transform.position;
        float parabolaHeight = Random.Range(parabolaHeightRange.x, parabolaHeightRange.y);
        Vector3 controlPoint = (startPosition + endPosition) / 2f;
        controlPoint.y += parabolaHeight;
        Vector3[] path = new Vector3[] { controlPoint, endPosition };
        
        dropSequence.Append(item.transform.DOPath(path, dropDuration, PathType.CatmullRom).SetEase(Ease.InQuad));

        float rotationSpeed = Random.Range(rotationSpeedRange.x, rotationSpeedRange.y);
        float totalRotation = 360 * rotationSpeed * dropDuration;
        dropSequence.Join(item.transform.DORotate(new Vector3(0, 0, totalRotation), dropDuration, RotateMode.FastBeyond360).SetEase(Ease.Linear));

        dropSequence.OnComplete(() =>
        {
            item.transform.rotation = Quaternion.identity;
            Debug.Log($"{item.name} 已掉落到指定地点！");
            var rb = item.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
        });
    }

    public void SetStage(int newStageIndex)
    {
        // ... (代码同前)
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