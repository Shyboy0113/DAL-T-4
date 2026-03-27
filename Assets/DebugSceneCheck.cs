using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// 아무 씬에나 임시로 붙여서 확인
public class DebugSceneCheck : MonoBehaviour
{
    private void Start()
    {
        StartCoroutine(Check(0f));
        StartCoroutine(Check(1f));
    }

    private IEnumerator Check(float time)
    {
        yield return new WaitForSeconds(time); // Bootstrapper 언로드 완료 후 확인

        var eventSystem = UnityEngine.EventSystems.EventSystem.current;
        Debug.Log($"EventSystem: {eventSystem?.gameObject.scene.name ?? "없음"}");

        var listeners = FindObjectsOfType<AudioListener>();
        Debug.Log($"AudioListener 개수: {listeners.Length}");
        foreach (var l in listeners)
            Debug.Log($"AudioListener 위치: {l.gameObject.scene.name} / {l.gameObject.name}");

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            Debug.Log($"로드된 씬 [{i}]: {scene.name} / 활성씬: {scene == SceneManager.GetActiveScene()}");
        }
    }
}