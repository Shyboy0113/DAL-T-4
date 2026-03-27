using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Eflatun.SceneReference;

public static class SceneLoader
{
    public static IEnumerator LoadScene(SceneReference sceneRef)
    {
        // 언로드할 씬을 미리 저장
        Scene previousScene = SceneManager.GetActiveScene();
        Debug.Log($"언로드할 씬: {previousScene.name}");

        var op = SceneManager.LoadSceneAsync(sceneRef.Name, LoadSceneMode.Additive);
        yield return new WaitUntil(() => op.isDone);
        
        // 이전 메인 씬 언로드 (Additive들은 유지)
        yield return SceneManager.UnloadSceneAsync(previousScene);
    }
}