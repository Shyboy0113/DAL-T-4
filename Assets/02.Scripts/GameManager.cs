using System;
using System.Linq;
using UnityEngine;
using Input = UnityEngine.Input;

public class GameManager : MonoBehaviour
{
    
    private void OnEnable()
    {
        GameEvents.StageCleared += GameClear;
        GameEvents.PlayerDied   += HandleGameOver;
    }

    private void OnDisable()
    {
        GameEvents.StageCleared -= GameClear;
        GameEvents.PlayerDied   -= HandleGameOver;
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
    public bool isOption = false;
    public bool isGameOver = false;
    public bool isCleared = false;
    public bool isPaused = false;
    public void ToggleIsPaused() => isPaused = !isPaused;
    
    public bool canUseF4 = true;
    public bool canUseLeftALT = true;
    public bool canUseTAB = false;
    public bool hasSecondMap = false;
    public bool isChatting = false;
    
    // 도전 과제용 데이터
    public float currentTime;
    public int pushedNumberALT;
    public int pushedNumberF4;
    public int pushedNumberTAB;

    
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
        // 게임 상태 초기화
        isGameOver = false;
        isCleared = false;
        isPaused = false;

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

    // JSON 데이터에서 현재 스테이지 데이터 불러오기
    private void LoadStageData(int chap, int stg)
    {
        chapter = chap;
        stage   = stg;

        currentStageData    = _mapDataLoader.GetStageData(chapter, stage);
        currentProgressData = _jsonDataManager.GetStageData(chapter, stage);
    }

    // 스테이지 클리어 시 진행 데이터 저장 (항상 최신 chapter/stage 기준으로 가져옴)
    private void SaveStageProgress()
    {
        currentStageData    = _mapDataLoader.GetStageData(chapter, stage);
        currentProgressData = _jsonDataManager.GetStageData(chapter, stage);

        if (currentProgressData == null || currentStageData == null)
        {
            Debug.LogError($"Stage data not found for {chapter}-{stage}!");
            return;
        }

        currentProgressData.isCleared             = true;
        currentProgressData.isFirstMissionCleared = true;

        CheckAndSaveMission(currentStageData.secondMissionType, currentStageData,
            ref currentProgressData.isSecondMissionCleared);
        CheckAndSaveMission(currentStageData.thirdMissionType, currentStageData,
            ref currentProgressData.isThirdMissionCleared);

        _jsonDataManager.SaveStageData(currentProgressData);
    }

    private void CheckAndSaveMission(MissionType type, StageData data, ref bool result)
    {
        if (result) return; // 이미 달성한 미션은 스킵

        switch (type)
        {
            case MissionType.MoveCountLimit:
                if (data.limitNumberALT >= pushedNumberALT &&
                    data.limitNumberF4  >= pushedNumberF4  &&
                    data.limitNumberTAB >= pushedNumberTAB)
                {
                    result = true;
                    currentProgressData.minALT = Mathf.Min(currentProgressData.minALT, pushedNumberALT);
                    currentProgressData.minF4  = Mathf.Min(currentProgressData.minF4,  pushedNumberF4);
                    currentProgressData.minTAB = Mathf.Min(currentProgressData.minTAB, pushedNumberTAB);
                }
                break;

            case MissionType.TimeLimit:
                if (currentTime <= data.limitTime)
                {
                    result = true;
                    currentProgressData.minClearTime = Mathf.Min(currentProgressData.minClearTime, currentTime);
                }
                break;

            case MissionType.KillAllEnemies:
                var enemies = FindObjectsByType<EnemyBehaviour>(FindObjectsSortMode.None)
                    .Where(e => e.gameObject.activeSelf).ToArray();
                if (enemies.Length > 0 && enemies.All(e => e.IsDead))
                    result = true;
                break;

            case MissionType.CollectAllStars:
                var stars = FindObjectsByType<TileBehaviour>(FindObjectsSortMode.None)
                    .Where(t => t.currentTileType == TileType.Star).ToArray();
                if (stars.Length > 0 && stars.All(t => t.IsCollected))
                    result = true;
                break;

            case MissionType.NoSpecificFeature:
                result = data.forbiddenFeature switch
                {
                    ForbiddenFeature.ALT => pushedNumberALT == 0,
                    ForbiddenFeature.F4  => pushedNumberF4  == 0,
                    ForbiddenFeature.TAB => pushedNumberTAB == 0,
                    _                    => false
                };
                break;
        }
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
