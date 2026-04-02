using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    #region Singleton

    public static GameManager Instance { get; private set; }
    
    void Awake()
    {
        // 싱글톤 구현
        if (Instance == null)
        {
            Instance = this;
            
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    #endregion
    
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
    
    // 선택한 맵 정보
    public SO_StageData currentStageData;
    public StageProgressData currentProgressData; // ✅ 현재 진행 데이터 추가
    public JsonDataManager jsonDataManager;
    
    public int chapter;
    public int stage;

    // 게임 상태
    public bool isOption = false;
    public bool isGameOver = false;
    public bool isCleared = false;
    public bool isPaused = false;
    
    public bool isChatting = false;
    
    // 도전 과제용 데이터
    public float currentTime;
    public int pushedNumberALT;
    public int pushedNumberF4;
    public int pushedNumberTAB;
    
    void Update()
    {
        if (isGameOver || isCleared) return;

        currentTime += Time.deltaTime;
    }

    public void GameClear()
    {
        isCleared = true;

        // GameManager 초기화 호출
        GoToNextScene();
        
        SaveStageProgress();
    }

    public void GoToNextScene()
    {
        ResetData();
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

    // 스테이지 클리어 시 진행 데이터 저장 (항상 최신 chapter/stage 기준으로 가져옴)
    private void SaveStageProgress()
    {
        if (currentProgressData == null || currentStageData == null)
        {
            Debug.LogError($"GameManager: {chapter}-{stage}의 데이터가 비어있어 저장할 수 없습니다!");
            return;
        }

        currentProgressData.isCleared             = true;
        currentProgressData.isFirstMissionCleared = true;

        CheckAndSaveMission(currentStageData.firstMissionType, currentStageData,
            ref currentProgressData.isFirstMissionCleared);
        
        CheckAndSaveMission(currentStageData.secondMissionType, currentStageData,
            ref currentProgressData.isSecondMissionCleared);
        
        CheckAndSaveMission(currentStageData.thirdMissionType, currentStageData,
            ref currentProgressData.isThirdMissionCleared);

        // 유저 세이브 데이터 업데이트
        if (jsonDataManager != null)
        {
            jsonDataManager.SaveStageData(currentProgressData);
        }
    }

    private void CheckAndSaveMission(MissionType type, SO_StageData data, ref bool result)
    {
        if (result) return; 

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
    
    private void UnlockNextStage()
    {
        if (jsonDataManager == null) return;

        int nextStage = stage + 1;
        StageProgressData nextStageData = jsonDataManager.GetStageData(chapter, nextStage);

        if (nextStageData != null)
        {
            nextStageData.isAppeared = true;
            jsonDataManager.SaveStageData(nextStageData);
        }
    }

}
