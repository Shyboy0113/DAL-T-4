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
        public Image      clearMark;  // 클리어 시 활성화할 아이콘 Image
    }

    [Header("Canvas")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float       fadeTime = 0.18f;

    [Header("Stage Info")]
    [SerializeField] private TMP_Text stageNameText;
    [SerializeField] private TMP_Text stageDescText;

    [Header("Status")]
    [SerializeField] private GameObject clearBadge;   // 클리어 배지
    [SerializeField] private GameObject lockedBadge;  // 잠금 배지
    [SerializeField] private GameObject confirmHint;  // "Enter로 진입" 힌트

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

    /// <summary>
    /// 일시정지 패널 등 위치 조정 없이 현재 스테이지 데이터를 바로 표시합니다.
    /// </summary>
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

        bool isLocked  = node.CurrentState == StageNode.NodeState.Locked;
        bool isCleared = node.CurrentState == StageNode.NodeState.Cleared;

        if (clearBadge  != null) clearBadge.SetActive(isCleared);
        if (lockedBadge != null) lockedBadge.SetActive(isLocked);
        if (confirmHint != null) confirmHint.SetActive(!isLocked);

        RefreshTexts(_currentData);

        canvasGroup.DOFade(1f, fadeTime);
    }

    /// <summary>현재 언어로 텍스트 필드를 다시 채웁니다. 언어 변경 시에도 호출됩니다.</summary>
    private void RefreshTexts(SO_StageData data)
    {
        var jdm      = GameManager.Instance?.jsonDataManager;
        var progress = jdm?.GetStageData(data.chapterNum, data.stageNum);

        if (stageNameText != null) stageNameText.text = data.stageName;
        if (stageDescText  != null) stageDescText.text = LDesc(data);

        SetMission(mission1, data.firstMissionType,  progress?.isFirstMissionCleared  ?? false);
        SetMission(mission2, data.secondMissionType, progress?.isSecondMissionCleared ?? false);
        SetMission(mission3, data.thirdMissionType,  progress?.isThirdMissionCleared  ?? false);
    }

    public void Hide()
    {
        canvasGroup.DOKill();
        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    private void SetMission(MissionRowUI ui, MissionType type, bool isCleared)
    {
        if (ui.row == null) return;

        bool hasMission = type != MissionType.None;
        ui.row.SetActive(hasMission);
        if (!hasMission) return;

        if (ui.label    != null) ui.label.text = GetMissionLabel(type);
        if (ui.clearMark != null) ui.clearMark.gameObject.SetActive(isCleared);
    }

    private string GetMissionLabel(MissionType type) => type switch
    {
        MissionType.StageClear        => L("Mission_StageClear"),
        MissionType.TimeLimit         => L("Mission_TimeLimit"),
        MissionType.MoveCountLimit    => L("Mission_MoveCountLimit"),
        MissionType.KillAllEnemies    => L("Mission_KillAllEnemies"),
        MissionType.CollectStar       => L("Mission_CollectStar"),
        MissionType.NoSpecificFeature => L("Mission_NoSpecificFeature"),
        _                             => ""
    };

    private string L(string key) =>
        LocalizationSettings.StringDatabase.GetLocalizedString("StageSelect Strings", key);

    private string LDesc(SO_StageData data) =>
        LocalizationSettings.StringDatabase.GetLocalizedString(
            "StageData Strings", $"Stage_{data.chapterNum}-{data.stageNum}_Desc");
}
