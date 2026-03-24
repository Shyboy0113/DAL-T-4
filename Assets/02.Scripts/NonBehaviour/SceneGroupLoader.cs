using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public static class SceneGroupLoader
{
    public static async void LoadGroup(SO_SceneGroup group)
    {
        // 1. 메인 씬 로드 (기존 씬들은 모두 Unload됨)
        var mainOp = SceneManager.LoadSceneAsync(group.mainScene.Name, LoadSceneMode.Single);
        while (!mainOp.isDone) await Task.Yield();

        // 2. 부가 씬들(Option 등)을 Additive로 로드
        foreach (var sceneRef in group.additiveScenes)
        {
            // 이미 로드되어 있는지 체크 (중복 로드 방지)
            if (!SceneManager.GetSceneByName(sceneRef.Name).isLoaded)
            {
                var addOp = SceneManager.LoadSceneAsync(sceneRef.Name, LoadSceneMode.Additive);
                while (!addOp.isDone) await Task.Yield();
            }
        }
    }
}