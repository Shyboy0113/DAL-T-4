using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class StartManager : MonoBehaviour
{    
    //싱글톤에 기존 찌꺼기 데이터들이 남아있지 않게 초기 설정해주는 스크립트입니다.
    
    public Image transparentCircle;
    public Image fadeSquare;

    public Ease DOEase;
    
    private void Start()
    {
        GameManager.Instance.ResetStage();

        StartCoroutine(CircleFadeIn()); // 1 -> 55 Size

    }

    public void Restart()
    {
        StartCoroutine(CircleFadeOut()); // 55 -> 1 Size
    }

    public void TestButton()
    {
        StartCoroutine(TestFade());
    }

    IEnumerator TestFade()
    {
        transparentCircle.transform.DOScale(1.05f,  0.8f).SetEase(DOEase);
        yield return new WaitForSeconds(0.7f);
        
        fadeSquare.gameObject.SetActive(true);
        transparentCircle.transform.DOScale(1.0f,  0.1f).SetEase(DOEase);
        
        Debug.Log("잠시 대기");
        yield return new WaitForSeconds(0.5f);
        
        transparentCircle.transform.DOScale(55f,  1.0f).SetEase(DOEase);
        fadeSquare.gameObject.SetActive(false);
        yield return new WaitForSeconds(1.0f);
        
    }
    
    IEnumerator CircleFadeIn()
    {
        yield return new WaitForSeconds(0.5f);
        
        transparentCircle.transform.DOScale(55f,  1.0f).SetEase(DOEase);
        fadeSquare.gameObject.SetActive(false);
        yield return new WaitForSeconds(1.0f);
        
    }

    IEnumerator CircleFadeOut()
    {
        transparentCircle.transform.DOScale(1.05f,  0.8f).SetEase(DOEase);
        yield return new WaitForSeconds(0.7f);
        
        fadeSquare.gameObject.SetActive(true);
        transparentCircle.transform.DOScale(1.0f,  0.1f).SetEase(DOEase);
        
        Debug.Log("잠시 대기");
        yield return new WaitForSeconds(0.5f);
        
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
}
