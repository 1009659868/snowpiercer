using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    public void GameStart(){
        SceneManager.LoadScene(1);
    }
    public void GameStop(){
        
        StartCoroutine(DelayExit());
    }
    private IEnumerator DelayExit(){
        yield return new WaitForSeconds(0.5f);

        // 正式构建中退出游戏
        Application.Quit();
        
        // 在Unity编辑器中停止运行（仅编辑器生效）
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
