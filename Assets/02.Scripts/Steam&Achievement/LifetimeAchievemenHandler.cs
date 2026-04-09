using UnityEngine;
#if !DISABLESTEAMWORKS
using Steamworks;
#endif

/// <summary>
/// 세션을 초월한 누적 플레이 데이터를 JsonDataManager(GlobalStatsData)에 기록하고,
/// 임계치 달성 시 Steam 업적을 해제합니다.
/// 싱글톤 없이 GameEvents만 구독합니다.
/// </summary>
public class LifetimeAchievemenHandler : MonoBehaviour
{
    private JsonDataManager _jsonDataManager => GameManager.Instance.jsonDataManager;

    // ── 임계치 (Steam 업적 조건) ──────────────────────────────────────
    private const int ThresholdALT    = 100;  // ALT를 누적 100회 → ACH_SPIN_100
    private const int ThresholdDeaths = 50;   // 누적 사망 50회 → ACH_DEATH_50

    private void OnEnable()
    {
        GameEvents.KeyUsed      += OnKeyUsed;
        GameEvents.PlayerDied   += OnPlayerDied;
        GameEvents.StageCleared += OnStageCleared;
    }

    private void OnDisable()
    {
        GameEvents.KeyUsed      -= OnKeyUsed;
        GameEvents.PlayerDied   -= OnPlayerDied;
        GameEvents.StageCleared -= OnStageCleared;
    }
    
    // 이벤트 핸들러
    
    private void OnKeyUsed(KeyType keyType)
    {
        var stats = _jsonDataManager.GetGlobalStats();

        switch (keyType)
        {
            case KeyType.Alt:
                stats.lifetimeALT++;
                if (stats.lifetimeALT >= ThresholdALT) TryUnlock("ACH_SPIN_100");
                break;

            case KeyType.F4:
                stats.lifetimeF4++;
                break;

            case KeyType.Tab:
                stats.lifetimeTAB++;
                break;
        }

        _jsonDataManager.SaveGlobalStats();
        StoreStats();
    }

    private void OnPlayerDied()
    {
        var stats = _jsonDataManager.GetGlobalStats();
        stats.totalDeaths++;
        _jsonDataManager.SaveGlobalStats();

        if (stats.totalDeaths >= ThresholdDeaths) TryUnlock("ACH_DEATH_50");
        StoreStats();
    }

    private void OnStageCleared()
    {
        var stats = _jsonDataManager.GetGlobalStats();
        stats.totalClears++;
        _jsonDataManager.SaveGlobalStats();
    }

    // 외부 조회용 (UI 디버그 패널 등)

    public int GetLifetimeALT()  => _jsonDataManager.GetGlobalStats().lifetimeALT;
    public int GetLifetimeF4()   => _jsonDataManager.GetGlobalStats().lifetimeF4;
    public int GetLifetimeTAB()  => _jsonDataManager.GetGlobalStats().lifetimeTAB;
    public int GetTotalDeaths()  => _jsonDataManager.GetGlobalStats().totalDeaths;
    public int GetTotalClears()  => _jsonDataManager.GetGlobalStats().totalClears;
    
    
    #region Steamworks Utility
    
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
    #endregion

}
