using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using Eflatun.SceneReference;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Bootstrapper : MonoBehaviour
{
#if UNITY_EDITOR
    private const string PreviousSceneKey = "DAL-T4.PreviousScenePath";
#endif

    [SerializeField] private SO_SceneGroup defaultGroup;
    [SerializeField] private SceneReference bootstrapScene; // Bootstrap 씬 직접 참조
    
    private void Start()
    {
        StartCoroutine(LoadAdditiveScenes());
    }

    private IEnumerator LoadAdditiveScenes()
    {
        // 1. Additive 씬들 먼저 로드 (System → Option 순서)
        foreach (var sceneRef in defaultGroup.additiveScenes)
        {
            if (!SceneManager.GetSceneByName(sceneRef.Name).isLoaded)
            {
                var op = SceneManager.LoadSceneAsync(sceneRef.Name, LoadSceneMode.Additive);
                yield return new WaitUntil(() => op.isDone);
            }
        }

#if UNITY_EDITOR
        string previousScene = EditorPrefs.GetString(PreviousSceneKey);
        EditorPrefs.DeleteKey(PreviousSceneKey);

        if (!string.IsNullOrEmpty(previousScene))
        {
            // 2. 목적지 씬 Additive 로드
            var op = SceneManager.LoadSceneAsync(previousScene, LoadSceneMode.Additive);
            yield return new WaitUntil(() => op.isDone);

            // 목적지 씬을 활성 씬으로 명시적 설정
            Scene destination = SceneManager.GetSceneByPath(previousScene);
            
            SceneManager.SetActiveScene(destination);
            
            // 3. Bootstrap 씬만 언로드
            yield return SceneManager.UnloadSceneAsync(bootstrapScene.Name);
            yield break;
        }
#endif

        // 4. 빌드: 목적지 씬 Additive 로드 후 Bootstrap 언로드
        var mainOp = SceneManager.LoadSceneAsync(defaultGroup.mainScene.Name, LoadSceneMode.Additive);
        yield return new WaitUntil(() => mainOp.isDone);

        // 목적지 씬을 활성 씬으로 명시적 설정
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(defaultGroup.mainScene.Name));

        // Bootstrap 씬만 언로드
        yield return SceneManager.UnloadSceneAsync(bootstrapScene.Name);
    }
}