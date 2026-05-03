using System.Collections;
using UnityEngine;
using Eflatun.SceneReference;

/*
싱글톤에 기존 찌꺼기 데이터들이 남아있지 않게 초기 설정해주는 스크립트입니다.

이벤트 : OnStageCleared에 NextStage 추가

*/

public class GameStateManagement : MonoBehaviour
{   
    [SerializeField] private StageLoader stageLoader; // 맵 생성 담당 스크립트
    [SerializeField] private CutoutFade cutoutFade; //Fade용
    
    [SerializeField] private SceneReference stageSelectScene;
    
    [SerializeField] private BehaviourManager behaviourManager;

    [SerializeField] private CanvasGroup mapPanel;
    [SerializeField] private GameObject howToPlayPanel;
    [SerializeField] private GameObject pausePanel;
    
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
        
        // UI 패널들 초기화
        HideHowToPlayPanel();
        HideMapUI();
        HidePausePanel();
        
        stageLoader.LoadStage(GameManager.Instance.chapter, GameManager.Instance.stage);
        behaviourManager.Init();
    }
    
    private void Start()
    {
        StartCoroutine(WaitForGameManager());
    }
    
    void Update()
    {
        if (GameManager.Instance is null || GameManager.Instance.isChatting) return;
        
        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartStage();
        }
        
        if (Input.GetKeyDown(KeyCode.M))
        {
            ToggleMapUI();
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            ToggleHowToPlayPanel();
        }
        
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (GameManager.Instance.isOption) return;
            
            TogglePausePanel();
            
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

    public void ToggleMapUI()
    {
        bool newState = !mapPanel.gameObject.activeSelf;
        mapPanel.gameObject.SetActive(newState);
    }
    
    public void ToggleHowToPlayPanel()
    {
        howToPlayPanel.SetActive(!howToPlayPanel.activeSelf);
    }

    public void TogglePausePanel()
    {
        bool newState = !pausePanel.activeSelf;
        pausePanel.SetActive(newState);
        GameManager.Instance.isPaused = newState;
    }
    
    public void HideHowToPlayPanel()
    {
        howToPlayPanel.SetActive(false);
    }
    
    public void HideMapUI()
    {
        mapPanel.gameObject.SetActive(false);
    }

    public void HidePausePanel()
    {
        pausePanel.SetActive(false);
        GameManager.Instance.isPaused = false;
    }

    public void RestartStage()
    {
        if (_isProcessing || _isRestart) return;
        
        if (GameManager.Instance.isCleared) return;
        
        _isRestart = true;
        ChangeStage();
    }

    public void ChangeStage()
    {
        if (_isProcessing) return; //이미 실행 중이면 무시
        _isProcessing = true;

        // 현재 세션 종료 기록 (클리어 또는 리스타트 모두 시도 횟수로 카운트)
        GameEvents.RaiseStageRecordEnded();

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

            // 다음 스테이지가 존재하지 않는다면 (챕터 마지막 스테이지)
            bool loaded = stageLoader.LoadStage(nextChapter, nextStage);
            if (!loaded)
            {
                nextChapter++;
                nextStage = 1;
                loaded = stageLoader.LoadStage(nextChapter, nextStage);

                // 추후 게임 클리어 관련 업적이나 축하 메세지 로직 추가
                if (!loaded)
                {
                    StartCoroutine(SceneLoader.LoadScene(stageSelectScene));
                }
            }

            // LoadStage 내부에서 InitStageData가 호출된 뒤 isAppeared를 true로 설정 및 저장
            // (챕터 경계를 포함해 정확한 nextChapter/nextStage가 결정된 이후에 실행)
            if (loaded)
            {
                var jdm = GameManager.Instance.jsonDataManager;
                if (jdm != null)
                {
                    var nextData = jdm.GetStageData(nextChapter, nextStage);
                    nextData.isAppeared = true;
                    jdm.SaveStageData(nextData);
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
            // UI 패널들 초기화
            HideHowToPlayPanel();
            HideMapUI();
            HidePausePanel();
            
            // 데이터 리셋
            GameManager.Instance.ResetData();
            GameEvents.RaiseInputLockChanged(false);
            
            _isProcessing = false;
            _isRestart = false;
            
        });
        
        yield return null;

    }
    
}
