using System.Collections.Generic;
using UnityEngine.Localization.Settings;

/// <summary>
/// 미션 라벨 텍스트 생성 공통 로직.
/// StageInfoPanel, ClearMissionResult 등 여러 UI 패널이 공유합니다.
/// </summary>
public static class MissionLabelHelper
{
    private static string L(string key) =>
        LocalizationSettings.StringDatabase.GetLocalizedString("StageSelect Strings", key);

    // ─── 기본 미션 라벨 ──────────────────────────────────────────────────────

    public static string GetMissionLabel(MissionType type, SO_StageData data) => type switch
    {
        MissionType.StageClear        => L("Mission_StageClear"),
        MissionType.TimeLimit         => GetTimeLimitLabel(data),
        MissionType.MoveCountLimit    => GetMoveCountLabel(data),
        MissionType.KillAllEnemies    => L("Mission_KillAllEnemies"),
        MissionType.CollectStar       => L("Mission_CollectStar"),
        MissionType.NoSpecificFeature => GetNoFeatureLabel(data),
        _                             => ""
    };

    // ─── 3번째 도전과제 조건 라벨 ─────────────────────────────────────────────

    public static string GetThirdConditionLabel(ThirdMissionCondition condition, SO_StageData data) => condition switch
    {
        ThirdMissionCondition.TimeLimit         => GetTimeLimitLabel(data),
        ThirdMissionCondition.MoveCountLimit    => GetMoveCountLabel(data),
        ThirdMissionCondition.KillAllEnemies    => L("Mission_KillAllEnemies"),
        ThirdMissionCondition.NoSpecificFeature => GetNoFeatureLabel(data),
        _                                       => string.Empty,
    };

    // ─── 플래그 열거 ─────────────────────────────────────────────────────────

    public static List<ThirdMissionCondition> GetConditions(SO_StageData data)
    {
        var result = new List<ThirdMissionCondition>();
        if (data == null) return result;
        foreach (ThirdMissionCondition c in System.Enum.GetValues(typeof(ThirdMissionCondition)))
        {
            if (c == ThirdMissionCondition.None) continue;
            if ((data.thirdMissionConditions & c) != 0) result.Add(c);
        }
        return result;
    }

    // ─── 개별 라벨 빌더 ──────────────────────────────────────────────────────

    public static string GetTimeLimitLabel(SO_StageData data)
    {
        if (data == null || data.limitTime <= 0f) return L("Mission_TimeLimit");
        return string.Format(L("Mission_TimeLimit_Format"), (int)data.limitTime);
    }

    public static string GetMoveCountLabel(SO_StageData data)
    {
        if (data == null || data.missionActionCount <= 0) return L("Mission_MoveCountLimit");
        return string.Format(L("Mission_MoveCountLimit_Format"), data.missionActionCount);
    }

    public static string GetNoFeatureLabel(SO_StageData data)
    {
        string featureName = data?.forbiddenFeature switch
        {
            ForbiddenFeature.ALT => "ALT",
            ForbiddenFeature.F4  => "F4",
            ForbiddenFeature.TAB => "TAB",
            _                    => null
        };

        if (featureName == null) return L("Mission_NoSpecificFeature");

        if (data.missionFeatureUsageLimit > 0)
            return string.Format(L("Mission_NoSpecificFeature_CountFormat"), featureName, data.missionFeatureUsageLimit);

        return string.Format(L("Mission_NoSpecificFeature_Format"), featureName);
    }
}
