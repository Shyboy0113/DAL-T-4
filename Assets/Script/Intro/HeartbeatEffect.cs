using UnityEngine;
using DG.Tweening; // DoTween 네임스페이스 추가

public class HeartbeatEffect : MonoBehaviour
{
    // 심장이 최대로 커질 크기
    public float maxScale = 1.1f;

    // 한 번 뛰는 데 걸리는 시간
    public float pulseDuration = 0.25f;

    public Ease setEase; 
    
    private Vector3 _originalScale; //초기 Scale 
    
    void Start()
    {
        _originalScale = transform.localScale;
        // transform.DOScale(최종 크기, 시간)
        transform.DOScale(maxScale, pulseDuration)
            .SetLoops(-1, LoopType.Yoyo) // 무한 반복, Yoyo는 갔다가 돌아오는 방식
            .SetEase(setEase);    // 부드러운 움직임을 위한 Ease 설정
    }

    public void TestButton()
    {
        transform.DOKill(); //DO 변환 정지
        transform.localScale = _originalScale;
        
        transform.DOScale(maxScale, pulseDuration)
            .SetLoops(-1, LoopType.Yoyo) // 무한 반복, Yoyo는 갔다가 돌아오는 방식
            .SetEase(setEase); 
    }
}