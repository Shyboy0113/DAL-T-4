using UnityEngine;
#if !DISABLESTEAMWORKS
using Steamworks;
#endif

/// <summary>
/// 스테이지 클리어 시 도전과제 달성 여부를 확인하고 Steam 업적을 처리합니다.
/// 상태를 갖지 않으며 GameEvents만 구독합니다 (싱글톤 없음).
///
/// ★ Steam 파트너 대시보드에서 아래 업적 ID들을 등록해야 합니다.
///   ACH_FIRST_CLEAR       - 첫 스테이지 클리어
///   ACH_KILL_ALL_ENEMIES  - 모든 적 퇴치 미션 달성
///   ACH_COLLECT_ALL_STARS - 모든 STAR 수집 미션 달성
///   ACH_MOVE_LIMIT        - 제한 횟수 내 클리어 미션 달성
///   ACH_NO_FEATURE        - 특정 기능 미사용 클리어 미션 달성
///   ACH_TIME_LIMIT        - 제한 시간 내 클리어 미션 달성
/// </summary>
public class AchievementHandler : MonoBehaviour
{
    private void OnEnable()
    {
        GameEvents.StageCleared += OnStageCleared;
        GameEvents.PlayerDied   += OnPlayerDied;
    }

    private void OnDisable()
    {
        GameEvents.StageCleared -= OnStageCleared;
        GameEvents.PlayerDied   -= OnPlayerDied;
    }

    // ─────────────────────────────────────────────────────────────────
    // 이벤트 핸들러
    // ─────────────────────────────────────────────────────────────────

    private void OnStageCleared()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        // 1번 미션: 스테이지 클리어 (항상)
        TryUnlock("ACH_FIRST_CLEAR");

        // 2번 / 3번 미션 달성 여부에 따라 업적 해제
        // GameManager.SaveStageProgress()는 StageCleared 구독 순서상 이 핸들러보다 먼저 실행됩니다
        var progress = gm.currentProgressData;
        if (progress == null) { StoreStats(); return; }

        if (progress.isSecondMissionCleared)
        {
            var stageData = gm.currentStageData;
            if (stageData != null)
                TryUnlock(GetMissionAchievementId(stageData.secondMissionType));
        }

        if (progress.isThirdMissionCleared)
        {
            var stageData = gm.currentStageData;
            if (stageData != null)
                TryUnlock(GetMissionAchievementId(stageData.thirdMissionType));
        }

        StoreStats();
    }

    private void OnPlayerDied()
    {
        // 사망 관련 누적 업적은 AchievementManager에서 처리
        // 여기서는 특정 사망 패턴 업적을 추가할 수 있습니다
    }

    // ─────────────────────────────────────────────────────────────────
    // 미션 타입 → 업적 ID 매핑
    // Steam 파트너 대시보드에 등록한 ID와 일치해야 합니다
    // ─────────────────────────────────────────────────────────────────

    private static string GetMissionAchievementId(MissionType type)
    {
        return type switch
        {
            MissionType.KillAllEnemies    => "ACH_KILL_ALL_ENEMIES",
            MissionType.CollectAllStars   => "ACH_COLLECT_ALL_STARS",
            MissionType.MoveCountLimit    => "ACH_MOVE_LIMIT",
            MissionType.NoSpecificFeature => "ACH_NO_FEATURE",
            MissionType.TimeLimit         => "ACH_TIME_LIMIT",
            _                             => null
        };
    }

    // ─────────────────────────────────────────────────────────────────
    // Steamworks 유틸
    // ─────────────────────────────────────────────────────────────────

    private static void TryUnlock(string achievementId)
    {
        if (string.IsNullOrEmpty(achievementId)) return;
#if !DISABLESTEAMWORKS
        if (!SteamManager.Initialized) return;
        SteamUserStats.SetAchievement(achievementId);
#endif
    }

    private static void StoreStats()
    {
#if !DISABLESTEAMWORKS
        if (!SteamManager.Initialized) return;
        SteamUserStats.StoreStats();
#endif
    }
}
