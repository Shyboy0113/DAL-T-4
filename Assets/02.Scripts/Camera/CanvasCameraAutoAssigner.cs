using UnityEngine;

[RequireComponent(typeof(Canvas))]
public class CanvasCameraAutoAssigner : MonoBehaviour
{
    private Canvas _canvas;

    private void Awake()
    {
        _canvas = GetComponent<Canvas>();
    }

    private void LateUpdate()
    {
        if (_canvas.renderMode != RenderMode.ScreenSpaceCamera) return;
        if (_canvas.worldCamera != null) return; // 이미 있으면 패스

        Camera main = Camera.main;
        if (main != null)
            _canvas.worldCamera = main;
    }
}