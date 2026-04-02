using UnityEngine;
#if !DISABLESTEAMWORKS
using Steamworks;
#endif

/// <summary>
/// 세션을 초월한 누적 플레이 데이터를 PlayerPrefs에 기록하고,
/// 임계치 달성 시 Steam 업적을 해제합니다.
/// 싱글톤 없이 GameEvents만 구독합니다.
/// </summary>
public class AchievementManager : MonoBehaviour
{
    // ── PlayerPrefs 키 ────────────────────────────────────────────────
    private const string KeyLifetimeALT  = "ach_lifetime_alt";
    private const string KeyLifetimeF4   = "ach_lifetime_f4";
    private const string KeyLifetimeTAB  = "ach_lifetime_tab";
    private const string KeyTotalDeaths  = "ach_total_deaths";
    private const string KeyTotalClears  = "ach_total_clears";

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

    // ─────────────────────────────────────────────────────────────────
    // 이벤트 핸들러
    // ─────────────────────────────────────────────────────────────────

    private void OnKeyUsed(KeyType keyType)
    {
        switch (keyType)
        {
            case KeyType.Alt:
                int alt = PlayerPrefs.GetInt(KeyLifetimeALT, 0) + 1;
                PlayerPrefs.SetInt(KeyLifetimeALT, alt);
                if (alt >= ThresholdALT) TryUnlock("ACH_SPIN_100");
                break;

            case KeyType.F4:
                int f4 = PlayerPrefs.GetInt(KeyLifetimeF4, 0) + 1;
                PlayerPrefs.SetInt(KeyLifetimeF4, f4);
                break;

            case KeyType.Tab:
                int tab = PlayerPrefs.GetInt(KeyLifetimeTAB, 0) + 1;
                PlayerPrefs.SetInt(KeyLifetimeTAB, tab);
                break;
        }
        PlayerPrefs.Save();
        StoreStats();
    }

    private void OnPlayerDied()
    {
        int deaths = PlayerPrefs.GetInt(KeyTotalDeaths, 0) + 1;
        PlayerPrefs.SetInt(KeyTotalDeaths, deaths);
        PlayerPrefs.Save();

        if (deaths >= ThresholdDeaths) TryUnlock("ACH_DEATH_50");
        StoreStats();
    }

    private void OnStageCleared()
    {
        int clears = PlayerPrefs.GetInt(KeyTotalClears, 0) + 1;
        PlayerPrefs.SetInt(KeyTotalClears, clears);
        PlayerPrefs.Save();
    }

    // ─────────────────────────────────────────────────────────────────
    // 외부 조회용 (UI 디버그 패널 등)
    // ─────────────────────────────────────────────────────────────────

    public int GetLifetimeALT()  => PlayerPrefs.GetInt(KeyLifetimeALT, 0);
    public int GetLifetimeF4()   => PlayerPrefs.GetInt(KeyLifetimeF4,  0);
    public int GetLifetimeTAB()  => PlayerPrefs.GetInt(KeyLifetimeTAB, 0);
    public int GetTotalDeaths()  => PlayerPrefs.GetInt(KeyTotalDeaths, 0);
    public int GetTotalClears()  => PlayerPrefs.GetInt(KeyTotalClears, 0);

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
