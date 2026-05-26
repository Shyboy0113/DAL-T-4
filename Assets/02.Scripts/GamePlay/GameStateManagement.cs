using System.Collections;
using UnityEngine;
using Eflatun.SceneReference;

public class GameStateManagement : MonoBehaviour
{   
    [Header("References")]
    [SerializeField] private StageLoader stageLoader;
    [SerializeField] private CutoutFade cutoutFade;
    [SerializeField] private SceneReference stageSelectScene;
    [SerializeField] private SceneReference clearScene;
    [SerializeField] private BehaviourManager behaviourManager;

    [Header("UI Panels")]
    [SerializeField] private GameObject howToPlayPanel;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private StageInfoUIController missionPanel;
    [SerializeField] private FadeClearPanel clearPanel;
    
    private bool _isRestart = false;
    private bool _isProcessing = false;
    [SerializeField] private float fadeTime = 1.5f;

    private void Start()
    {
        StartCoroutine(InitialSetup());
    }

    private IEnumerator InitialSetup()
    {
        yield return new WaitUntil(() => GameManager.Instance != null);
        
        // 초기화 시에는 즉시 FadeIn
        cutoutFade.FadeIn(() => GameEvents.RaiseInputLockChanged(false));
        
        GameManager.Instance.ResetData();
        //HideHowToPlayPanel();
        HidePausePanel();
        
        stageLoader.LoadStage(GameManager.Instance.chapter, GameManager.Instance.stage);
        behaviourManager.Init();
    }
    
    void Update()
    {
        if (GameManager.Instance.isCleared)
        {
            if (Input.GetKeyDown(KeyCode.R)) RestartStage();
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (howToPlayPanel.activeSelf)
                {
                    HideHowToPlayPanel();
                }
                else
                {
                    TogglePausePanel();
                }
            }

            // 입력 차단 조건 통합
            if (GameManager.Instance == null || GameManager.Instance.isChatting ||
                GameManager.Instance.isOption || GameManager.Instance.isPaused) return;

            if (Input.GetKeyDown(KeyCode.R)) RestartStage();
            if (Input.GetKeyDown(KeyCode.H)) ToggleHowToPlayPanel();
            if (Input.GetKeyDown(KeyCode.M)) ToggleMissionPanel();
        }
    }

    // --- 이벤트 구독 ---
    private void OnEnable()
    {
        GameEvents.StageCleared += RecordStage; // 클리어 시 로그 데이터 기록
        GameEvents.StageRecordStarted += OnStageEntered; // 스테이지 입장 시 다음 스테이지 해금
        GameEvents.ChatCommandRestart += RestartStage;
        GameEvents.ChatCommandPause += TogglePausePanel;
        GameEvents.ClearSequenceCompleted += SetIsProcessingFalse;
    }

    private void OnDisable()
    {
        GameEvents.StageCleared -= RecordStage;
        GameEvents.StageRecordStarted -= OnStageEntered;
        GameEvents.ChatCommandRestart -= RestartStage;
        GameEvents.ChatCommandPause -= TogglePausePanel;
        GameEvents.ClearSequenceCompleted -= SetIsProcessingFalse;
        
    }
    
    private void SetIsProcessingFalse()
    {
        _isProcessing = false;
    }

    // --- 스테이지 흐름 제어 ---
    
    public void RestartStage()
    {
        if (_isProcessing) return;
        
        _isRestart = true;
        RecordStage();
        ChangeStage();
        
        clearPanel.ResetEffect();
        
        if (missionPanel.gameObject.activeSelf)
        {
            ToggleMissionPanel();
        }
    }

    public void RecordStage()
    {
        _isProcessing = true;
        
        GameEvents.RaiseStageRecordEnded();
        GameEvents.RaiseInputLockChanged(true);
    }

    public void ChangeStage()
    {
        _isProcessing = true;
        
        cutoutFade.FadeOut(() => StartCoroutine(IChangeStage()));
        
        if(missionPanel.gameObject.activeSelf) ToggleMissionPanel();
    }
    
    private IEnumerator IChangeStage()
    {
        // 기존의 GameManager.StageClear()에서 호출했던 부분을 스테이지가 바뀔때만 초기화하게 설정
        GameManager.Instance.ResetData();
        
        if (!_isRestart)
        {
            UpdateNextStageInfo();
        }
        else
        {
            stageLoader.LoadStage(GameManager.Instance.chapter, GameManager.Instance.stage);
        }
        
        behaviourManager.Init();
        GameEvents.RaiseStageRestarted();
        
        cutoutFade.FadeIn(() =>
        {
            HideHowToPlayPanel();
            HidePausePanel();
            GameManager.Instance.ResetData();
            GameEvents.RaiseInputLockChanged(false);
            
            _isProcessing = false;
            _isRestart = false;
        });
        yield break;
    }
    
    // 스테이지 입장 시 다음 스테이지 해금
    private void OnStageEntered(int chapter, int stage)
    {
        var jdm = GameManager.Instance?.jsonDataManager;
        if (jdm == null) return;

        int nextChapter = chapter;
        int nextStage   = stage + 1;

        // 챕터 내 다음 스테이지가 없으면 다음 챕터 1스테이지 시도
        if (!stageLoader.StageExists(nextChapter, nextStage))
        {
            nextChapter++;
            nextStage = 1;
            if (!stageLoader.StageExists(nextChapter, nextStage)) return;
        }

        var nextData = jdm.GetStageData(nextChapter, nextStage);
        if (nextData.isAppeared) return;  // 이미 해금됐으면 스킵

        nextData.isAppeared = true;
        jdm.SaveStageData(nextData);
    }

    private void UpdateNextStageInfo()
    {
        int nextChapter = GameManager.Instance.chapter;
        int nextStage   = GameManager.Instance.stage + 1;

        bool loaded = stageLoader.LoadStage(nextChapter, nextStage);
        if (!loaded)
        {
            nextChapter++;
            nextStage = 1;
            loaded = stageLoader.LoadStage(nextChapter, nextStage);

            if (!loaded)
            {
                StartCoroutine(SceneLoader.LoadScene(clearScene));
                return;
            }
        }

        // ← 해금 코드 제거 (OnStageEntered에서 처리)

        GameManager.Instance.chapter = nextChapter;
        GameManager.Instance.stage   = nextStage;
    }
    
    public void ToggleHowToPlayPanel() => howToPlayPanel.SetActive(!howToPlayPanel.activeSelf);
    
    public void HideHowToPlayPanel() => howToPlayPanel.SetActive(false);
    public void HidePausePanel() { pausePanel.SetActive(false); GameManager.Instance.isPaused = false; }
    
    public void TogglePausePanel()
    {
        if (GameManager.Instance.isOption) return;
        bool newState = !pausePanel.activeSelf;
        pausePanel.SetActive(newState);
        GameManager.Instance.isPaused = newState;
    }
    
    public void ToggleMissionPanel()
    {
        if (!missionPanel.gameObject.activeSelf) missionPanel.gameObject.SetActive(true);
        else missionPanel.HidePanel(() => missionPanel.gameObject.SetActive(false));
    }
    
    public void StageSelectButton()
    {
        GameManager.Instance.isCleared = false;
        cutoutFade.FadeOut(() =>
        {
            StartCoroutine(SceneLoader.LoadScene(stageSelectScene));
        });
    }

}