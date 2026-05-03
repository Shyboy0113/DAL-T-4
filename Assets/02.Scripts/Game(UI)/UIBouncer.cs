using UnityEngine;
using DG.Tweening;
using TMPro; // TextMeshPro를 사용하기 위해 추가

public class UIBouncer : MonoBehaviour
{
    [Header("Bounce Settings")]
    [SerializeField] private float jumpHeight = 20f; // 위로 뛰어오를 픽셀 높이
    [SerializeField] private float bounceTime = 0.25f; // 바운스 시간
    [SerializeField] private float maxBounceScale = 1.05f; // 바운스 사이즈

    [SerializeField] private bool isText = false;
    
    private RectTransform _rectTransform;
    private TextMeshProUGUI _textComponent; // 텍스트 컴포넌트를 저장할 변수
    private float _startY; // 원래 Y 위치를 기억할 변수

    private void Awake()
    {
        // 텍스트, 이미지, 버튼 상관없이 UI 요소라면 모두 호환됩니다.
        _rectTransform = GetComponent<RectTransform>();
        
        // [수정 1] Start가 아닌 Awake에서 초기 위치를 저장하여 실행 순서 꼬임 방지!
        _startY = _rectTransform.localPosition.y;

        // 텍스트라면 컴포넌트를 가져오고 시작부터 안 보이게 처리합니다.
        if (isText)
        {
            _textComponent = GetComponent<TextMeshProUGUI>();
            if (_textComponent != null)
            {
                _textComponent.alpha = 0f; // 알파값을 0으로 만들어서 투명하게
            }
        }
    }

    private void OnEnable()
    {
        GameEvents.GameOverUIEnabled += StartBounce;
        GameEvents.GameOverUIDisabled += StopBounce;
    }

    private void OnDisable()
    {
        // 구독 해제를 먼저 해주는 것이 더 안전한 패턴입니다.
        GameEvents.GameOverUIEnabled -= StartBounce;
        GameEvents.GameOverUIDisabled -= StopBounce;
        
        StopBounce();
    }

    private void StartBounce()
    {
        if (_rectTransform == null) return;

        // 중복 실행 방지를 위해 기존 애니메이션 킬
        _rectTransform.DOKill();
        if (isText && _textComponent != null) _textComponent.DOKill();

        // 텍스트일 경우 바운스 시작과 함께 글자가 보이도록 처리
        if (isText && _textComponent != null)
        {
            // 바로 보이게 하려면: _textComponent.alpha = 1f;
            // 부드럽게 나타나게 하려면 DOTween의 DOFade 사용 (추천)
            _textComponent.DOFade(1f, bounceTime); 
        }

        // 1. 위아래로 점프하는 애니메이션
        _rectTransform.DOLocalMoveY(_startY + jumpHeight, bounceTime)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.OutQuad);

        // 2. 스케일 애니메이션
        _rectTransform.DOScale(maxBounceScale, bounceTime)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.OutQuad);
    }

    private void StopBounce()
    {
        // [수정 2] 파괴되는 과정에서 에러가 나지 않도록 맨 위에서 방어
        if (_rectTransform == null) return;

        // 컴포넌트 비활성화 시 Dotween 애니메이션 삭제
        _rectTransform.DOKill();
        if (isText && _textComponent != null) _textComponent.DOKill();
        
        // 위치와 스케일 모두 원래대로 완벽하게 복구
        Vector3 pos = _rectTransform.localPosition;
        pos.y = _startY;
        _rectTransform.localPosition = pos;
        
        _rectTransform.localScale = Vector3.one;

        // 텍스트라면 다시 보이지 않게 투명화 처리
        if (isText && _textComponent != null)
        {
            _textComponent.alpha = 0f;
        }
    }
}