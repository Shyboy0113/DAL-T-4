using System.Linq;
using UnityEngine;

/// <summary>
/// 게임 진행 중 (M키) 나타나는 실시간 미션 정보 패널입니다.
/// Update문과 키 이벤트를 통해 플레이어의 행동 수치를 실시간으로 갱신합니다.
/// </summary>
public class Game_MissionPanel_StageInfoPanel : Base_StageInfoPanel
{
    // 인게임 진행 상태를 보여주어야 하므로 true로 설정
    protected override bool IsInGameScene => true;
    private float _refreshTimer;

    protected override void OnEnable()
    {
        base.OnEnable();
        GameEvents.KeyUsed += OnKeyUsedRefresh;
        GameEvents.UndoTriggered += OnUndoRefresh;
        
        // 패널이 켜질 때 현재 스테이지 데이터로 초기 갱신
        if (GameManager.Instance != null)
            ShowInfo(GameManager.Instance.currentStageData);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        GameEvents.KeyUsed -= OnKeyUsedRefresh;
        GameEvents.UndoTriggered -= OnUndoRefresh;
    }

    private void Update()
    {
        if (_currentData == null || !gameObject.activeSelf) return;
        
        // 0.1초 주기로 실시간 타이머 및 수치 갱신
        _refreshTimer += Time.deltaTime;
        if (_refreshTimer < 0.1f) return;
        _refreshTimer = 0f;
        
        RefreshTexts(_currentData);
    }

    private void OnKeyUsedRefresh(KeyType _) { if (_currentData != null) RefreshTexts(_currentData); }
    private void OnUndoRefresh()             { if (_currentData != null) RefreshTexts(_currentData); }

    public override void ShowInfo(SO_StageData data)
    {
        base.ShowInfo(data);
        if (clearBadge != null) clearBadge.SetActive(false);
        if (lockedBadge != null) lockedBadge.SetActive(false);
    }

    // ─── 실시간 미션 수치 계산 (PausePanel과 동일 로직) ──────────────────────

    protected override string GetMissionProgressString(MissionType type, SO_StageData data)
    {
        if (GameManager.Instance == null) return string.Empty;
        var gm = GameManager.Instance;

        switch (type)
        {
            case MissionType.TimeLimit:
            {
                float cur  = gm.currentTime;
                bool  done = cur <= data.limitTime;
                return string.Format(L("Mission_Progress_Time_Format"), cur.ToString("F1"), (int)data.limitTime)
                       + (done ? L("Mission_Progress_Complete") : "");
            }
            case MissionType.MoveCountLimit:
            {
                int  total = gm.pushedNumberALT + gm.pushedNumberF4 + gm.pushedNumberTAB;
                bool done  = total <= data.missionActionCount;
                return string.Format(L("Mission_Progress_Count_Format"), total, data.missionActionCount)
                       + (done ? L("Mission_Progress_Complete") : "");
            }
            case MissionType.NoSpecificFeature:
            {
                int pushed = data.forbiddenFeature switch
                {
                    ForbiddenFeature.ALT => gm.pushedNumberALT,
                    ForbiddenFeature.F4  => gm.pushedNumberF4,
                    ForbiddenFeature.TAB => gm.pushedNumberTAB,
                    _                    => -1
                };
                if (pushed < 0) return string.Empty;
                bool   done = pushed <= data.missionFeatureUsageLimit;
                string prog = data.missionFeatureUsageLimit <= 0
                    ? string.Format(L("Mission_Progress_FeatureUsed_Format"), pushed)
                    : string.Format(L("Mission_Progress_Count_Format"), pushed, data.missionFeatureUsageLimit);
                return prog + (done ? L("Mission_Progress_Complete") : "");
            }
            case MissionType.KillAllEnemies:
            {
                var  enemies = FindObjectsByType<EnemyBehaviour>(FindObjectsSortMode.None)
                                   .Where(e => e.gameObject.activeSelf).ToArray();
                if (enemies.Length == 0) return string.Empty;
                int  dead = enemies.Count(e => e.IsDead);
                bool done = dead == enemies.Length;
                return string.Format(L("Mission_Progress_Count_Format"), dead, enemies.Length)
                       + (done ? L("Mission_Progress_Complete") : "");
            }
            case MissionType.CollectStar:
            {
                var  stars = FindObjectsByType<TileBehaviour>(FindObjectsSortMode.None)
                                 .Where(t => t.currentTileType == TileType.Star).ToArray();
                if (stars.Length == 0) return string.Empty;
                int  collected = stars.Count(t => t.IsCollected);
                bool done      = collected == stars.Length;
                return string.Format(L("Mission_Progress_Count_Format"), collected, stars.Length)
                       + (done ? L("Mission_Progress_Complete") : "");
            }
            default:
                return string.Empty;
        }
    }

    protected override string GetThirdConditionProgressString(ThirdMissionCondition condition, SO_StageData data)
    {
        if (GameManager.Instance == null) return string.Empty;
        var gm = GameManager.Instance;

        switch (condition)
        {
            case ThirdMissionCondition.TimeLimit:
            {
                float cur  = gm.currentTime;
                bool  done = cur <= data.limitTime;
                return string.Format(L("Mission_Progress_Time_Format"), cur.ToString("F1"), (int)data.limitTime)
                       + (done ? L("Mission_Progress_Complete") : "");
            }
            case ThirdMissionCondition.MoveCountLimit:
            {
                int  total = gm.pushedNumberALT + gm.pushedNumberF4 + gm.pushedNumberTAB;
                bool done  = total <= data.missionActionCount;
                return string.Format(L("Mission_Progress_Count_Format"), total, data.missionActionCount)
                       + (done ? L("Mission_Progress_Complete") : "");
            }
            case ThirdMissionCondition.NoSpecificFeature:
            {
                int pushed = data.forbiddenFeature switch
                {
                    ForbiddenFeature.ALT => gm.pushedNumberALT,
                    ForbiddenFeature.F4  => gm.pushedNumberF4,
                    ForbiddenFeature.TAB => gm.pushedNumberTAB,
                    _                    => -1
                };
                if (pushed < 0) return string.Empty;
                bool   done = pushed <= data.missionFeatureUsageLimit;
                string prog = data.missionFeatureUsageLimit <= 0
                    ? string.Format(L("Mission_Progress_FeatureUsed_Format"), pushed)
                    : string.Format(L("Mission_Progress_Count_Format"), pushed, data.missionFeatureUsageLimit);
                return prog + (done ? L("Mission_Progress_Complete") : "");
            }
            case ThirdMissionCondition.KillAllEnemies:
            {
                var  enemies = FindObjectsByType<EnemyBehaviour>(FindObjectsSortMode.None)
                                   .Where(e => e.gameObject.activeSelf).ToArray();
                if (enemies.Length == 0) return string.Empty;
                int  dead = enemies.Count(e => e.IsDead);
                bool done = dead == enemies.Length;
                return string.Format(L("Mission_Progress_Count_Format"), dead, enemies.Length)
                       + (done ? L("Mission_Progress_Complete") : "");
            }
            default:
                return string.Empty;
        }
    }
}