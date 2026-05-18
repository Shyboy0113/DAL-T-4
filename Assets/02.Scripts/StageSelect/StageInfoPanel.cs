using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

/// <summary>
/// 플레이어가 스테이지 노드를 선택하면 나타나는 정보 패널.
/// 스테이지 이름, 클리어/잠금 상태, 미션 3개의 달성 여부를 표시합니다.
/// </summary>
public class StageInfoPanel : MonoBehaviour
{
    [System.Serializable]
    public struct MissionRowUI
    {
        public GameObject row;
        public TMP_Text   label;
        public Image      clearMark;
    }

    [Header("Canvas")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float       fadeTime = 0.18f;

    [Header("Stage Info")]
    [SerializeField] private TMP_Text stageNameText;

    [Header("Status")]
    [SerializeField] private GameObject clearBadge;
    [SerializeField] private GameObject lockedBadge;
    [SerializeField] private GameObject confirmHint;

    [Header("Missions")]
    [SerializeField] private MissionRowUI mission1;
    [SerializeField] private MissionRowUI mission2;
    [SerializeField] private MissionRowUI mission3;

    private SO_StageData _currentData;

    private void Awake()
    {
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        ShowFromData(GameManager.Instance?.currentStageData);
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    private void OnLocaleChanged(Locale _)
    {
        if (_currentData != null)
            RefreshTexts(_currentData);
    }

    public void ShowFromData(SO_StageData data)
    {
        if (data == null) return;
        _currentData = data;

        var jdm      = GameManager.Instance?.jsonDataManager;
        var progress = jdm?.GetStageData(data.chapterNum, data.stageNum);

        if (clearBadge  != null) clearBadge.SetActive(progress?.isCleared ?? false);
        if (lockedBadge != null) lockedBadge.SetActive(false);
        if (confirmHint != null) confirmHint.SetActive(false);

        RefreshTexts(data);

        gameObject.SetActive(true);
        if (canvasGroup != null)
        {
            canvasGroup.DOKill();
            canvasGroup.alpha = 1f;
        }
    }

    public void Show(StageNode node, RectTransform nodeRect, Vector2 offset)
    {
        Debug.Log($"[InfoPanel] Show() 진입 | canvasGroup={canvasGroup != null} | stageData={node?.stageData?.name}");

        if (node?.stageData == null) return;
        _currentData = node.stageData;

        DOTween.Kill(canvasGroup);
        gameObject.SetActive(true);
        canvasGroup.alpha = 0f;

        var panelRect = GetComponent<RectTransform>();
        Vector2 nodeScreenPos = RectTransformUtility.WorldToScreenPoint(null, nodeRect.position);

        float screenCenter = Screen.width * 0.5f;
        float sign = nodeScreenPos.x < screenCenter ? 1f : -1f;

        panelRect.position = nodeRect.position;
        panelRect.anchoredPosition += new Vector2(offset.x * sign, offset.y);

        Debug.Log($"[InfoPanel] 포지션 설정 완료 | anchoredPos={panelRect.anchoredPosition}");

        bool isLocked  = node.CurrentState == StageNode.NodeState.Locked;
        bool isCleared = node.CurrentState == StageNode.NodeState.Cleared;

        if (clearBadge  != null) clearBadge.SetActive(isCleared);
        if (lockedBadge != null) lockedBadge.SetActive(isLocked);
        if (confirmHint != null) confirmHint.SetActive(!isLocked);

        try
        {
            RefreshTexts(_currentData);
            Debug.Log("[InfoPanel] RefreshTexts 완료");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[InfoPanel] RefreshTexts 예외 발생: {e}");
        }

        canvasGroup.DOFade(1f, fadeTime);
        Debug.Log($"[InfoPanel] DOFade 시작 | fadeTime={fadeTime} | alpha={canvasGroup.alpha}");
    }

    private void RefreshTexts(SO_StageData data)
    {
        var jdm      = GameManager.Instance?.jsonDataManager;
        var progress = jdm?.GetStageData(data.chapterNum, data.stageNum);

        if (stageNameText != null) stageNameText.text = data.stageName;

        SetMission(mission1, data.firstMissionType,  progress?.isFirstMissionCleared  ?? false, data);
        SetMission(mission2, data.secondMissionType, progress?.isSecondMissionCleared ?? false, data);
        SetMission(mission3, data.thirdMissionType,  progress?.isThirdMissionCleared  ?? false, data);
    }

    public void Hide()
    {
        canvasGroup.DOKill();
        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    private void SetMission(MissionRowUI ui, MissionType type, bool isCleared, SO_StageData data)
    {
        if (ui.row == null) return;

        bool hasMission = type != MissionType.None;
        ui.row.SetActive(hasMission);
        if (!hasMission) return;

        if (ui.label     != null) ui.label.text = GetMissionLabel(type, data);
        if (ui.clearMark != null) ui.clearMark.gameObject.SetActive(isCleared);
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