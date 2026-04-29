using UnityEngine;
using DG.Tweening;

/// <summary>
/// 메인 카메라 위치를 Map 1 타일들의 중심으로 초기화하고,
/// 플레이어가 화면 좌/우 경계에 도달하면 카메라를 해당 방향으로 이동합니다.
/// 글리치 이벤트 발생 시 카메라 셰이크 효과를 줍니다.
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerTransform;

    [Header("Edge Scroll")]
    [Tooltip("화면 가장자리 기준 비율 (0~0.5). 플레이어가 이 안쪽으로 들어오면 스크롤 발생")]
    [SerializeField] private float edgeThreshold = 0.25f;

    [Tooltip("스크롤 이동 거리 (World 단위)")]
    [SerializeField] private float scrollStep = 2f;

    [Tooltip("카메라 이동 트윈 지속 시간")]
    [SerializeField] private float tweenDuration = 0.25f;

    [Header("Glitch Shake")]
    [Tooltip("셰이크 지속 시간")]
    [SerializeField] private float shakeDuration = 0.2f;
    
    [Tooltip("셰이크 강도 (X, Y 축)")]
    [SerializeField] private float shakeStrength = 0.5f;
    
    [Tooltip("셰이크 진동수 (값이 클수록 덜덜거리는 기계적인 느낌이 강해짐)")]
    [SerializeField] private int shakeVibrato = 20;

    private Camera _camera;
    private bool   _isTweening;

    // ─────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    private void OnEnable()
    {
        GameEvents.MapInitialized      += OnMapInitialized;
        GameEvents.PlayerActionFinished += OnPlayerActionFinished;
        GameEvents.GlitchTriggered += TriggerGlitchShake; 
    }

    private void OnDisable()
    {
        GameEvents.MapInitialized      -= OnMapInitialized;
        GameEvents.PlayerActionFinished -= OnPlayerActionFinished;
        GameEvents.GlitchTriggered -= TriggerGlitchShake;
    }

    private void Start()
    {
        CenterOnMap1Tiles(instant: true);
    }

    // ─────────────────────────────────────────────────────────────────
    // 이벤트 핸들러
    // ─────────────────────────────────────────────────────────────────

    private void OnMapInitialized()
    {
        // 맵 교체 시 즉시 중심으로 이동
        DOTween.Kill(transform, complete: false);
        _isTweening = false;
        CenterOnMap1Tiles(instant: true);
    }

    private void OnPlayerActionFinished(int layer)
    {
        if (playerTransform == null || _isTweening) return;

        Vector3 viewportPos = _camera.WorldToViewportPoint(playerTransform.position);

        float deltaX = 0f;
        if      (viewportPos.x < edgeThreshold)       deltaX = -scrollStep;
        else if (viewportPos.x > 1f - edgeThreshold)  deltaX =  scrollStep;
        
        float deltaY = 0f;
        if      (viewportPos.y < edgeThreshold)       deltaY = -scrollStep;
        else if (viewportPos.y > 1f - edgeThreshold)  deltaY =  scrollStep;
        
        if (Mathf.Approximately(deltaX, 0f) && Mathf.Approximately(deltaY, 0f)) return;

        Vector3 target = transform.position + new Vector3(deltaX, deltaY, 0f);
        _isTweening = true;
        
        // 이동 중 셰이크가 발생해도 목표 지점으로 자연스럽게 갈 수 있도록 SetRelative(false) 상태로 작동
        transform.DOMove(target, tweenDuration)
                 .SetEase(Ease.OutCubic)
                 .OnComplete(() => _isTweening = false);
    }

    // 외부에서 직접 호출하거나 이벤트로 연결할 셰이크 함수
    public void TriggerGlitchShake()
    {
        // 카메라의 Z축이 흔들려 화면 크기가 줌인/줌아웃 되는 것을 막기 위해 Vector3로 XY축만 지정
        Vector3 strengthVector = new Vector3(shakeStrength, shakeStrength, 0f);
        
        // Transform에 직접 DOShakePosition 적용 (기존 이동 트윈을 덮어쓰지 않고 더해집니다)
        transform.DOComplete(withCallbacks: true); // 흔들림 중복 누적 방지
        transform.DOShakePosition(shakeDuration, strengthVector, shakeVibrato, randomness: 90f, snapping: false, fadeOut: true);
    }

    // ─────────────────────────────────────────────────────────────────
    // 내부 유틸
    // ─────────────────────────────────────────────────────────────────

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
        Vector3 target = new Vector3(center.x, center.y, transform.position.z);

        if (instant)
            transform.position = target;
        else
        {
            _isTweening = true;
            transform.DOMove(target, tweenDuration)
                     .SetEase(Ease.OutCubic)
                     .OnComplete(() => _isTweening = false);
        }
    }
}