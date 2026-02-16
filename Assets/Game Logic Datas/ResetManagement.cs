using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;

/*
싱글톤에 기존 찌꺼기 데이터들이 남아있지 않게 초기 설정해주는 스크립트입니다.

이벤트 : OnStageCleared에 NextStage 추가

*/

public class ResetManagement : MonoBehaviour
{    
    [SerializeField] private CutoutFade cutoutFade; //Fade용

    // 재시작 버튼을 눌렀을 경우에 bool 값
    private bool _isRestart;
    
    // 스테이지에 할당되는 BGM 할당
    [SerializeField] private AudioClip audioClip; //각 스테이지별로 할당되는 오디오클립(mp3)

    [Header("인스펙터 창에서 직접 입력해야 하는 것")]
    
    // 스테이지 정보
    [SerializeField] private string currentStageName;
    [SerializeField] private string nextStageName;

    // 마지막 스테이지인지에 대한 여부
    public bool isEndStage;
    
    private void OnEnable()
    {
        GameEvents.StageCleared += NextStage;
    }

    private void OnDestroy()
    {
        GameEvents.StageCleared -= NextStage;
    }

    private void Start()
    {
        // bool 값 초기화
        _isRestart = false;
        isEndStage = false;
        
        GameManager.Instance.ResetData();
        GameManager.Instance.isCleared = false;
        
        //만약 SoundManager가 있을 경우, BGM 갱신
        if(SoundManager.Instance is not null) SoundManager.Instance.RenewalBGM(audioClip, currentStageName);
        
    }

    public void Restart()
    {
        // FadeOut 발동
        cutoutFade.FadeOut();
        
        _isRestart = true; //재시작 bool을 true로 설정
        Invoke("ChangeStage",1.0f);
    }

    public void NextStage()
    {
        cutoutFade.ClearFadeOut();
        
        Invoke("ChangeStage",5.0f);
    }
    
    public void ChangeStage()
    {
        // 씬을 로드하기 직전에 모든 DOTween 애니메이션을 깔끔하게 제거합니다.
        DOTween.KillAll(); //이걸 안 하면 Dotween 기존 Scene에서의 Dotween 찌꺼기가 그대로 남아있음
        
        // 만약 재시작 버튼이라면 대기 후, 재시작
        if (_isRestart) SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        // 씬 이동 코드 추가
        else if(isEndStage) SceneManager.LoadScene("StageSelect"); // buildIndex 바로 다음
        else SceneManager.LoadScene(nextStageName);
        
    }
}
