using UnityEngine;
using DG.Tweening;

/// <summary>
/// 메인 카메라 위치를 Map 1 타일들의 중심으로 초기화하고,
/// 플레이어가 화면 좌/우 경계에 도달하면 카메라를 해당 방향으로 이동합니다.
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
    }

    private void OnDisable()
    {
        GameEvents.MapInitialized      -= OnMapInitialized;
        GameEvents.PlayerActionFinished -= OnPlayerActionFinished;
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

    private void OnPlayerActionFinished()
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
        transform.DOMove(target, tweenDuration)
                 .SetEase(Ease.OutCubic)
                 .OnComplete(() => _isTweening = false);
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
