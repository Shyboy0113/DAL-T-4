using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public enum TutorialType
{
    None,
    LeftAlt, // 시계 방향 회전
    Tab,     // 반시계 방향 회전
    F4       // 오른쪽 이동
}

public class TutorialPlayerLogic : MonoBehaviour
{
    [SerializeField] private RectTransform playerRect;
    [SerializeField] private RectTransform arrowRect;
    
    [Header("Animation Settings")]
    [SerializeField] private TutorialType selectedTutorial;

    private Coroutine _currentLoop;
    private Vector2 _initialPlayerPos;

    private void Awake()
    {
        // 시작 시 초기 위치를 저장합니다.
        _initialPlayerPos = playerRect.anchoredPosition;
    }

    private void OnEnable()
    {
        //Close 버튼으로 패널을 비활성화 했을 때, dotween이 중간에 끊켜 transform이 z축으로 약간 회전돼있는 상태 방지 
        transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        
        PlaySelectedTutorial();
    }

    private void OnDisable()
    {
        StopTutorial();
    }

    private void Start()
    {
        PlaySelectedTutorial();
    }

    // 인스펙터의 버튼이나 OnEnable 등에서 호출하여 튜토리얼을 시작합니다.
    public void PlaySelectedTutorial()
    {
        StopTutorial(); // 기존 실행 중인 루프 중단

        switch (selectedTutorial)
        {
            case TutorialType.LeftAlt:
                _currentLoop = StartCoroutine(RotateRoutine(-90f)); // 시계 방향
                break;
            case TutorialType.Tab:
                _currentLoop = StartCoroutine(RotateRoutine(90f));  // 반시계 방향
                break;
            case TutorialType.F4:
                _currentLoop = StartCoroutine(MoveRoutine());
                break;
        }
    }

    // 1. 회전 로직 루프 (Alt, Tab)
    private IEnumerator RotateRoutine(float targetAngle)
    {
        while (true)
        {
            
            // 초기화: 화살표를 0도(오른쪽)로 리셋
            arrowRect.localRotation = Quaternion.identity;
            
            yield return new WaitForSeconds(0.5f); // 루프 간 간격 (0.5초)

            // 동작: 지정된 각도로 회전
            arrowRect.DORotate(new Vector3(0, 0, targetAngle), 0.5f)
                .SetEase(Ease.OutElastic);
            
            // 본체 펀치 효과 (선택 사항)
            playerRect.DOPunchRotation(new Vector3(0, 0, 15f), 0.25f, 10, 0.5f);

            // 1초 대기 후 루프 재시작
            yield return new WaitForSeconds(1.5f); // 애니메이션 시간(0.5) + 대기시간(1.0)
        }
    }

    // 2. 이동 로직 루프 (F4)
    private IEnumerator MoveRoutine()
    {
        while (true)
        {
            // 초기화: 원래 위치로 즉시 복구
            playerRect.anchoredPosition = _initialPlayerPos;
            
            // 동작: 오른쪽으로 100만큼 이동
            playerRect.DOAnchorPosX(_initialPlayerPos.x + 155f, 0.5f)
                .SetEase(Ease.OutQuad);

            // 1초 대기 후 루프 재시작
            yield return new WaitForSeconds(1.0f);
        }
    }

    public void StopTutorial()
    {
        if (_currentLoop != null)
        {
            StopCoroutine(_currentLoop);
            _currentLoop = null;
        }

        // 모든 트윈 제거 및 위치 리셋
        DOTween.Kill(playerRect);
        DOTween.Kill(arrowRect);
        
        playerRect.anchoredPosition = _initialPlayerPos;
        arrowRect.localRotation = Quaternion.identity;
    }
}