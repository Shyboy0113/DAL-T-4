using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

/// <summary>
/// 스테이지 정보 패널의 최상위 부모 클래스.
/// 공통적인 UI 참조와 데이터(SO_StageData) 바인딩, 다국어 처리만 담당합니다.
/// 연출이나 실시간 업데이트 로직은 자식 클래스에서 구현합니다.
/// </summary>
public abstract class Base_StageInfoPanel : MonoBehaviour
{
    [System.Serializable]
    public struct MissionRowUI
    {
        public GameObject row;
        public TMP_Text   label;
        public Image      clearMark;
    }

    [Header("Base - Canvas")]
    [SerializeField] protected CanvasGroup canvasGroup;

    [Header("Base - Stage Info")]
    [SerializeField] protected TMP_Text stageNameText;

    [Header("Base - Status")]
    [SerializeField] protected GameObject clearBadge;
    [SerializeField] protected GameObject lockedBadge;

    [Header("Base - Missions")]    
    [SerializeField] protected MissionRowUI mission1;
    [SerializeField] protected MissionRowUI mission2;
    [SerializeField] protected MissionRowUI mission3;

    [Header("Base - 3rd Mission Detail")]
    [SerializeField] protected GameObject thirdDetailPanel;
    [SerializeField] protected GameObject[] thirdConditionPanel;
    [SerializeField] protected TMP_Text[] thirdConditionTexts;

    protected SO_StageData _currentData;

    // 인게임 씬인지 여부 (자식 클래스에서 오버라이드하여 노란색 텍스트 하이라이트 여부 결정)
    protected virtual bool IsInGameScene => false;

    protected virtual void Awake()
    {
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    protected virtual void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    protected virtual void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    private void OnLocaleChanged(Locale _)
    {
        if (_currentData != null)
            RefreshTexts(_currentData);
    }

    /// <summary>
    /// 데이터를 받아 기본 UI를 활성화하고 텍스트를 갱신합니다.
    /// (애니메이션이나 팝업 연출은 자식 클래스에서 base.ShowInfo() 호출 후 구현)
    /// </summary>
    public virtual void ShowInfo(SO_StageData data)
    {
        if (data == null) return;
        _currentData = data;

        RefreshTexts(data);

        gameObject.SetActive(true);
    }

    /// <summary>
    /// 패널을 숨깁니다.
    /// </summary>
    public virtual void Hide()
    {
        if (canvasGroup != null)
        {
            canvasGroup.DOKill();
            canvasGroup.alpha = 0f;
        }
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 세이브 데이터(JsonDataManager)와 스테이지 데이터를 비교하여 텍스트를 갱신합니다.
    /// </summary>
    protected virtual void RefreshTexts(SO_StageData data)
    {
        var jdm      = GameManager.Instance?.jsonDataManager;
        var progress = jdm?.GetStageData(data.chapterNum, data.stageNum);

        if (stageNameText != null) stageNameText.text = data.stageName;

        bool isFirstCleared = progress?.isFirstMissionCleared ?? false;
        bool isSecondCleared = progress?.isSecondMissionCleared ?? false;
        bool isThirdCleared = progress?.isThirdMissionCleared ?? false;

        SetMission(mission1, data.firstMissionType, isFirstCleared, data);
        SetMission(mission2, data.secondMissionType, isSecondCleared, data);
        
        SetThirdMissionHeader(mission3, data.thirdMissionConditions, isThirdCleared);
        RefreshThirdDetailPanel(data, isThirdCleared);
    }

    protected void SetMission(MissionRowUI ui, MissionType type, bool isCleared, SO_StageData data)
    {
        if (ui.row == null) return;

        bool hasMission = type != MissionType.None;
        ui.row.SetActive(hasMission);
        if (!hasMission) return;

        if (ui.label != null)
        {
            string text = MissionLabelHelper.GetMissionLabel(type, data) + GetMissionProgressString(type, data);
            
            // 로비나 일시정지 상태에서 클리어한 미션은 노란색 하이라이트 처리
            if (!IsInGameScene && isCleared)
                text = $"<color=yellow>{text}{L("Mission_Progress_Complete")}</color>";
                
            ui.label.text = text;
        }
        if (ui.clearMark != null) ui.clearMark.gameObject.SetActive(isCleared);
    }

    protected void SetThirdMissionHeader(MissionRowUI ui, ThirdMissionCondition conditions, bool isCleared)
    {
        if (ui.row == null) return;

        bool hasMission = conditions != ThirdMissionCondition.None;
        ui.row.SetActive(hasMission);
        if (!hasMission) return;

        if (ui.label != null)
        {
            string text = L("Mission_ThirdMission_Header");
            if (!IsInGameScene && isCleared)
                text = $"<color=yellow>{text}{L("Mission_Progress_Complete")}</color>";
            ui.label.text = text;
        }
        if (ui.clearMark != null) ui.clearMark.gameObject.SetActive(isCleared);
    }

    protected void RefreshThirdDetailPanel(SO_StageData data, bool isThirdCleared)
    {
        if (thirdDetailPanel == null) return;

        var conditions = MissionLabelHelper.GetConditions(data);
        bool hasAny = conditions.Count > 0;
        thirdDetailPanel.SetActive(hasAny);
        if (!hasAny) return;

        for (int i = 0; i < thirdConditionPanel.Length; i++)
        {
            if (thirdConditionPanel[i] == null) continue;

            bool isActive = i < conditions.Count;
            thirdConditionPanel[i].SetActive(isActive);

            if (isActive && thirdConditionTexts != null && i < thirdConditionTexts.Length)
            {
                if (thirdConditionTexts[i] != null)
                {
                    string text = MissionLabelHelper.GetThirdConditionLabel(conditions[i], data)
                        + GetThirdConditionProgressString(conditions[i], data);
                        
                    if (!IsInGameScene && isThirdCleared)
                        text = $"<color=yellow>{text}{L("Mission_Progress_Complete")}</color>";
                        
                    thirdConditionTexts[i].text = text;
                }
            }
        }
    }

    protected string L(string key) => LocalizationSettings.StringDatabase.GetLocalizedString("StageSelect Strings", key);

    // ─────────────────────────────────────────────────────────
    // 자식 클래스(InGameInfoPanel 등)에서 오버라이드 할 실시간 진행도 함수
    // ─────────────────────────────────────────────────────────

    protected virtual string GetMissionProgressString(MissionType type, SO_StageData data)
    {
        // 기본은 빈 문자열 (월드맵에서는 카운트를 보여주지 않으므로)
        return string.Empty;
    }

    protected virtual string GetThirdConditionProgressString(ThirdMissionCondition condition, SO_StageData data)
    {
        // 기본은 빈 문자열
        return string.Empty;
    }
}