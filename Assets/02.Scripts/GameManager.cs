using System;
using UnityEngine;
using UnityEngine.Windows;
using Input = UnityEngine.Input;

public class GameManager : MonoBehaviour
{
    private void OnEnable()
    {
        // StackManager가 보내는 방송을 구독합니다.
        GameEvents.StageCleared += GameClear;
        GameEvents.PlayerDied += HandleGameOver;
        
        
    }

    private void OnDisable()
    {
        // 오브젝트가 비활성화될 때 구독을 해제합니다. (메모리 누수 방지)
        GameEvents.StageCleared -= GameClear;
        GameEvents.PlayerDied -= HandleGameOver;
        
    }

    public void HandleGameOver()
    {
        isGameOver = true;
    }

    // 싱글톤 패턴
    public static GameManager Instance { get; private set; }

    [SerializeField]
    private PlayerBehaviour _playerBehaviour;
    [SerializeField]
    private MapDataLoader _mapDataLoader;    
    [SerializeField]
    private JsonDataManager _jsonDataManager;  // ✅ JsonDataManager로 변경
    
    // 선택한 맵 정보
    public StageData currentStageData;
    public StageProgressData currentProgressData; // ✅ 현재 진행 데이터 추가

    public int chapter;
    public int stage;

    // NullReferenceException 방지용 토글
    private bool _ismapDataLoaded = false;
    
    // 게임 상태
    public bool isGameOver = false;
    public bool isCleared = false;

    public bool canUseF4 = true;
    public bool canUseLeftALT = true;
    public bool canUseTAB = false;
    
    // 도전 과제용 데이터
    public float currentTime;
    public int pushedNumberALT;
    public int pushedNumberF4;
    public int pushedNumberTAB;

    // 게임 클리어 패널
    public GameObject clearPanel;
    public GameObject pausePanel;
    private bool _pausePanelActivity;

    public void GetCurrentStageData(MapDataLoader mapDataLoader)
    {
        // 현재 맵 데이터 불러오기
        currentStageData = mapDataLoader.GetStageData(chapter, stage);
    }
    
    void Awake()
    {
        // 싱글톤 구현
        if (Instance == null)
        {
            Instance = this;
            
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);

        // 맵 데이터 로드 확인
        if (_mapDataLoader is null)
        {
            Debug.LogError("Can't find the MapDataLoader!!");
            _ismapDataLoaded = false;
        }
        
        CheckPausePanel();
    }

    private void Start()
    {
        // ✅ JSON에서 현재 스테이지 데이터 불러오기
        LoadStageData(1, 1);

        _pausePanelActivity = false;
    }

    void Update()
    {
        if (isGameOver || isCleared) return;

        currentTime += Time.deltaTime;

        if (!isCleared)
        {
            if (Input.GetKeyDown(KeyCode.Escape) && pausePanel is not null)
            {
                _pausePanelActivity = !_pausePanelActivity;
                pausePanel.SetActive(_pausePanelActivity);
            }
        }
    }

    #region StackManager 외부 등록
    public void RegisterStackManager(PlayerBehaviour playerBehaviour)
    {
        //StackManager 클래스에서 GameManager.Instance에 직접 자기를 등록
        _playerBehaviour = playerBehaviour;
        
    }

    public void UnregisterStackManager()
    {
        _playerBehaviour = null;
    }
    #endregion

    public void CheckPausePanel()
    {
        pausePanel = GameObject.Find("Pause Canvas");
        if(pausePanel) pausePanel.gameObject.SetActive(false);
    }

    public void GameClear()
    {
        isCleared = true;

        // GameManager 초기화 호출
        GoToNextScene();
        
        // ✅ JsonDataManager를 통해 데이터 저장
        SaveStageProgress();
    }

    public void GoToNextScene()
    {
        ResetData();

        // ✅ 다음 스테이지 진행 상태 업데이트
        UnlockNextStage();
    }

    public void ResetData()
    {
        //Scene에 있는 패널을 재연결
        CheckPausePanel();
        
        // 게임 상태 초기화
        isGameOver = false;
        isCleared = false;

        // 도전과제 초기화
        currentTime = 0f;
        pushedNumberALT = 0;
        pushedNumberF4 = 0;
        pushedNumberTAB = 0;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // ✅ JSON 데이터에서 현재 스테이지 데이터 불러오기
    private void LoadStageData(int chap, int stg)
    {
        chapter = chap;
        stage = stg;

        // 현재 맵 데이터 불러오기
        currentStageData = _mapDataLoader.GetStageData(chapter, stage);

        // ✅ JSON 세이브 데이터 불러오기
        currentProgressData = _jsonDataManager.GetStageData(chapter, stage);
    }

    // ✅ JSON에 현재 진행 데이터 저장
    private void SaveStageProgress()
    {
        if (currentProgressData == null)
        {
            Debug.LogError("Stage progress data is null!");
            return;
        }

        // 게임 클리어 및 도전과제 반영
        currentProgressData.isCleared = true;
        currentProgressData.isFirstMissionCleared = true;

        if (currentStageData.limitNumberALT >= pushedNumberALT &&
            currentStageData.limitNumberF4 >= pushedNumberF4 &&
            currentStageData.limitNumberTAB >= pushedNumberTAB)
        {
            currentProgressData.isSecondMissionCleared = true;

            // 기존 값보다 작을 경우 갱신
            currentProgressData.minALT = Mathf.Min(currentProgressData.minALT, pushedNumberALT);
            currentProgressData.minF4 = Mathf.Min(currentProgressData.minF4, pushedNumberF4);
            currentProgressData.minTAB = Mathf.Min(currentProgressData.minTAB, pushedNumberTAB);
        }

        if (currentTime <= currentStageData.limitTime)
        {
            currentProgressData.isThirdMissionCleared = true;
            currentProgressData.minClearTime = Mathf.Min(currentProgressData.minClearTime, currentTime);
        }

        // JSON에 저장
        _jsonDataManager.SaveStageData(currentProgressData);
    }

    // ✅ 다음 스테이지 해금
    private void UnlockNextStage()
    {
        int nextStage = stage + 1;
        StageProgressData nextStageData = _jsonDataManager.GetStageData(chapter, nextStage);

        if (nextStageData != null)
        {
            nextStageData.isAppeared = true;
            _jsonDataManager.SaveStageData(nextStageData);
        }
    }

}
