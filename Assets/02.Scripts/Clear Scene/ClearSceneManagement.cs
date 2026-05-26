using UnityEngine;
using Eflatun.SceneReference;
using TMPro;
using UnityEngine.Localization.Settings;

public class ClearSceneManagement : MonoBehaviour
{
    public CutoutFade cutoutFade;

    [SerializeField] private SceneReference clearScene;
    [SerializeField] private SceneReference introScene;

    [Header("Stats UI")]
    [SerializeField] private TMP_Text statStageClearText;
    [SerializeField] private TMP_Text statStarsText;
    [SerializeField] private TMP_Text statEnemyKillsText;
    [SerializeField] private TMP_Text statTotalActionsText;
    [SerializeField] private TMP_Text statALTText;
    [SerializeField] private TMP_Text statF4Text;
    [SerializeField] private TMP_Text statTABText;
    [SerializeField] private TMP_Text statPlayTimeText;
    [SerializeField] private TMP_Text statDeathsText;

    private const int TotalChapters      = 4;
    private const int StagesPerChapter   = 15;

    void Start()
    {
        if (cutoutFade != null)
            cutoutFade.FadeIn();

        if (SoundManager.Instance != null)
            SoundManager.Instance.RenewalBGMForSCene(clearScene);

        RefreshStats();
    }

    public void GotoIntroScene()
    {
        cutoutFade.FadeOut(() =>
        {
            StartCoroutine(SceneLoader.LoadScene(introScene));
        });
        
    }

    private void RefreshStats()
    {
        if (GameManager.Instance == null || GameManager.Instance.jsonDataManager == null) return;

        var db    = GameManager.Instance.jsonDataManager;
        var stats = db.GetGlobalStats();

        int cleared = db.GetClearedStageCount();
        int total   = TotalChapters * StagesPerChapter;
        db.GetStarStats(out int starsCollected, out int starsMax, TotalChapters, StagesPerChapter);
        int totalActions = stats.lifetimeALT + stats.lifetimeF4 + stats.lifetimeTAB;

        SetText(statStageClearText,    "ClearStats_StageClear",   $"{cleared}/{total}");
        SetText(statStarsText,         "ClearStats_Stars",        $"{starsCollected}/{starsMax}");
        SetText(statEnemyKillsText,    "ClearStats_EnemyKills",   $"{stats.totalEnemyKills}");
        SetText(statTotalActionsText,  "ClearStats_TotalActions", $"{totalActions}");
        SetText(statALTText,           "ClearStats_ALT",          $"{stats.lifetimeALT}");
        SetText(statF4Text,            "ClearStats_F4",           $"{stats.lifetimeF4}");
        SetText(statTABText,           "ClearStats_TAB",          $"{stats.lifetimeTAB}");
        SetText(statPlayTimeText,      "ClearStats_PlayTime",     FormatTime(db.GetTotalPlayTime()));
        SetText(statDeathsText,        "ClearStats_Deaths",       $"{stats.totalDeaths}");
    }

    private void SetText(TMP_Text target, string locKey, string value)
    {
        if (target == null) return;
        target.text = $"{L(locKey)}: {value}";
    }

    private static string FormatTime(float seconds)
    {
        int h = (int)(seconds / 3600);
        int m = (int)(seconds % 3600 / 60);
        int s = (int)(seconds % 60);
        return h > 0 ? $"{h}:{m:D2}:{s:D2}" : $"{m}:{s:D2}";
    }

    private string L(string key) =>
        LocalizationSettings.StringDatabase.GetLocalizedString("Game Menu Strings", key);
}
