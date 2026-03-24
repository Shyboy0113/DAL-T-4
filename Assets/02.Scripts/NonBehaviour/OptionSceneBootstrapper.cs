#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// 유니티 에디터에서 작업할 때, 옵션 창이 자동으로 로드되게 하는 스크립트

[InitializeOnLoad]
public static class OptionSceneBootstrapper
{
    static OptionSceneBootstrapper()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeChanged; // 중복 등록 방지
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        
        Debug.Log("<color=green>SystemSceneBootstrapper: 에디터 이벤트 등록 완료</color>");
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            Debug.Log("플레이 모드 진입 확인");
            
            string path = "Assets/ScriptableObject/SO_SceneReference/SO_OptionScene.asset";
            
            // Project - Asset - ScriptableObject 폴더에서 optionScene이 들어가있는 SO_SceneReference를 탐색 
            SO_SceneReference SO_optionSceneReference = AssetDatabase.LoadAssetAtPath<SO_SceneReference>(path);
            
            // SO_SceneReference이 존재하지 않다면
            if (SO_optionSceneReference == null)
            {
                Debug.LogError($"[Bootstrapper] 해당 경로에 SO가 없습니다: {path}");
                return;
            }

            //SO_SceneReference 내부에 optionScene이 할당돼있지 않다면
            if (SO_optionSceneReference.scene == null)
            {
                Debug.LogError("[Bootstrapper] SO는 찾았으나 내부 SceneReference가 비어있습니다.");
                return;
            }
            
            
            
            // 정상이라면, Scene 이름이 Option_Additive Scene으로 출력됨
            string optionSceneName = SO_optionSceneReference.scene.Name;

            // 이름으로 해당 Scene이 존재하는지 다시 탐색
            Scene scene = SceneManager.GetSceneByName(optionSceneName);

            // 실제로 존재한다면
            if (!scene.isLoaded)
            {
                // Option Scene을 Additive Mode로 Load
                SceneManager.LoadScene(optionSceneName, LoadSceneMode.Additive);
            }
        }
    }
}
#endif
