// DestructionAudioPlayer.cs
using UnityEngine;

/// <summary>
/// 挂载到任何游戏物体上，当该物体被销毁时，会自动从列表中随机播放一个音效。
/// 需要场景中存在一个有效的 AudioManager 实例。
/// </summary>
public class DestructionAudioPlayer : MonoBehaviour
{
    [Header("音效设置")]
    [Tooltip("将多个“破坏音效”的 AudioConfigSO 拖拽到这里。物体销毁时会从中随机选一个播放。")]
    public AudioConfigSO[] destructionSounds;

    private bool isQuitting = false;

    private void OnApplicationQuit()
    {
        // 当游戏退出时，OnDestroy 也可能被调用。
        // 设置一个标志位，防止在退出过程中因 AudioManager 已被销毁而产生不必要的报错。
        isQuitting = true;
    }

    /// <summary>
    /// Unity 的生命周期方法，当该组件附加的游戏对象被销毁时自动调用。
    /// </summary>
    private void OnDestroy()
    {
        // 如果正在退出游戏，或者 AudioManager 实例不存在，则不执行任何操作。
        if (isQuitting || AudioManager.Instance == null)
        {
            return;
        }

        // 检查音效列表是否有效且不为空
        if (destructionSounds != null && destructionSounds.Length > 0)
        {
            // 从列表中随机选择一个音效配置
            int randomIndex = Random.Range(0, destructionSounds.Length);
            AudioConfigSO soundToPlay = destructionSounds[randomIndex];

            // 确保选中的音效配置不为 null
            if (soundToPlay != null)
            {
                // 使用 AudioManager 播放这个一次性的音效。
                // Play 方法返回一个 trackId，但对于一次性音效我们通常不需要关心它。
                AudioManager.Instance.Play(soundToPlay);

                // 如果需要，可以在这里打印调试日志
                // Debug.Log($"物体 {gameObject.name} 被销毁，正在播放音效: {soundToPlay.name}");
            }
        }
    }
}