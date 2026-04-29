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
        
        var op = SceneManager.LoadSceneAsync(sceneRef.Name, LoadSceneMode.Additive);
        yield return new WaitUntil(() => op.isDone);

        
        // 새 씬이 활성화되기 전에, 렌더 텍스처 뒤집힘 버그를 막기 위해 
        // 이전 씬의 모든 카메라를 비활성화합니다.
        foreach (GameObject rootObj in previousScene.GetRootGameObjects())
        {
            Camera[] cameras = rootObj.GetComponentsInChildren<Camera>();
            foreach (Camera cam in cameras)
            {
                cam.enabled = false;
            }
        }
        
        
        // 새 씬을 활성 씬으로 명시적 설정 (미설정 시 다음 전환에서 GetActiveScene()이 Option/System을 반환하는 버그 방지)
        Scene newScene = SceneManager.GetSceneAt(SceneManager.sceneCount - 1);
        SceneManager.SetActiveScene(newScene);
        
        // 이전 메인 씬 언로드 (Additive들은 유지)
        yield return SceneManager.UnloadSceneAsync(previousScene);
    }
}