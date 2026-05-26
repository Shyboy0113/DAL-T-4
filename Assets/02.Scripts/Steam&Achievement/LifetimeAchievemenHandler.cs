using UnityEngine;
#if !DISABLESTEAMWORKS
using Steamworks;
#endif

public class LifetimeAchievemenHandler : MonoBehaviour
{
    private JsonDataManager _jsonDataManager => GameManager.Instance.jsonDataManager;

    // ── 누적 업적 임계치 ──────────────────────────────────────────────

    // ACH_SPIN_100  : 피겨스케이트 유망주
    // ACH_SPIN_300  : 반고리관 괜찮아요?
    // ACH_SPIN_500  : 저는 멀미라는 걸 느껴본 적이 없어요
    private static readonly (int threshold, string id)[] AltMilestones =
    {
        (100, "ACH_SPIN_100"),
        (300, "ACH_SPIN_300"),
        (500, "ACH_SPIN_500")
    };

    // ACH_F4_100    : 단세포 마라토너
    // ACH_F4_500    : 앞만 보고 달린다
    // ACH_F4_1000   : 전진, 전진! 그리고 또 전진!
    private static readonly (int threshold, string id)[] F4Milestones =
    {
        (100,  "ACH_F4_100"),
        (500,  "ACH_F4_500"),
        (1000, "ACH_F4_1000")
    };

    // ACH_TAB_1     : TAB이 왜 회전이죠?
    // ACH_TAB_10    : 나 혼자만 역회전
    // ACH_TAB_50    : 엄마가 운전대 잡지 마래요
    // ACH_TAB_100   : 코끼리코 역주행 마스터
    private static readonly (int threshold, string id)[] TabMilestones =
    {
        (1,   "ACH_TAB_1"),
        (10,  "ACH_TAB_10"),
        (50,  "ACH_TAB_50"),
        (100, "ACH_TAB_100")
    };

    // ACH_ALTTAB_1   : 이런 게 있었네
    // ACH_ALTTAB_10  : 있었는데요 아뇨 없어요
    // ACH_ALTTAB_50  : 블랙홀이 필요 없어
    // ACH_ALTTAB_100 : 차원 여행자
    private static readonly (int threshold, string id)[] AltTabMilestones =
    {
        (1,   "ACH_ALTTAB_1"),
        (10,  "ACH_ALTTAB_10"),
        (50,  "ACH_ALTTAB_50"),
        (100, "ACH_ALTTAB_100")
    };

    // ACH_DEATH_1   : 끄아아악
    // ACH_DEATH_10  : 죽다 살아났네
    // ACH_DEATH_50  : 죽음이 두렵지 않은 자
    // ACH_DEATH_100 : 죽었어? 딸깍
    // ACH_DEATH_200 : 이세계 사망회귀 능력자
    // ACH_DEATH_300 : 죽음을 초월한 자
    private static readonly (int threshold, string id)[] DeathMilestones =
    {
        (1,   "ACH_DEATH_1"),
        (10,  "ACH_DEATH_10"),
        (50,  "ACH_DEATH_50"),
        (100, "ACH_DEATH_100"),
        (200, "ACH_DEATH_200"),
        (300, "ACH_DEATH_300")
    };

    private void OnEnable()
    {
        GameEvents.KeyUsed      += OnKeyUsed;
        GameEvents.PlayerDied   += OnPlayerDied;
        GameEvents.MapSwitched  += OnMapSwitched;
        GameEvents.EnemyDied    += OnEnemyDied;
    }

    private void OnDisable()
    {
        GameEvents.KeyUsed      -= OnKeyUsed;
        GameEvents.PlayerDied   -= OnPlayerDied;
        GameEvents.MapSwitched  -= OnMapSwitched;
        GameEvents.EnemyDied    -= OnEnemyDied;
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

    private void OnMapSwitched()
    {
        var stats = _jsonDataManager.GetGlobalStats();
        stats.lifetimeAltTab++;
        _jsonDataManager.SaveGlobalStats();

        CheckMilestones(AltTabMilestones, stats.lifetimeAltTab);
        StoreStats();
    }

    private void OnEnemyDied()
    {
        var stats = _jsonDataManager.GetGlobalStats();
        stats.totalEnemyKills++;
        _jsonDataManager.SaveGlobalStats();
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

    public int GetLifetimeALT()      => _jsonDataManager.GetGlobalStats().lifetimeALT;
    public int GetLifetimeF4()       => _jsonDataManager.GetGlobalStats().lifetimeF4;
    public int GetLifetimeTAB()      => _jsonDataManager.GetGlobalStats().lifetimeTAB;
    public int GetLifetimeAltTab()   => _jsonDataManager.GetGlobalStats().lifetimeAltTab;
    public int GetTotalDeaths()      => _jsonDataManager.GetGlobalStats().totalDeaths;
    public int GetTotalEnemyKills()  => _jsonDataManager.GetGlobalStats().totalEnemyKills;

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
