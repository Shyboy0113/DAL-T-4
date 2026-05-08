using UnityEngine;
using DG.Tweening;

public enum CameraTrackingMode
{
    FrameEntireMap, // 맵 전체 타일의 정중앙에 카메라를 고정하는 방식
    EdgeScroll      // 플레이어가 화면 가장자리에 닿으면 스크롤되는 기존 방식
}

/// <summary>
/// .
/// 메인 카메라 위치를 제어합니다.
/// 인스펙터의 CameraTrackingMode에 따라 전체 맵 고정 또는 가장자리 스크롤 방식을 선택할 수 있습니다.
/// 글리치 이벤트 발생 시 카메라 셰이크 효과를 줍니다.
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("Camera Mode")]
    [Tooltip("카메라 추적 방식을 선택하세요.")]
    [SerializeField] private CameraTrackingMode cameraMode = CameraTrackingMode.FrameEntireMap;

    [Header("References")]
    [Tooltip("Edge Scroll 모드에서 사용할 플레이어 Transform")]
    [SerializeField] private Transform playerTransform;

    [Header("Edge Scroll Settings")]
    [Tooltip("화면 가장자리 기준 비율 (0~0.5). 플레이어가 이 안쪽으로 들어오면 스크롤 발생")]
    [SerializeField] private float edgeThreshold = 0.25f;

    [Tooltip("스크롤 이동 거리 (World 단위)")]
    [SerializeField] private float scrollStep = 2f;

    [Header("Common Settings")]
    [Tooltip("카메라 이동 트윈 지속 시간")]
    [SerializeField] private float tweenDuration = 0.25f;

    [Tooltip("카메라 위치에 적용할 추가 오프셋 (X, Y)")]
    [SerializeField] private Vector2 cameraOffset = Vector2.zero;

    [Header("Glitch Shake")]
    [Tooltip("셰이크 지속 시간")]
    [SerializeField] private float shakeDuration = 0.2f;
    
    [Tooltip("셰이크 강도 (X, Y 축)")]
    [SerializeField] private float shakeStrength = 0.5f;
    
    [Tooltip("셰이크 진동수 (값이 클수록 덜덜거리는 기계적인 느낌이 강해짐)")]
    [SerializeField] private int shakeVibrato = 20;

    private Camera _camera;
    private bool   _isTweening;

    // 카메라 오프셋 중복 누적(Drift)을 방지하기 위한 순수 스크롤 기준 좌표
    private Vector3 _baseScrollPosition;

    // ─────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    private void OnEnable()
    {
        GameEvents.MapInitialized      += OnMapInitialized;
        GameEvents.PlayerActionFinished += OnPlayerActionFinished;
        GameEvents.GlitchTriggered     += TriggerGlitchShake; 
    }

    private void OnDisable()
    {
        GameEvents.MapInitialized      -= OnMapInitialized;
        GameEvents.PlayerActionFinished -= OnPlayerActionFinished;
        GameEvents.GlitchTriggered     -= TriggerGlitchShake;
    }

    private void Start()
    {
        InitializeCameraPosition();
    }

    // ─────────────────────────────────────────────────────────────────
    // 이벤트 핸들러
    // ─────────────────────────────────────────────────────────────────

    private void OnMapInitialized()
    {
        // 맵 교체 시 즉시 중심으로 이동
        DOTween.Kill(transform, complete: false);
        _isTweening = false;
        InitializeCameraPosition();
    }

    private void OnPlayerActionFinished(int layer)
    {
        if (_isTweening) return;

        if (cameraMode == CameraTrackingMode.FrameEntireMap)
        {
            // [새로운 방식] 액션 종료 후 맵에 변화가 생겼을 수 있으므로 전체 맵 중앙 재계산
            FrameEntireMap(instant: false);
        }
        else if (cameraMode == CameraTrackingMode.EdgeScroll)
        {
            // [기존 방식] 플레이어가 가장자리에 도달하면 스크롤
            if (playerTransform == null) return;

            Vector3 viewportPos = _camera.WorldToViewportPoint(playerTransform.position);

            float deltaX = 0f;
            if      (viewportPos.x < edgeThreshold)       deltaX = -scrollStep;
            else if (viewportPos.x > 1f - edgeThreshold)  deltaX =  scrollStep;
            
            float deltaY = 0f;
            if      (viewportPos.y < edgeThreshold)       deltaY = -scrollStep;
            else if (viewportPos.y > 1f - edgeThreshold)  deltaY =  scrollStep;
            
            if (Mathf.Approximately(deltaX, 0f) && Mathf.Approximately(deltaY, 0f)) return;

            // 기준 위치 갱신 후 오프셋 적용하여 이동
            _baseScrollPosition += new Vector3(deltaX, deltaY, 0f);
            Vector3 targetPosition = _baseScrollPosition + new Vector3(cameraOffset.x, cameraOffset.y, 0f);
            
            _isTweening = true;
            transform.DOMove(targetPosition, tweenDuration)
                     .SetEase(Ease.OutCubic)
                     .OnComplete(() => _isTweening = false);
        }
    }

    public void TriggerGlitchShake()
    {
        Vector3 strengthVector = new Vector3(shakeStrength, shakeStrength, 0f);
        transform.DOComplete(withCallbacks: true); 
        transform.DOShakePosition(shakeDuration, strengthVector, shakeVibrato, randomness: 90f, snapping: false, fadeOut: true);
    }

    // ─────────────────────────────────────────────────────────────────
    // 내부 로직 분리
    // ─────────────────────────────────────────────────────────────────

    private void InitializeCameraPosition()
    {
        if (cameraMode == CameraTrackingMode.FrameEntireMap)
        {
            FrameEntireMap(instant: true);
        }
        else
        {
            // 기존 EdgeScroll 모드일 때 초기화 로직 (타일 위치 평균값)
            CenterOnMap1Tiles(instant: true);
        }
    }

    /// <summary>
    /// [새로운 방식] Map 1 타일들의 Bounding Box를 계산하여 '진짜' 정중앙에 위치시킵니다.
    /// </summary>
    private void FrameEntireMap(bool instant)
    {
        int map1Layer = LayerMask.NameToLayer("Map 1");
        var tiles = FindObjectsByType<TileBehaviour>(FindObjectsSortMode.None);

        if (tiles.Length == 0) return;

        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;
        int count = 0;

        foreach (var tile in tiles)
        {
            if (tile.gameObject.layer == map1Layer)
            {
                Vector3 pos = tile.transform.position;
                if (pos.x < minX) minX = pos.x;
                if (pos.x > maxX) maxX = pos.x;
                if (pos.y < minY) minY = pos.y;
                if (pos.y > maxY) maxY = pos.y;
                count++;
            }
        }

        if (count == 0) return;

        Vector3 center = new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, transform.position.z);
        
        _baseScrollPosition = center;
        Vector3 targetPosition = _baseScrollPosition + new Vector3(cameraOffset.x, cameraOffset.y, 0f);

        if (instant)
        {
            transform.position = targetPosition;
        }
        else
        {
            _isTweening = true;
            transform.DOMove(targetPosition, tweenDuration)
                     .SetEase(Ease.OutCubic)
                     .OnComplete(() => _isTweening = false);
        }
    }

    /// <summary>
    /// [기존 방식] Map 1 타일들의 좌표를 모두 더해 나눈 평균값을 중앙으로 잡습니다.
    /// </summary>
    private void CenterOnMap1Tiles(bool instant)
    {
        int map1Layer = LayerMask.NameToLayer("Map 1");
        var tiles = FindObjectsByType<TileBehaviour>(FindObjectsSortMode.None);

        Vector3 sum   = Vector3.zero;
        int     count = 0;

        foreach (var tile in tiles)
        {
            if (tile.gameObject.layer == map1Layer)
            {
                sum += tile.transform.position;
                count++;
            }
        }

        if (count == 0) return;

        Vector3 center = sum / count;
        
        _baseScrollPosition = new Vector3(center.x, center.y, transform.position.z);
        Vector3 targetPosition = _baseScrollPosition + new Vector3(cameraOffset.x, cameraOffset.y, 0f);

        if (instant)
        {
            transform.position = targetPosition;
        }
        else
        {
            _isTweening = true;
            transform.DOMove(targetPosition, tweenDuration)
                     .SetEase(Ease.OutCubic)
                     .OnComplete(() => _isTweening = false);
        }
    }
}