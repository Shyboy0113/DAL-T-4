using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using Eflatun.SceneReference;

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

    [Header("3번째 도전과제 상세 패널")]
    [SerializeField] private GameObject thirdDetailPanel;
    [SerializeField] private GameObject[] thirdConditionPanel;
    [SerializeField] private TMP_Text[] thirdConditionTexts; // 최대 4개, 인스펙터에서 순서대로 할당

    [SerializeField] SO_StageData _currentData;

    [Header("Game Scene - Mission Panel")]
    // Game Scene 내부에서 참조하는 StageInfoPanel 일 경우
    [SerializeField] private SceneReference gameScene;
    [SerializeField] private GameObject thirdMissionDetailPanel;

    [SerializeField] bool  _isInGameScene;
    private float _refreshTimer;

    private void Awake()
    {
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        _isInGameScene = gameScene != null && SceneManager.GetActiveScene().name == gameScene.Name;

        if (_isInGameScene)
        {
            ShowFromData(GameManager.Instance?.currentStageData);
            GameEvents.KeyUsed       += OnKeyUsedRefresh;
            GameEvents.UndoTriggered += OnUndoRefresh;
        }
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    private void OnDisable()
    {
        if (_isInGameScene)
        {
            GameEvents.KeyUsed       -= OnKeyUsedRefresh;
            GameEvents.UndoTriggered -= OnUndoRefresh;
        }
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    private void Update()
    {
        /*
        if (!_isInGameScene || _currentData == null || !gameObject.activeSelf) return;
        if (!HasTimeMission()) return;
        */
        
        if(_currentData == null || !gameObject.activeSelf) return;
        
        _refreshTimer += Time.deltaTime;
        if (_refreshTimer < 0.1f) return;
        _refreshTimer = 0f;
        RefreshTexts(_currentData);
    }

    private bool HasTimeMission() =>
        _currentData.firstMissionType  == MissionType.TimeLimit ||
        _currentData.secondMissionType == MissionType.TimeLimit ||
        (_currentData.thirdMissionConditions & ThirdMissionCondition.TimeLimit) != 0;

    private void OnKeyUsedRefresh(KeyType _) { if (_currentData != null) RefreshTexts(_currentData); }
    private void OnUndoRefresh()              { if (_currentData != null) RefreshTexts(_currentData); }

    private void OnLocaleChanged(Locale _)
    {
        if (_currentData != null)
            RefreshTexts(_currentData);
    }

    public void ShowFromData(SO_StageData data) // Pause Panel에서 데이터를 가져옴
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

    public void Show(StageNode node, Vector2 offset) // StageSelect에서 가져옴
    {
        Debug.Log($"[InfoPanel] Show() 진입 | canvasGroup={canvasGroup != null} | stageData={node?.stageData?.name}");

        if (node?.stageData == null) return;
        _currentData = node.stageData;

        DOTween.Kill(canvasGroup);
        gameObject.SetActive(true);
        canvasGroup.alpha = 0f;

        // 포지션 설정
        var panelRect = GetComponent<RectTransform>();
        Canvas rootCanvas = panelRect.GetComponentInParent<Canvas>().rootCanvas;

        // 1. 화면 정중앙 월드 좌표 계산
        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            rootCanvas.GetComponent<RectTransform>(),
            new Vector2(Screen.width * 0.5f, Screen.height * 0.5f),
            rootCanvas.worldCamera,
            out Vector3 worldPos
        );

        // 2. 중앙에 배치
        panelRect.position = worldPos;

        // 3. offset은 UI 단위로 anchoredPosition에 따로 적용
        panelRect.anchoredPosition += offset;

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
        bool isThirdCleared = progress?.isThirdMissionCleared ?? false;
        SetThirdMission(mission3, data.thirdMissionConditions, isThirdCleared);
        RefreshThirdDetailPanel(data, isThirdCleared);
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

        if (ui.label != null)
        {
            string text = MissionLabelHelper.GetMissionLabel(type, data) + GetMissionProgressString(type, data);
            if (!_isInGameScene && isCleared)
                text = $"<color=yellow>{text}{L("Mission_Progress_Complete")}</color>";
            ui.label.text = text;
        }
        if (ui.clearMark != null) ui.clearMark.gameObject.SetActive(isCleared);
    }

    // ─── 3번째 도전과제 상세 패널 ────────────────────────────────────────

    private void RefreshThirdDetailPanel(SO_StageData data, bool isThirdCleared)
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
                    if (!_isInGameScene && isThirdCleared)
                        text = $"<color=yellow>{text}{L("Mission_Progress_Complete")}</color>";
                    thirdConditionTexts[i].text = text;
                }
            }
        }
    }

    // ─── 3번째 도전과제 헤더 row ────────────────────────────────────────

    private void SetThirdMission(MissionRowUI ui, ThirdMissionCondition conditions, bool isCleared)
    {
        if (ui.row == null) return;

        bool hasMission = conditions != ThirdMissionCondition.None;
        ui.row.SetActive(hasMission);
        if (!hasMission) return;

        if (ui.label != null)
        {
            string text = L("Mission_ThirdMission_Header");
            if (!_isInGameScene && isCleared)
                text = $"<color=yellow>{text}{L("Mission_Progress_Complete")}</color>";
            ui.label.text = text;
        }
        if (ui.clearMark != null) ui.clearMark.gameObject.SetActive(isCleared);
    }

    // ─── 인게임 실시간 진행 표시 ─────────────────────────────────────────

    private string GetMissionProgressString(MissionType type, SO_StageData data)
    {
        if (!_isInGameScene || GameManager.Instance == null) return string.Empty;
        var gm = GameManager.Instance;

        switch (type)
        {
            case MissionType.TimeLimit:
            {
                float cur  = gm.currentTime;
                bool  done = cur <= data.limitTime;
                return string.Format(L("Mission_Progress_Time_Format"), cur.ToString("F1"), (int)data.limitTime)
                       + (done ? L("Mission_Progress_Complete") : "");
            }
            case MissionType.MoveCountLimit:
            {
                int  total = gm.pushedNumberALT + gm.pushedNumberF4 + gm.pushedNumberTAB;
                bool done  = total <= data.missionActionCount;
                return string.Format(L("Mission_Progress_Count_Format"), total, data.missionActionCount)
                       + (done ? L("Mission_Progress_Complete") : "");
            }
            case MissionType.NoSpecificFeature:
            {
                int pushed = data.forbiddenFeature switch
                {
                    ForbiddenFeature.ALT => gm.pushedNumberALT,
                    ForbiddenFeature.F4  => gm.pushedNumberF4,
                    ForbiddenFeature.TAB => gm.pushedNumberTAB,
                    _                    => -1
                };
                if (pushed < 0) return string.Empty;
                bool   done = pushed <= data.missionFeatureUsageLimit;
                string prog = data.missionFeatureUsageLimit <= 0
                    ? string.Format(L("Mission_Progress_FeatureUsed_Format"), pushed)
                    : string.Format(L("Mission_Progress_Count_Format"), pushed, data.missionFeatureUsageLimit);
                return prog + (done ? L("Mission_Progress_Complete") : "");
            }
            case MissionType.KillAllEnemies:
            {
                var  enemies = FindObjectsByType<EnemyBehaviour>(FindObjectsSortMode.None)
                                   .Where(e => e.gameObject.activeSelf).ToArray();
                if (enemies.Length == 0) return string.Empty;
                int  dead = enemies.Count(e => e.IsDead);
                bool done = dead == enemies.Length;
                return string.Format(L("Mission_Progress_Count_Format"), dead, enemies.Length)
                       + (done ? L("Mission_Progress_Complete") : "");
            }
            case MissionType.CollectStar:
            {
                var  stars = FindObjectsByType<TileBehaviour>(FindObjectsSortMode.None)
                                 .Where(t => t.currentTileType == TileType.Star).ToArray();
                if (stars.Length == 0) return string.Empty;
                int  collected = stars.Count(t => t.IsCollected);
                bool done      = collected == stars.Length;
                return string.Format(L("Mission_Progress_Count_Format"), collected, stars.Length)
                       + (done ? L("Mission_Progress_Complete") : "");
            }
            default:
                return string.Empty;
        }
    }

    private string GetThirdConditionProgressString(ThirdMissionCondition condition, SO_StageData data)
    {
        if (!_isInGameScene || GameManager.Instance == null) return string.Empty;
        var gm = GameManager.Instance;

        switch (condition)
        {
            case ThirdMissionCondition.TimeLimit:
            {
                float cur  = gm.currentTime;
                bool  done = cur <= data.limitTime;
                return string.Format(L("Mission_Progress_Time_Format"), cur.ToString("F1"), (int)data.limitTime)
                       + (done ? L("Mission_Progress_Complete") : "");
            }
            case ThirdMissionCondition.MoveCountLimit:
            {
                int  total = gm.pushedNumberALT + gm.pushedNumberF4 + gm.pushedNumberTAB;
                bool done  = total <= data.missionActionCount;
                return string.Format(L("Mission_Progress_Count_Format"), total, data.missionActionCount)
                       + (done ? L("Mission_Progress_Complete") : "");
            }
            case ThirdMissionCondition.NoSpecificFeature:
            {
                int pushed = data.forbiddenFeature switch
                {
                    ForbiddenFeature.ALT => gm.pushedNumberALT,
                    ForbiddenFeature.F4  => gm.pushedNumberF4,
                    ForbiddenFeature.TAB => gm.pushedNumberTAB,
                    _                    => -1
                };
                if (pushed < 0) return string.Empty;
                bool   done = pushed <= data.missionFeatureUsageLimit;
                string prog = data.missionFeatureUsageLimit <= 0
                    ? string.Format(L("Mission_Progress_FeatureUsed_Format"), pushed)
                    : string.Format(L("Mission_Progress_Count_Format"), pushed, data.missionFeatureUsageLimit);
                return prog + (done ? L("Mission_Progress_Complete") : "");
            }
            case ThirdMissionCondition.KillAllEnemies:
            {
                var  enemies = FindObjectsByType<EnemyBehaviour>(FindObjectsSortMode.None)
                                   .Where(e => e.gameObject.activeSelf).ToArray();
                if (enemies.Length == 0) return string.Empty;
                int  dead = enemies.Count(e => e.IsDead);
                bool done = dead == enemies.Length;
                return string.Format(L("Mission_Progress_Count_Format"), dead, enemies.Length)
                       + (done ? L("Mission_Progress_Complete") : "");
            }
            default:
                return string.Empty;
        }
    }

    private string L(string key) =>
        LocalizationSettings.StringDatabase.GetLocalizedString("StageSelect Strings", key);

    public void ToggleThirdMissionDetailPanel()
    {
        if (thirdMissionDetailPanel != null)
        {
            thirdMissionDetailPanel.SetActive(!thirdMissionDetailPanel.activeSelf);
        }
    }
}