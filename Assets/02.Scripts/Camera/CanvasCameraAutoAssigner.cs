using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Canvas))]
public class CanvasCameraAssigner : MonoBehaviour
{
    private Canvas _canvas;

    private void Awake()
    {
        _canvas = GetComponent<Canvas>();
        
        // 씬이 새로 로드될 때마다 실행될 함수 등록
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        // 메모리 누수 방지를 위해 이벤트 해제
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        if (_canvas.worldCamera != Camera.main && Camera.main != null)
        {
            _canvas.worldCamera = Camera.main;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 새로운 씬(B 씬)이 로드되었을 때, 메인 카메라를 찾아 캔버스에 할당합니다.
        // B 씬의 메인 카메라에는 반드시 "MainCamera" 태그가 붙어있어야 합니다.
        if (Camera.main != null)
        {
            _canvas.worldCamera = Camera.main;
        }
        else
        {
            Debug.LogWarning($"[{scene.name}] 씬에서 MainCamera 태그가 붙은 카메라를 찾을 수 없습니다.");
        }
    }
}