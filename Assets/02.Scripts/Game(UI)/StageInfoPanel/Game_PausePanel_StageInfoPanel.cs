using System.Linq;
using UnityEngine;

/// <summary>
/// 게임 중 일시정지(Pause) 캔버스에서 사용되는 스테이지 정보 패널입니다.
/// 패널이 켜지는 시점의 GameManager 데이터를 읽어와 현재 미션 진행도를 텍스트로 표시합니다.
/// </summary>
public class Game_PausePanel_StageInfoPanel : Base_StageInfoPanel
{
    // 인게임 씬이므로 로비용 노란색 텍스트 하이라이트(완료 처리)를 무시하고 진행도 텍스트 포맷을 사용합니다.
    protected override bool IsInGameScene => true;

    /// <summary>
    /// Base_StageInfoPanel의 ShowInfo를 호출하면 내부적으로 RefreshTexts가 돌면서
    /// 아래 오버라이드한 GetMissionProgressString을 사용해 텍스트를 구성합니다.
    /// </summary>
    public override void ShowInfo(SO_StageData data)
    {
        base.ShowInfo(data);
        
        // 일시정지 창에서는 잠금/클리어 뱃지와 확인 힌트 등은 필요 없으므로 강제로 끕니다.
        if (clearBadge != null) clearBadge.SetActive(false);
        if (lockedBadge != null) lockedBadge.SetActive(false);
    }

    // ─── 현재 진행 중인 미션 수치 계산 ─────────────────────────────────────

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