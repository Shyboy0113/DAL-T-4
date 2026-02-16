using System.Collections;
using UnityEngine;
using DG.Tweening;
using TMPro;

// 스테이지가 클리어 됐을 경우, Stage Clear 문구가 뜨도록 하는 텍스트. Clear Text에 들어가있어야 함

public class FadeClearText : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    
    private void OnEnable()
    {
        GameEvents.StageCleared += FadeIn;
    }

    private void OnDestroy()
    {
        GameEvents.StageCleared -= FadeIn;
    }

    private void Awake()
    {
        text = GetComponent<TextMeshProUGUI>(); //컴포넌트 가져오기
        
    }

    public void FadeIn()
    {
        text.DOFade(1f, 1f);
        text.rectTransform.DOScale(1f, 1f).SetEase(Ease.OutElastic);
    }
    
}
