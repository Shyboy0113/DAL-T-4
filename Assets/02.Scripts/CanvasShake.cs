using UnityEngine;
using DG.Tweening;

/// <summary>
/// 최상위 캔버스를 흔들어 화면 전체 UI 글리치 효과를 연출합니다.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class CanvasShake : MonoBehaviour
{
    [Header("Shake Settings")]
    [SerializeField] private float defaultDuration = 0.2f;
    [SerializeField] private float defaultStrength = 20f;
    [SerializeField] private int defaultVibrato = 30;

    private RectTransform _rectTransform;
    private Vector2 _originalAnchorPos;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _originalAnchorPos = _rectTransform.anchoredPosition;
    }

    private void OnEnable()
    {
        GameEvents.GlitchTriggered += TriggerRootShake;
    }

    private void OnDisable()
    {
        GameEvents.GlitchTriggered -= TriggerRootShake;
        
        // 비활성화 시 위치 초기화 및 트윈 정리
        _rectTransform.DOKill();
        _rectTransform.anchoredPosition = _originalAnchorPos;
    }

    /// <summary>
    /// 외부에서 호출할 수 있는 셰이크 실행 메서드
    /// </summary>
    public void TriggerRootShake()
    {
        TriggerRootShake(defaultDuration, defaultStrength, defaultVibrato);
    }

    public void TriggerRootShake(float duration, float strength, int vibrato)
    {
        // 1. 이전 셰이크가 진행 중이라면 즉시 종료하고 원위치로 복구 (위치 밀림 방지)
        _rectTransform.DOKill(true); 
        _rectTransform.anchoredPosition = _originalAnchorPos;

        // 2. 새로운 셰이크 실행
        // UI 전체이므로 강도를 조금 세게(Vector2) 주는 것이 효과적입니다.
        _rectTransform.DOShakeAnchorPos(duration, strength, vibrato, 90f, false, true)
            .OnComplete(() => {
                // 종료 후 소수점 오차로 인한 미세한 어긋남 방지
                _rectTransform.anchoredPosition = _originalAnchorPos;
            });
    }

    public void RaiseGlitch()
    {
        GameEvents.RaiseGlitchTriggered();
    }
}