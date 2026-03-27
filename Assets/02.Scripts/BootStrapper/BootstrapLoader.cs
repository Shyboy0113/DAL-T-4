// BootstrapLoader.cs - 씬에 배치 불필요, 프로젝트 어디든 존재하면 됨
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class BootstrapLoader
{
    //Static 클래스라 어쩔 수 없이 문자열을 사용해야 함
    private const string BootstrapSceneName = "Bootstrapper";

#if UNITY_EDITOR
    private const string PreviousSceneKey = "DAL-T4.PreviousScenePath";
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Execute()
    {
        // 이미 Bootstrap 씬이면 개입 안 함
        if (SceneManager.GetActiveScene().name == BootstrapSceneName) return;

#if UNITY_EDITOR
        // 에디터: 현재 씬 경로 저장 후 Bootstrap으로
        EditorPrefs.SetString(PreviousSceneKey, SceneManager.GetActiveScene().path);
#endif

        SceneManager.LoadScene(BootstrapSceneName, LoadSceneMode.Single);
    }
}