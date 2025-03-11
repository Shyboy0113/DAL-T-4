using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // 싱글톤 패턴
    public static GameManager Instance { get; private set; }

    [SerializeField]
    private StackManager _stackManager;
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
    [SerializeField]
    private bool _isStackManagerLoaded = false;

    // 게임 상태
    public bool isGameOver = false;
    public bool isCleared = false;

    // 도전 과제용 데이터
    public float currentTime;
    public int pushedNumberALT;
    public int pushedNumberF4;
    public int pushedNumberTAB;

    // 게임 클리어 패널
    public GameObject clearPanel;

    void Awake()
    {
        // 싱글톤 구현
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // JsonDataManager 가져오기
        _jsonDataManager = FindObjectOfType<JsonDataManager>();

        // 맵 데이터 로드 확인
        if (_mapDataLoader is null)
        {
            Debug.LogError("Can't find the MapDataLoader!!");
            _ismapDataLoaded = false;
        }
    }

    private void Start()
    {
        // ✅ JSON에서 현재 스테이지 데이터 불러오기
        LoadStageData(1, 1);
    }

    void Update()
    {
        if (isGameOver || isCleared) return;

        currentTime += Time.deltaTime;

        if (_isStackManagerLoaded)
        {
            if (Input.GetKeyDown(KeyCode.LeftAlt) && currentStageData.canUseF4)
            {
                _stackManager.ProcessAltInput();
                pushedNumberALT += 1;
            }

            if (Input.GetKeyDown(KeyCode.F4) && currentStageData.canUseF4)
            {
                _stackManager.ProcessF4Input();
                pushedNumberF4 += 1;
            }

            if (Input.GetKeyDown(KeyCode.Tab) && currentStageData.canUseTab)
            {
                _stackManager.ProcessTabInput();
                pushedNumberTAB += 1;
            }
        }
    }

    public void GameClear()
    {
        isCleared = true;

        // ✅ JsonDataManager를 통해 데이터 저장
        SaveStageProgress();

        clearPanel.SetActive(true);
    }

    public void GoToNextScene()
    {
        ResetData();

        // ✅ 다음 스테이지 진행 상태 업데이트
        UnlockNextStage();

        // 씬 이동 코드 추가
    }

    public void ResetData()
    {
        // 스택 상태 초기화
        DisconnectStackManager();

        // 클리어 패널 숨기기
        clearPanel.SetActive(false);

        // 게임 상태 초기화
        isGameOver = false;
        isCleared = false;

        // 도전과제 초기화
        currentTime = 0f;
        pushedNumberALT = 0;
        pushedNumberF4 = 0;
        pushedNumberTAB = 0;
    }

    public void TileOut()
    {
        if (_isStackManagerLoaded)
        {
            _stackManager.PlayExplosion();
        }
    }

    public void ConnectStackManager()
    {
        _stackManager = FindObjectOfType<StackManager>();

        if (_stackManager is null)
        {
            Debug.LogError("Failed To Connect StackManager!");
        }
        else
        {
            Debug.Log("Succeed To Connect StackManager!");
            _isStackManagerLoaded = true;
        }
    }

    public void DisconnectStackManager()
    {
        _stackManager = null;
        _isStackManagerLoaded = false;
    }

    private void OnDestroy()
    {
        Instance = null;
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
