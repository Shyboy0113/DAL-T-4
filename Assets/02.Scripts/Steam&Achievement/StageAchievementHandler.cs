using UnityEngine;
#if !DISABLESTEAMWORKS
using Steamworks;
#endif

/// <summary>
/// 스테이지 클리어 시 업적 달성 여부를 확인하고 Steam 업적을 처리합니다.
///
/// ★ Steam 파트너 대시보드에 등록해야 할 업적 ID 형식:
///   ACH_CH{ch}_COMPLETE            → 챕터 전 스테이지 클리어
///   ACH_CH{ch}_PERFECT             → 챕터 전 미션 달성
///   ACH_ALL_CLEAR                  → 전체 스테이지 클리어
///   ACH_ALL_PERFECT                → 전체 미션 달성
/// </summary>

public class StageAchievementHandler : MonoBehaviour
{
    private const int TotalChapters    = 4;
    private const int StagesPerChapter = 15;
    
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
    // 에디터에서 테스트할 챕터를 지정합니다.
    [SerializeField] private int debugChapter = 1;

    private void Start()
    {
        if (!SteamManager.Initialized) return;

        SteamUserStats.ClearAchievement($"ACH_CH{debugChapter}_COMPLETE");
        SteamUserStats.ClearAchievement($"ACH_CH{debugChapter}_PERFECT");
        SteamUserStats.ClearAchievement("ACH_ALL_CLEAR");
        SteamUserStats.ClearAchievement("ACH_ALL_PERFECT");
        SteamUserStats.StoreStats();

        Debug.Log($"[AchievementHandler] 에디터 시작: CH{debugChapter} 업적 초기화 완료");
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

        // ── 챕터 완료 / 퍼펙트 ──
        var dm = gm.jsonDataManager;
        if (dm != null)
        {
            if (dm.IsChapterCleared(ch, StagesPerChapter))
                TryUnlock($"ACH_CH{ch}_COMPLETE");
 
            if (dm.IsChapterPerfect(ch, StagesPerChapter))
                TryUnlock($"ACH_CH{ch}_PERFECT");
 
            // ── 전체 올클리어 / 올퍼펙트 ──
            if (dm.IsAllCleared(TotalChapters, StagesPerChapter))
                TryUnlock("ACH_ALL_CLEAR");
 
            if (dm.IsAllPerfect(TotalChapters, StagesPerChapter))
                TryUnlock("ACH_ALL_PERFECT");
        }
 
        StoreStats();
    }
 
    private void OnPlayerDied()
    {
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
