using UnityEngine;

public class MenuScript : MonoBehaviour
{
    public Transform where_game;   // 目标位置
    public GameObject camera;      // 摄像机对象
    private bool off = true;
    public void StartGame()
    {
        Vector3 newPos = where_game.position;
        newPos.z = camera.transform.position.z; 
        camera.transform.position = newPos;
    }
      public void Flip(GameObject to_flip)
    {
        if (off == true)
        {
            to_flip.SetActive(true); // 显示
            off = false;
            return;
        }

        to_flip.SetActive(false); // 隐藏
        off = true;
    }
}
