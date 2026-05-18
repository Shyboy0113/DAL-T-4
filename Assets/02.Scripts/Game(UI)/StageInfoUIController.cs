using System;
using UnityEngine;
using DG.Tweening;

public class StageInfoUIController : MonoBehaviour
{
    private RectTransform rectTransform;
    
    [Header("설정")]
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private Ease showEase = Ease.OutCubic;
    [SerializeField] private Ease hideEase = Ease.InCubic;
    
    [SerializeField] private float targetX;
    [SerializeField] private float hideX;

    private CanvasGroup _canvasGroup;
    
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        // 초기 위치 강제 설정
        rectTransform.anchoredPosition = new Vector2(hideX, rectTransform.anchoredPosition.y);

        _canvasGroup = GetComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        // 오브젝트가 켜지자마자 등장 애니메이션 실행
        ShowPanel();
    }

    private void OnDisable()
    {
        // 꺼질 때는 다음 활성화를 위해 즉시 숨김 위치로 리셋
        rectTransform.DOKill();
        rectTransform.anchoredPosition = new Vector2(hideX, rectTransform.anchoredPosition.y);
    }

    public void ShowPanel()
    {
        rectTransform.DOKill();
        _canvasGroup.alpha = 1;
        rectTransform.DOAnchorPosX(targetX, duration).SetEase(showEase);
    }

    // 연출이 끝난 후 실행할 작업을 위해 Act
    // ion 매개변수 추가
    public void HidePanel(Action onComplete = null)
    {
        rectTransform.DOKill();
        rectTransform.DOAnchorPosX(hideX, duration)
            .SetEase(hideEase)
            .OnComplete(() =>
            {
                _canvasGroup.alpha = 0;
                onComplete?.Invoke();
            }); // 연출 종료 후 콜백 실행
    }
}