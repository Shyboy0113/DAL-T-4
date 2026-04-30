using UnityEngine;
using DG.Tweening;

/// <summary>
/// 최상위 캔버스를 흔들고 회전시켜 화면 전체 UI 글리치 효과를 연출합니다.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class CanvasShake : MonoBehaviour
{
    [Header("Position Shake Settings")]
    [SerializeField] private float defaultDuration = 0.2f;
    [SerializeField] private float defaultStrength = 20f;
    [SerializeField] private int defaultVibrato = 30;

    [Header("Rotation Shake Settings")]
    [Tooltip("Z축 회전 강도 (도 단위)")]
    [SerializeField] private float rotationStrength = 7f; // 5~10도 사이 추천
    [SerializeField] private int rotationVibrato = 20;

    private RectTransform _rectTransform;
    private Vector2 _originalAnchorPos;
    private Vector3 _originalRotation;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _originalAnchorPos = _rectTransform.anchoredPosition;
        _originalRotation = _rectTransform.localEulerAngles;
    }

    private void OnEnable()
    {
        GameEvents.GlitchTriggered += TriggerRootShake;
        GameEvents.PlayerDied += TriggerRootShake;
    }

    private void OnDisable()
    {
        GameEvents.GlitchTriggered -= TriggerRootShake;
        GameEvents.PlayerDied -= TriggerRootShake;
        
        // 비활성화 시 즉시 정리
        _rectTransform.DOKill();
        ResetTransform();
    }

    public void TriggerRootShake()
    {
        TriggerRootShake(defaultDuration, defaultStrength, defaultVibrato);
    }

    public void TriggerRootShake(float duration, float strength, int vibrato)
    {
        // 1. 이전 트윈 종료 및 상태 초기화
        _rectTransform.DOKill(true); 
        ResetTransform();

        // 2. 위치 흔들림 (XY축)
        _rectTransform.DOShakeAnchorPos(duration, strength, vibrato, 90f, false, true);

        // 3. 회전 흔들림 (Z축만 흔들기 위해 Vector3 사용)
        // Z축에만 강도를 주고 X, Y는 0으로 설정합니다.
        Vector3 rotStrengthVector = new Vector3(0, 0, rotationStrength);
        _rectTransform.DOShakeRotation(duration, rotStrengthVector, rotationVibrato, 90f, true)
            .OnComplete(() => {
                ResetTransform(); // 종료 후 미세 오차 방지
            });
    }

    private void ResetTransform()
    {
        _rectTransform.anchoredPosition = _originalAnchorPos;
        _rectTransform.localEulerAngles = _originalRotation;
    }

    public void RaiseGlitch()
    {
        GameEvents.RaiseGlitchTriggered();
    }
}