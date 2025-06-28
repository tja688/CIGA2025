using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class EffectController : MonoBehaviour
{
    [Header("效果设置")]
    [Tooltip("用于实现负片效果的材质")]
    [SerializeField] private Material _invertMaterial;

    private SpriteRenderer _spriteRenderer;
    private Material _originalMaterial;
    private Coroutine _invertCoroutine;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _originalMaterial = _spriteRenderer.material;
    }

    /// <summary>
    /// 对外公开的方法，用于触发负片效果。
    /// </summary>
    /// <param name="duration">效果持续时间（秒）。</param>
    public void TriggerInvertEffect(float duration)
    {
        // 如果当前有正在进行的协程，先停止它，避免效果重叠出错
        if (_invertCoroutine != null)
        {
            StopCoroutine(_invertCoroutine);
        }
        
        // 开启新的协程来处理限时效果
        _invertCoroutine = StartCoroutine(InvertEffectRoutine(duration));
    }

    private IEnumerator InvertEffectRoutine(float duration)
    {
        // 如果没有设置负片材质，则直接退出
        if (_invertMaterial == null)
        {
            Debug.LogError("请在 Inspector 面板中指定 Invert Material！");
            yield break;
        }

        // 步骤 1: 将材质切换为负片材质，开启效果
        _spriteRenderer.material = _invertMaterial;

        // 步骤 2: 等待指定的持续时间
        yield return new WaitForSeconds(duration);

        // 步骤 3: 将材质切换回原始材质，关闭效果
        _spriteRenderer.material = _originalMaterial;

        // 重置协程引用
        _invertCoroutine = null;
    }

    // (可选) 在对象被销毁时，确保材质恢复正常，防止在编辑器中出现材质泄漏
    private void OnDestroy()
    {
        if (_spriteRenderer != null && _originalMaterial != null)
        {
            _spriteRenderer.material = _originalMaterial;
        }
    }
}