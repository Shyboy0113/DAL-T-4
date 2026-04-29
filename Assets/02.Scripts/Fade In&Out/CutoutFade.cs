// CutoutFade.cs
// 이 스크립트는 Mask 컴포넌트가 있는 'Mask Image' 오브젝트에 붙여주세요.
using UnityEngine;
using DG.Tweening;
using System;

public class CutoutFade : MonoBehaviour
{
    public RectTransform rectTransform;
    public float fadeDuration = 1.0f;
    public Ease easeType = Ease.Unset;

    [SerializeField] private RectTransform background;

    private void Awake()
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }
    }
    
    private void Start()
    {
        //if (background != null) background.sizeDelta = new Vector2(Screen.width , Screen.height);
        if (background != null) background.sizeDelta = new Vector2(1920 , 1080);
        
    }
    
    // UI의 크기를 0에서 해상도의 2배로 확장시켜 화면을 덮습니다.
    public void FadeIn(Action onFadeComplete = null)
    {
        // FadeIn은 특별한 콜백이 필요 없으므로 그대로 둡니다.
        rectTransform.sizeDelta = Vector2.zero;
        //Vector2 targetSize = new Vector2(Screen.width * 2, Screen.height * 2);
        Vector2 targetSize = new Vector2(1920 * 2, 1080 * 2);
        rectTransform.DOSizeDelta(targetSize, fadeDuration).SetEase(easeType).OnComplete(() =>
        {
            onFadeComplete?.Invoke();
        });
    }

    public void ResizeResolution()
    {
        //if (background != null) background.sizeDelta = new Vector2(Screen.width , Screen.height);
        if (background != null) background.sizeDelta = new Vector2(1920 , 1080);
        
        //rectTransform.sizeDelta = new Vector2(Screen.width * 2, Screen.height * 2);
        rectTransform.sizeDelta = new Vector2(1920 * 2, 1080 * 2);
        
    }

    // UI의 크기를 현재 해상도에서 0으로 축소시켜 사라지게 합니다.
    public void FadeOut(Action onFadeComplete = null)
    {
        // DOSizeDelta 뒤에 .OnComplete()를 연결하는 것이 핵심입니다.
        rectTransform.DOSizeDelta(Vector2.zero, fadeDuration)
            .SetEase(easeType)
            .OnComplete(() =>
            {
                // 이 블록 안의 코드는 애니메이션이 '정말로' 끝났을 때 실행됩니다.
                onFadeComplete?.Invoke();
            });
    }

    private void OnDestroy()
    {
        rectTransform.DOKill(this);
    }
}