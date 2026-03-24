using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/*
싱글톤에 기존 찌꺼기 데이터들이 남아있지 않게 초기 설정해주는 스크립트입니다.

이벤트 : OnStageCleared에 NextStage 추가

*/

public class ResetManagement : MonoBehaviour
{   
    [SerializeField] private StageLoader stageLoader; // 맵 생성 담당 스크립트
    [SerializeField] private CutoutFade cutoutFade; //Fade용
    
    [SerializeField] private BehaviourManager behaviourManager;

    [SerializeField] private CanvasGroup changePanelCanvasGroup;
    [SerializeField] private GameObject howToPlayPanel;
    
    private bool _isRestart = false;
    private bool _isProcessing = false; // 초기화 로직이 중복 실행되는 것 방지

    [SerializeField] private float time = 1.5f;

    private IEnumerator WaitForGameManager()
    {
        yield return new WaitUntil(() => GameManager.Instance != null);
        
        cutoutFade.FadeIn(() =>
        {
            GameEvents.RaiseInputLockChanged(false); 
            
        });
        
        GameManager.Instance.isCleared = false;
        GameManager.Instance.ResetData();
        stageLoader.LoadStage(GameManager.Instance.chapter, GameManager.Instance.stage);
    }
    
    private void Start()
    {
        StartCoroutine(WaitForGameManager());
    }
    
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.R) && !_isProcessing && !_isRestart)
        {
            RestartStage();
        }
        
        if (Input.GetKeyDown(KeyCode.M))
        {
            ToggleMapUI();
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            ToggleHowToPlayPanel();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (GameManager.Instance != null && !GameManager.Instance.isOption)
            {
                // Option_UIHandler.HandleOptionToggle(true) 가 반응함
                StartCoroutine(HandleOptionToggle(!GameManager.Instance.isOption));
            }
        }
        
    }
    
    private void OnEnable()
    {
        GameEvents.StageCleared += ChangeStage;
    }
    
    private void OnDisable()
    {
        GameEvents.StageCleared -= ChangeStage;
    }

    public void ToggleHowToPlayPanel()
    {
        howToPlayPanel.SetActive(!howToPlayPanel.activeSelf);
    }
    
    public void ToggleMapUI()
    {
        changePanelCanvasGroup.gameObject.SetActive(!changePanelCanvasGroup.gameObject.activeSelf);
    }

    public void RestartStage()
    {
        if (GameManager.Instance.isCleared) return;
        
        _isRestart = true;
        ChangeStage();
    }

    public void ChangeStage()
    {
        if (_isProcessing) return; //이미 실행 중이면 무시
        _isProcessing = true;
        
        GameEvents.RaiseInputLockChanged(true); // _isInputLocked true로 설정
        
        if (_isRestart) StartCoroutine(IWait(0f));
        else StartCoroutine(IWait(time));

    }

    private IEnumerator IWait(float time)
    {
        yield return new WaitForSeconds(time);
        
        cutoutFade.FadeOut(() =>
        {
            StartCoroutine(IChangeStage());
        });
    }
    
    private IEnumerator IChangeStage()
    {
        if (!_isRestart)
        {
            int nextChapter = GameManager.Instance.chapter;
            int nextStage = GameManager.Instance.stage + 1;

            // 다음 스테이지가 존재하지 않는다면 (마지막 스테이지)
            if (!stageLoader.LoadStage(nextChapter, nextStage))
            {
                nextChapter++;
                nextStage = 1;

                // 다음 챕터도 존재하지 않는다면 (게임 클리어)
                if (!stageLoader.LoadStage(nextChapter, nextStage))
                {
                    // 추후 게임 클리어 관련 업적이나 축하 메세지 로직 추가
                    SceneManager.LoadScene("StageSelect");
                }
            }

            GameManager.Instance.chapter = nextChapter;
            GameManager.Instance.stage = nextStage;
        }
        else
        {
            int chapter = GameManager.Instance.chapter;
            int stage = GameManager.Instance.stage;
            
            stageLoader.LoadStage(chapter, stage);
        }
        
        // 플레이어 초기화
        behaviourManager.Init();
        GameEvents.RaiseInputLockChanged(true);
        
        GameEvents.RaiseStageRestarted(); //FadeText.Reset() 실행됨
        
        cutoutFade.FadeIn(() =>
        {
            // 데이터 리셋
            GameManager.Instance.ResetData();
            GameEvents.RaiseInputLockChanged(false);
            
            _isProcessing = false;
            _isRestart = false;
            
        });
        
        yield return null;

    }

    #region EscEvent
    
    // GameScene 한정 Esc 키를 눌러서, 옵션창을 열게끔 설정하기 
    [SerializeField] private SO_UIEvent optionEvent;
    
    private IEnumerator HandleOptionToggle(bool active)
    {
        // Option_UIHandler의 HandleOptionToggle 함수가 동시에 발동돼서, 한 프레임 뒤에 실행
        yield return new WaitForEndOfFrame();
        optionEvent.Raise(active);
    }
    
    #endregion
    
    
}
