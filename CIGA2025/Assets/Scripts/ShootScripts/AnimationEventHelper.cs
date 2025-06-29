using UnityEngine;

/// <summary>
/// 一个简单的帮助脚本，提供可以被动画事件调用的公共方法。
/// </summary>
public class AnimationEventHelper : MonoBehaviour
{

    /// <summary>
    /// 这个方法将被动画事件调用，用于隐藏自身所在的GameObject。
    /// </summary>
    public void HideGameObject()
    {
        Destroy(gameObject);

    }
    
    
}