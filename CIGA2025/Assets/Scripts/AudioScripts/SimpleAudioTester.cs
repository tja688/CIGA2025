using UnityEngine;

public class IntuitiveAudioTester : MonoBehaviour
{
    public AudioConfigSO testConfig;
    
    // 始终追踪最后一个请求播放的音轨ID
    private int _lastPlayedTrackId = -1; 
    
    void Update()
    {
        if (testConfig == null) return;

        // 【播放】
        // 按 1: 播放单次音轨，并记录ID以便控制
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            StopLastTrack(); // 停止上一个，避免混乱
            Debug.Log("【1】播放单次音轨...");
            _lastPlayedTrackId = AudioManager.Instance.Play(testConfig);
        }

        // 按 2: 循环播放，并记录ID
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            StopLastTrack();
            Debug.Log("【2】开始循环播放...");
            _lastPlayedTrackId = AudioManager.Instance.Play(testConfig, new PlaybackSettings { isLooping = true });
        }
        
        // 按 6: 播放2倍音量音轨，并记录ID
        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            StopLastTrack();
            Debug.Log("【6】播放2倍音量音轨...");
            _lastPlayedTrackId = AudioManager.Instance.Play(testConfig, new PlaybackSettings { volumeMultiplier = 2.0f });
        }

        // 按 7: 播放带淡入淡出的“一次性”音效 (通常无需控制，不记录ID)
        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            Debug.Log("【7】播放“一次性”淡入淡出音效...");
            var settings = new PlaybackSettings { fadeInDuration = 0.5f };
            AudioManager.Instance.Play(testConfig, settings, 0.5f);
        }


        // 【控制】(始终控制最后一个播放的音轨)
        // 按 3: 暂停
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            if (_lastPlayedTrackId != -1)
            {
                Debug.Log("【3】暂停...");
                AudioManager.Instance.Pause(_lastPlayedTrackId, 0.25f);
            }
        }

        // 按 4: 恢复
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            if (_lastPlayedTrackId != -1)
            {
                Debug.Log("【4】恢复...");
                AudioManager.Instance.Resume(_lastPlayedTrackId, 0.25f);
            }
        }

        // 按 5: 停止
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            Debug.Log("【5】停止...");
            StopLastTrack(0.5f);
        }
    }
    
    private void StopLastTrack(float fade = 0f)
    {
        if (_lastPlayedTrackId != -1)
        {
            AudioManager.Instance.Stop(_lastPlayedTrackId, fade);
            _lastPlayedTrackId = -1; // 停止后重置ID
        }
    }
    
    private void OnGUI()
    {
        GUI.Box(new Rect(150, 150, 300, 215), "高级音频测试");
        GUI.Label(new Rect(200, 230, 290, 20), "【1】播放可控的单次音轨");
        GUI.Label(new Rect(200, 250, 290, 20), "【2】播放可控的循环音轨");
        GUI.Label(new Rect(200, 270, 290, 20), "【3】暂停当前可控音轨(淡出)");
        GUI.Label(new Rect(200, 290, 290, 20), "【4】恢复当前可控音轨(淡入)");
        GUI.Label(new Rect(200, 310, 290, 20), "【5】停止当前可控音轨(淡出)");
        GUI.Label(new Rect(200, 340, 290, 20), "【6】播放一次性音效(2倍音量)");
        GUI.Label(new Rect(200, 360, 290, 20), "【7】播放一次性音效(淡入/淡出)");
        
        if (_lastPlayedTrackId != -1)
        {
            GUI.color = Color.green;
            GUI.Label(new Rect(200, 390, 290, 20), $"正在控制音轨 ID: {_lastPlayedTrackId}");
        }
        else
        {
            GUI.color = Color.yellow;
            GUI.Label(new Rect(200, 390, 290, 20), "无正在控制的音轨");
        }
    }
}