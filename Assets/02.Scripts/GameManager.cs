using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameManager : Singleton<GameManager>
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
    
    // 선택한 맵 정보
    public SO_StageData currentStageData;
    public StageProgressData currentProgressData; // 현재 진행 데이터 추가
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
    
    public void InitStageData(int ch, int st, SO_StageData stageData)
    {
        chapter = ch;
        stage   = st;
        currentStageData    = stageData;
        currentProgressData = jsonDataManager.GetStageData(ch, st);

        ResetData();
    }

    public void GameClear()
    {
        isCleared = true;

        // 데이터 저장
        SaveStageProgress();
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

        CheckAndSaveMission(currentStageData.firstMissionType, currentStageData,
            ref currentProgressData.isFirstMissionCleared);

        CheckAndSaveMission(currentStageData.secondMissionType, currentStageData,
            ref currentProgressData.isSecondMissionCleared);

        // 3번째 도전과제 — 활성화된 모든 조건이 통과해야 달성
        if (!currentProgressData.isThirdMissionCleared &&
            currentStageData.thirdMissionConditions != ThirdMissionCondition.None)
        {
            bool allPassed = true;
            foreach (var c in GetActiveThirdConditions())
            {
                if (!EvaluateThirdCondition(c)) { allPassed = false; break; }
            }
            if (allPassed) currentProgressData.isThirdMissionCleared = true;
        }

        // 유저 세이브 데이터 업데이트
        if (jsonDataManager != null)
        {
            jsonDataManager.SaveStageData(currentProgressData);
        }
    }
    // ─── 키 사용 가능 여부 ────────────────────────────────────────────
    public bool CanUseKey(KeyType keyType)
    {
        if (currentStageData == null) return false;

        return keyType switch
        {
            KeyType.Alt => currentStageData.canUseLeftALT &&
                           (DevelopmentPanel.IsUnlimitedTab ||
                            currentStageData.limitNumberALT <= 0 ||
                            pushedNumberALT < currentStageData.limitNumberALT),

            KeyType.F4  => currentStageData.canUseF4 &&
                           (DevelopmentPanel.IsUnlimitedTab ||
                            currentStageData.limitNumberF4 <= 0 ||
                            pushedNumberF4 < currentStageData.limitNumberF4),

            KeyType.Tab => currentStageData.canUseTAB &&
                           (DevelopmentPanel.IsUnlimitedTab ||
                            currentStageData.limitNumberTAB <= 0 ||
                            pushedNumberTAB < currentStageData.limitNumberTAB),

            _ => false
        };
    }

    // ─── 3번째 도전과제 헬퍼 (UI에서 서브 row 표시에 사용) ──────────────

    public List<ThirdMissionCondition> GetActiveThirdConditions()
    {
        var result = new List<ThirdMissionCondition>();
        if (currentStageData == null) return result;
        foreach (ThirdMissionCondition c in System.Enum.GetValues(typeof(ThirdMissionCondition)))
        {
            if (c == ThirdMissionCondition.None) continue;
            if ((currentStageData.thirdMissionConditions & c) != 0) result.Add(c);
        }
        return result;
    }

    public bool EvaluateThirdCondition(ThirdMissionCondition condition)
    {
        var data = currentStageData;
        if (data == null) return false;

        switch (condition)
        {
            case ThirdMissionCondition.TimeLimit:
                return currentTime <= data.limitTime;

            case ThirdMissionCondition.MoveCountLimit:
                return pushedNumberALT + pushedNumberF4 + pushedNumberTAB <= data.missionActionCount;

            case ThirdMissionCondition.KillAllEnemies:
                var enemies = FindObjectsByType<EnemyBehaviour>(FindObjectsSortMode.None)
                    .Where(e => e.gameObject.activeSelf).ToArray();
                return enemies.Length > 0 && enemies.All(e => e.IsDead);

            case ThirdMissionCondition.NoSpecificFeature:
                return data.forbiddenFeature switch
                {
                    ForbiddenFeature.ALT => pushedNumberALT <= data.missionFeatureUsageLimit,
                    ForbiddenFeature.F4  => pushedNumberF4  <= data.missionFeatureUsageLimit,
                    ForbiddenFeature.TAB => pushedNumberTAB <= data.missionFeatureUsageLimit,
                    _                    => false,
                };

            default: return false;
        }
    }

    private void CheckAndSaveMission(MissionType type, SO_StageData data, ref bool result)
    {
        if (result) return; 

        switch (type)
        {
            case MissionType.StageClear:
                result = true;
                break;
            
            case MissionType.MoveCountLimit:
                if (pushedNumberALT + pushedNumberF4 + pushedNumberTAB <= data.missionActionCount)
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

            case MissionType.CollectStar:
                var stars = FindObjectsByType<TileBehaviour>(FindObjectsSortMode.None)
                    .Where(t => t.currentTileType == TileType.Star).ToArray();
                if (stars.Length > 0 && stars.All(t => t.IsCollected))
                    result = true;
                break;

            case MissionType.NoSpecificFeature:
                result = data.forbiddenFeature switch
                {
                    ForbiddenFeature.ALT => pushedNumberALT <= data.missionFeatureUsageLimit,
                    ForbiddenFeature.F4  => pushedNumberF4  <= data.missionFeatureUsageLimit,
                    ForbiddenFeature.TAB => pushedNumberTAB <= data.missionFeatureUsageLimit,
                    _                    => false
                };
                break;
        }
    }
    
}
