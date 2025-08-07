using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System;
using DG.Tweening;

public class TransparentHoleFade : MonoBehaviour
{    
    //싱글톤에 기존 찌꺼기 데이터들이 남아있지 않게 초기 설정해주는 스크립트입니다.
    public Image transparentCircle;
    public Image fadeSquare;

    public Ease DOEase;

    public float waitTime = 0.5f;

    public float maxScale = 55f;
    
    private void Start()
    {
        FadeIn(); // 1 -> 55 Size
    }

    // onFadeComplete = null 로 기본값을 설정해주면, 이 매개변수는 선택사항이 됩니다.
    public void FadeOut(Action onFadeComplete = null)
    {
        StartCoroutine(CircleFadeOut(onFadeComplete));
    }

    public void FadeIn()
    {
        StartCoroutine(CircleFadeIn());
    }
    
    IEnumerator CircleFadeIn()
    {
        yield return new WaitForSeconds(waitTime);
        
        transparentCircle.transform.DOScale(maxScale,  1.0f).SetEase(DOEase);
        fadeSquare.gameObject.SetActive(false);
        yield return new WaitForSeconds(1.0f);
        
    }

    IEnumerator CircleFadeOut(Action onFadeComplete)
    {
        transparentCircle.transform.DOScale(1.05f,  0.8f).SetEase(DOEase);
        yield return new WaitForSeconds(0.7f);
        
        fadeSquare.gameObject.SetActive(true);
        transparentCircle.transform.DOScale(1.0f,  0.1f).SetEase(DOEase);
        
        Debug.Log("잠시 대기");
        yield return new WaitForSeconds(waitTime);
        
        // 애니메이션이 끝나면, 매개변수로 받아온 행동(콜백)을 실행
        // null이면 무시, null이 아니면 실행
        onFadeComplete?.Invoke();
        
    }
    
}
