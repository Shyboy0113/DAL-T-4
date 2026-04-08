using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

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
    
    private void Awake()
    {
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    public void Show(StageNode node, RectTransform nodeRect, Vector2 offset)
    {
        if (node?.stageData == null) return;

        // 패널 위치 계산 — 화면 중앙 기준 좌/우 반전
        var panelRect = GetComponent<RectTransform>();
        Vector2 nodeScreenPos = RectTransformUtility.WorldToScreenPoint(
            null, nodeRect.position);

        float screenCenter = Screen.width * 0.5f;
        float sign = nodeScreenPos.x < screenCenter ? 1f : -1f;

        panelRect.position = nodeRect.position;
        panelRect.anchoredPosition += new Vector2(offset.x * sign, offset.y);
        
        var data     = node.stageData;
        var jdm      = GameManager.Instance?.jsonDataManager;
        var progress = jdm?.GetStageData(data.chapterNum, data.stageNum);

        bool isLocked  = node.CurrentState == StageNode.NodeState.Locked;
        bool isCleared = node.CurrentState == StageNode.NodeState.Cleared;

        if (stageNameText != null) stageNameText.text = data.stageName;
        if (stageDescText  != null) stageDescText.text = data.stageDescription;

        if (clearBadge   != null) clearBadge.SetActive(isCleared);
        if (lockedBadge  != null) lockedBadge.SetActive(isLocked);
        if (confirmHint  != null) confirmHint.SetActive(!isLocked);

        SetMission(mission1, data.firstMissionType,  progress?.isFirstMissionCleared  ?? false);
        SetMission(mission2, data.secondMissionType, progress?.isSecondMissionCleared ?? false);
        SetMission(mission3, data.thirdMissionType,  progress?.isThirdMissionCleared  ?? false);

        gameObject.SetActive(true);
        canvasGroup.DOKill();
        canvasGroup.DOFade(1f, fadeTime);
    }

    public void Hide()
    {
        canvasGroup.DOKill();
        canvasGroup.DOFade(0f, fadeTime)
            .OnComplete(() => gameObject.SetActive(false));
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
        MissionType.StageClear        => "스테이지 클리어",
        MissionType.TimeLimit         => "제한 시간 내 클리어",
        MissionType.MoveCountLimit    => "최소 입력으로 클리어",
        MissionType.KillAllEnemies    => "모든 적 처치",
        MissionType.CollectStar       => "별 수집",
        MissionType.NoSpecificFeature => "특정 기능 없이 클리어",
        _                             => ""
    };
}
