using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ResetManagement : MonoBehaviour
{    
    //싱글톤에 기존 찌꺼기 데이터들이 남아있지 않게 초기 설정해주는 스크립트입니다.

    [SerializeField] private CutoutFade cutoutFade; //Fade용
    
    private bool _isRestart = false;

    private void Start()
    {
        GameManager.Instance.ResetData();
        
    }

    public void Restart()
    {
        _isRestart = true; //재시작 bool을 true로 설정
        StartCoroutine(IFadeOut()); // 55 -> 1 Size
    }
    
    public void GotoSelectStage()
    {
        StartCoroutine(IGotoSelectStage());
    }

    IEnumerator IFadeOut()
    {
        cutoutFade.FadeOut();
        yield return new WaitForSeconds(1.0f);
        
        //만약 재시작 버튼이라면 대기 후, 재시작
        if(_isRestart) SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        
    }
    
    IEnumerator IGotoSelectStage()
    {
        cutoutFade.FadeOut();
        yield return new WaitForSeconds(1.0f);
        
        GameManager.Instance.GoToNextScene();
        // 씬 이동 코드 추가
        SceneManager.LoadScene("StageSelect"); // buildIndex 바로 다음
        
    }
}
