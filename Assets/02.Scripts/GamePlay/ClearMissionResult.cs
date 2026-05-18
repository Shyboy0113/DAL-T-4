using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using UnityEngine.Localization.Settings;

public class ClearMissionResult : MonoBehaviour, IStageClearEffect
{
    [System.Serializable]
    public struct MissionRowUI
    {
        public GameObject row;
        public TMP_Text   label;
        public Image      clearMark;
    }

    [Header("Stage Info")]
    [SerializeField] private TMP_Text stageNameText;

    [Header("Mission Rows")]
    [SerializeField] private MissionRowUI mission1;
    [SerializeField] private MissionRowUI mission2;
    [SerializeField] private MissionRowUI mission3;

    [Header("Timing")]
    [SerializeField] private float rowInterval    = 0.25f;
    [SerializeField] private float fadeInDuration = 0.30f;

    // ─── IStageClearEffect ──────────────────────────────────────────────────

    public IEnumerator Execute()
    {
        Refresh();

        int activeCount = GetActiveMissionCount();
        float waitTime  = activeCount > 0
            ? rowInterval * (activeCount - 1) + fadeInDuration
            : 0f;

        yield return new WaitForSeconds(waitTime);
    }

    public void ResetEffect()
    {
        if (stageNameText != null) stageNameText.text = string.Empty;

        ResetRow(mission1);
        ResetRow(mission2);
        ResetRow(mission3);
    }

    // ─── Public API ─────────────────────────────────────────────────────────

    public void Refresh()
    {
        var gm       = GameManager.Instance;
        var data     = gm?.currentStageData;
        var progress = gm?.currentProgressData;

        if (data == null || progress == null) return;

        if (stageNameText != null)
            stageNameText.text = data.stageName;

        ApplyRow(mission1, data.firstMissionType,  progress.isFirstMissionCleared,  0, data);
        ApplyRow(mission2, data.secondMissionType, progress.isSecondMissionCleared, 1, data);
        ApplyRow(mission3, data.thirdMissionType,  progress.isThirdMissionCleared,  2, data);
    }

    // ─── Internal ───────────────────────────────────────────────────────────

    private void ApplyRow(MissionRowUI ui, MissionType type, bool cleared, int index, SO_StageData data)
    {
        if (ui.row == null) return;

        bool hasMission = type != MissionType.None;
        ui.row.SetActive(hasMission);
        if (!hasMission) return;

        if (ui.label != null)
            ui.label.text = GetMissionLabel(type, data);

        if (ui.clearMark == null) return;

        ui.clearMark.DOKill();
        ui.clearMark.gameObject.SetActive(cleared);

        if (cleared)
        {
            Color c = ui.clearMark.color;
            c.a = 0f;
            ui.clearMark.color = c;

            DOVirtual.DelayedCall(
                rowInterval * index,
                () => ui.clearMark.DOFade(1f, fadeInDuration)
            );
        }
    }

    private void ResetRow(MissionRowUI ui)
    {
        if (ui.row == null) return;

        ui.row.SetActive(false);

        if (ui.clearMark != null)
        {
            ui.clearMark.DOKill();
            Color c = ui.clearMark.color;
            c.a = 0f;
            ui.clearMark.color = c;
            ui.clearMark.gameObject.SetActive(false);
        }

        if (ui.label != null)
            ui.label.text = string.Empty;
    }

    private int GetActiveMissionCount()
    {
        var data = GameManager.Instance?.currentStageData;
        if (data == null) return 0;

        int count = 0;
        if (data.firstMissionType  != MissionType.None) count++;
        if (data.secondMissionType != MissionType.None) count++;
        if (data.thirdMissionType  != MissionType.None) count++;
        return count;
    }

    private string GetMissionLabel(MissionType type, SO_StageData data) => type switch
    {
        MissionType.StageClear        => L("Mission_StageClear"),
        MissionType.TimeLimit         => GetTimeLimitLabel(data),
        MissionType.MoveCountLimit    => GetMoveCountLabel(data),
        MissionType.KillAllEnemies    => L("Mission_KillAllEnemies"),
        MissionType.CollectStar       => L("Mission_CollectStar"),
        MissionType.NoSpecificFeature => GetNoFeatureLabel(data),
        _                             => ""
    };

    private string GetTimeLimitLabel(SO_StageData data)
    {
        if (data == null || data.limitTime <= 0f) return L("Mission_TimeLimit");
        return string.Format(L("Mission_TimeLimit_Format"), (int)data.limitTime);
    }

    private string GetMoveCountLabel(SO_StageData data)
    {
        if (data == null || data.missionActionCount <= 0) return L("Mission_MoveCountLimit");
        return string.Format(L("Mission_MoveCountLimit_Format"), data.missionActionCount);
    }

    private string GetNoFeatureLabel(SO_StageData data)
    {
        string featureName = data?.forbiddenFeature switch
        {
            ForbiddenFeature.ALT => "ALT",
            ForbiddenFeature.F4  => "F4",
            ForbiddenFeature.TAB => "TAB",
            _                    => null
        };

        if (featureName == null) return L("Mission_NoSpecificFeature");
        return string.Format(L("Mission_NoSpecificFeature_Format"), featureName);
    }

    private string L(string key) =>
        LocalizationSettings.StringDatabase.GetLocalizedString("StageSelect Strings", key);
}