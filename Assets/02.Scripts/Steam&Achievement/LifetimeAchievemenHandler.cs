using UnityEngine;
#if !DISABLESTEAMWORKS
using Steamworks;
#endif

public class LifetimeAchievemenHandler : MonoBehaviour
{
    private JsonDataManager _jsonDataManager => GameManager.Instance.jsonDataManager;

    // ── 누적 업적 임계치 ──────────────────────────────────────────────
    private static readonly (int threshold, string id)[] AltMilestones =
    {
        (100,  "ACH_SPIN_100"),
        (500,  "ACH_SPIN_500"),
        (1000, "ACH_SPIN_1000")
    };

    private static readonly (int threshold, string id)[] F4Milestones =
    {
        (100, "ACH_F4_100"),
        (500, "ACH_F4_500")
    };

    private static readonly (int threshold, string id)[] TabMilestones =
    {
        (100, "ACH_TAB_100"),
        (500, "ACH_TAB_500")
    };

    private static readonly (int threshold, string id)[] DeathMilestones =
    {
        (50,  "ACH_DEATH_50"),
        (100, "ACH_DEATH_100"),
        (500, "ACH_DEATH_500")
    };

    private static readonly (int threshold, string id)[] ClearMilestones =
    {
        (10, "ACH_CLEAR_10"),
        (50, "ACH_CLEAR_50")
    };

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

    // ─────────────────────────────────────────────────────────────────
    // 이벤트 핸들러
    // ─────────────────────────────────────────────────────────────────

    private void OnKeyUsed(KeyType keyType)
    {
        var stats = _jsonDataManager.GetGlobalStats();

        switch (keyType)
        {
            case KeyType.Alt:
                stats.lifetimeALT++;
                CheckMilestones(AltMilestones, stats.lifetimeALT);
                break;

            case KeyType.F4:
                stats.lifetimeF4++;
                CheckMilestones(F4Milestones, stats.lifetimeF4);
                break;

            case KeyType.Tab:
                stats.lifetimeTAB++;
                CheckMilestones(TabMilestones, stats.lifetimeTAB);
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

        CheckMilestones(DeathMilestones, stats.totalDeaths);
        StoreStats();
    }

    private void OnStageCleared()
    {
        var stats = _jsonDataManager.GetGlobalStats();
        stats.totalClears++;
        _jsonDataManager.SaveGlobalStats();

        CheckMilestones(ClearMilestones, stats.totalClears);
        StoreStats();
    }

    // ─────────────────────────────────────────────────────────────────
    // 임계치 일괄 판정
    // ─────────────────────────────────────────────────────────────────

    private static void CheckMilestones((int threshold, string id)[] milestones, int currentValue)
    {
        foreach (var (threshold, id) in milestones)
        {
            if (currentValue >= threshold)
                TryUnlock(id);
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // 외부 조회용
    // ─────────────────────────────────────────────────────────────────

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