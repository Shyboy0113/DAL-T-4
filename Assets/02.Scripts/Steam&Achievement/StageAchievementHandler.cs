using UnityEngine;
#if !DISABLESTEAMWORKS
using Steamworks;
#endif

/// <summary>
/// 스테이지 클리어 시 도전과제 달성 여부를 확인하고 Steam 업적을 처리합니다.
/// 상태를 갖지 않으며 GameEvents만 구독합니다 (싱글톤 없음).
///
/// ★ Steam 파트너 대시보드에 스테이지별로 아래 형식의 업적 ID를 등록해야 합니다.
///   ACH_CH{챕터}_ST{스테이지}_M{미션번호}
///   예) ACH_CH1_ST1_M1  → 1-1 스테이지 1번 미션
///       ACH_CH1_ST1_M2  → 1-1 스테이지 2번 미션
///       ACH_CH1_ST1_M3  → 1-1 스테이지 3번 미션
///       ACH_CH2_ST3_M2  → 2-3 스테이지 2번 미션
/// </summary>
public class StageAchievementHandler : MonoBehaviour
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

#if UNITY_EDITOR && !DISABLESTEAMWORKS
    // 에디터에서 테스트할 챕터/스테이지 범위를 지정합니다.
    [SerializeField] private int debugChapter = 1;
    [SerializeField] private int debugStageFrom = 1;
    [SerializeField] private int debugStageTo   = 5;

    private void Start()
    {
        if (!SteamManager.Initialized) return;

        for (int st = debugStageFrom; st <= debugStageTo; st++)
        {
            for (int m = 1; m <= 3; m++)
            {
                SteamUserStats.ClearAchievement(GetMissionAchievementId(debugChapter, st, m));
            }
        }
        SteamUserStats.StoreStats();

        Debug.Log($"[AchievementHandler] 에디터 시작: CH{debugChapter} ST{debugStageFrom}~{debugStageTo} 업적 초기화 완료");
    }
#endif

    // ─────────────────────────────────────────────────────────────────
    // 이벤트 핸들러
    // ─────────────────────────────────────────────────────────────────

    private void OnStageCleared()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        var progress  = gm.currentProgressData;
        var stageData = gm.currentStageData;
        if (progress == null || stageData == null) { StoreStats(); return; }

        int ch = stageData.chapterNum;
        int st = stageData.stageNum;

        if (progress.isFirstMissionCleared)
            TryUnlock(GetMissionAchievementId(ch, st, 1));

        if (progress.isSecondMissionCleared)
            TryUnlock(GetMissionAchievementId(ch, st, 2));

        if (progress.isThirdMissionCleared)
            TryUnlock(GetMissionAchievementId(ch, st, 3));

        StoreStats();
    }

    private void OnPlayerDied()
    {
        // 사망 관련 누적 업적은 AchievementManager에서 처리
        // 여기서는 특정 사망 패턴 업적을 추가할 수 있습니다
    }

    // ─────────────────────────────────────────────────────────────────
    // 업적 ID 생성
    // Steam 파트너 대시보드에 등록한 ID와 형식이 일치해야 합니다
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 챕터·스테이지·미션 번호(1~3)로 Steam 업적 ID를 생성합니다.
    /// 예: GetMissionAchievementId(1, 3, 2) → "ACH_CH1_ST3_M2"
    /// </summary>
    public static string GetMissionAchievementId(int chapter, int stage, int missionNumber)
    {
        return $"ACH_CH{chapter}_ST{stage}_M{missionNumber}";
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
