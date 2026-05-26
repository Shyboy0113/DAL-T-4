using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using UnityEngine.Localization.Settings;

public class ClearMissionResultPanel : MonoBehaviour, IStageClearEffect
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

    [Header("3번째 도전과제 상세 패널")]
    [SerializeField] private GameObject  thirdDetailPanel;
    [SerializeField] private GameObject[] thirdConditionPanel;
    [SerializeField] private TMP_Text[]  thirdConditionTexts; // 최대 4개, 인스펙터에서 순서대로 할당

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

        if (thirdDetailPanel != null) thirdDetailPanel.SetActive(false);
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
        ApplyThirdMissionHeader(mission3, data.thirdMissionConditions, progress.isThirdMissionCleared, 2);
        RefreshThirdDetailPanel(data);
    }

    // ─── Internal ───────────────────────────────────────────────────────────

    private void ApplyRow(MissionRowUI ui, MissionType type, bool cleared, int index, SO_StageData data)
    {
        if (ui.row == null) return;

        bool hasMission = type != MissionType.None;
        ui.row.SetActive(hasMission);
        if (!hasMission) return;

        if (ui.label != null)
            ui.label.text = MissionLabelHelper.GetMissionLabel(type, data);

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
        if (data.firstMissionType        != MissionType.None)           count++;
        if (data.secondMissionType       != MissionType.None)           count++;
        if (data.thirdMissionConditions  != ThirdMissionCondition.None) count++;
        return count;
    }

    // ─── 3번째 도전과제 헤더 row ───────────────────────────────────────────

    private void ApplyThirdMissionHeader(MissionRowUI ui, ThirdMissionCondition conditions, bool cleared, int index)
    {
        if (ui.row == null) return;

        bool hasMission = conditions != ThirdMissionCondition.None;
        ui.row.SetActive(hasMission);
        if (!hasMission) return;

        if (ui.label != null)
            ui.label.text = L("Mission_ThirdMission_Header");

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

    // ─── 3번째 도전과제 상세 패널 ─────────────────────────────────────────

    private void RefreshThirdDetailPanel(SO_StageData data)
    {
        if (thirdDetailPanel == null) return;

        var conditions = MissionLabelHelper.GetConditions(data);
        bool hasAny = conditions.Count > 0;
        thirdDetailPanel.SetActive(hasAny);
        if (!hasAny) return;

        if (thirdConditionPanel == null) return;

        for (int i = 0; i < thirdConditionPanel.Length; i++)
        {
            if (thirdConditionPanel[i] == null) continue;

            bool isActive = i < conditions.Count;
            thirdConditionPanel[i].SetActive(isActive);

            if (isActive && thirdConditionTexts != null && i < thirdConditionTexts.Length)
            {
                if (thirdConditionTexts[i] != null)
                    thirdConditionTexts[i].text = BuildThirdConditionDetailText(conditions[i], data);
            }
        }
    }

    // ─── 3rd 조건 상세 텍스트 (실제 값 + 완료 시 노란색) ────────────────────

    private string BuildThirdConditionDetailText(ThirdMissionCondition condition, SO_StageData data)
    {
        var  gm   = GameManager.Instance;
        bool done = gm?.EvaluateThirdCondition(condition) ?? false;

        string label  = MissionLabelHelper.GetThirdConditionLabel(condition, data);
        string actual = GetThirdConditionActualText(condition, data, gm);
        string full   = label + actual + (done ? L("Mission_Progress_Complete") : "");

        return done ? $"<color=yellow>{full}</color>" : full;
    }

    private string GetThirdConditionActualText(ThirdMissionCondition condition, SO_StageData data, GameManager gm)
    {
        if (gm == null) return string.Empty;
        switch (condition)
        {
            case ThirdMissionCondition.TimeLimit:
                return string.Format(L("Mission_Clear_Actual_Time"), gm.currentTime.ToString("F1"));

            case ThirdMissionCondition.MoveCountLimit:
                int total = gm.pushedNumberALT + gm.pushedNumberF4 + gm.pushedNumberTAB;
                return string.Format(L("Mission_Clear_Actual_Count"), total);

            case ThirdMissionCondition.NoSpecificFeature:
                int pushed = data.forbiddenFeature switch
                {
                    ForbiddenFeature.ALT => gm.pushedNumberALT,
                    ForbiddenFeature.F4  => gm.pushedNumberF4,
                    ForbiddenFeature.TAB => gm.pushedNumberTAB,
                    _                    => -1
                };
                if (pushed < 0) return string.Empty;
                return string.Format(L("Mission_Clear_Actual_Count"), pushed);

            default:
                return string.Empty;
        }
    }

    private string L(string key) =>
        LocalizationSettings.StringDatabase.GetLocalizedString("StageSelect Strings", key);
}