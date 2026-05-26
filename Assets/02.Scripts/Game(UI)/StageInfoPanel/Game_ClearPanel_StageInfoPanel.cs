using System.Collections;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// 스테이지 클리어 후 나타나는 최종 결과 패널입니다.
/// Base_StageInfoPanel을 상속받아 기본 UI 구조를 유지하되,
/// 클리어 연출(DOTween)과 세이브되지 않은 "현재 방금 달성한" 데이터를 표시하도록 오버라이드합니다.
/// </summary>
public class Game_ClearPanel_StageInfoPanel : Base_StageInfoPanel, IStageClearEffect
{
    [Header("Clear Effect Timing")]
    [SerializeField] private float rowInterval = 0.25f;
    [SerializeField] private float fadeInDuration = 0.30f;

    // 완료된 미션을 노란색 텍스트로 처리하기 위해 false 유지
    protected override bool IsInGameScene => false;

    // ─── IStageClearEffect ──────────────────────────────────────────────────

    public IEnumerator Execute()
    {
        Debug.Log("[ClearPanel] Execute() 코루틴 시작됨.");
        var gm = GameManager.Instance;
        
        if (gm != null && gm.currentStageData != null)
        {
            Debug.Log($"[ClearPanel] GameManager에서 스테이지 데이터 확인됨: {gm.currentStageData.stageName}. ShowInfo 호출.");
            // 부모의 ShowInfo를 호출해 기본 데이터 할당 및 UI 활성화를 진행합니다.
            ShowInfo(gm.currentStageData);
        }
        else
        {
            Debug.LogError("[ClearPanel] GameManager.Instance가 null이거나 currentStageData가 없습니다!");
        }

        int activeCount = GetActiveMissionCount();
        float waitTime = activeCount > 0 ? (rowInterval * (activeCount - 1)) + fadeInDuration : 0f;
        
        Debug.Log($"[ClearPanel] 활성화된 미션 개수: {activeCount} / 연출 대기 시간: {waitTime}초");
        yield return new WaitForSeconds(waitTime);
        Debug.Log("[ClearPanel] Execute() 코루틴 대기 완료 및 종료.");
    }

    public void ResetEffect()
    {
        Debug.Log("[ClearPanel] ResetEffect() 호출됨. 패널 숨김 및 연출 초기화.");
        Hide(); // Base_StageInfoPanel의 숨김 처리 호출
        
        // 투명도 등 연출 찌꺼기 초기화
        ResetClearMark(mission1);
        ResetClearMark(mission2);
        ResetClearMark(mission3);
    }

    private void ResetClearMark(MissionRowUI ui)
    {
        if (ui.clearMark != null)
        {
            ui.clearMark.DOKill();
            Color c = ui.clearMark.color;
            c.a = 0f;
            ui.clearMark.color = c;
        }
    }

    // ─── 베이스 클래스 로직 오버라이드 (핵심) ────────────────────────────────

    public override void ShowInfo(SO_StageData data)
    {
        Debug.Log("[ClearPanel] ShowInfo() 진입. 부모 클래스 base.ShowInfo(data) 실행.");
        // 1. 부모 로직 실행 (텍스트 갱신, 마크 켜기 등)
        base.ShowInfo(data);
        
        // 2. 결과창이므로 잠금/클리어 뱃지 끄기
        if (clearBadge != null) clearBadge.SetActive(false);
        if (lockedBadge != null) lockedBadge.SetActive(false);

        Debug.Log("[ClearPanel] 순차적 클리어 마크 애니메이션 세팅 시작.");
        // 3. 켜져있는 클리어 마크들을 낚아채서 투명하게 만든 뒤 순차 페이드인 실행
        AnimateClearMark(mission1, 0);
        AnimateClearMark(mission2, 1);
        AnimateClearMark(mission3, 2);
    }

    /// <summary>
    /// Base_StageInfoPanel은 세이브된 과거 데이터(jdm)를 읽어오지만, 
    /// 클리어 패널은 "방금 달성한 현재 결과(currentProgressData)"를 읽어야 하므로 덮어씌웁니다.
    /// </summary>
    protected override void RefreshTexts(SO_StageData data)
    {
        Debug.Log("[ClearPanel] RefreshTexts() 오버라이드 실행. 현재 진행 데이터(currentProgressData) 로드 시도.");
        
        var gm = GameManager.Instance;
        // 세이브 데이터 대신 현재 진행 데이터를 직접 꽂아줌
        var progress = gm?.currentProgressData; 

        if (progress == null)
        {
            Debug.LogWarning("[ClearPanel] currentProgressData가 null입니다! 미션 클리어 여부를 정상적으로 판단할 수 없습니다.");
        }

        if (stageNameText != null) stageNameText.text = data.stageName;

        bool isFirstCleared = progress?.isFirstMissionCleared ?? false;
        bool isSecondCleared = progress?.isSecondMissionCleared ?? false;
        bool isThirdCleared = progress?.isThirdMissionCleared ?? false;

        Debug.Log($"[ClearPanel] 미션 클리어 상태 - 1번: {isFirstCleared}, 2번: {isSecondCleared}, 3번: {isThirdCleared}");

        // 부모의 SetMission 함수들을 그대로 활용
        SetMission(mission1, data.firstMissionType, isFirstCleared, data);
        SetMission(mission2, data.secondMissionType, isSecondCleared, data);
        SetThirdMissionHeader(mission3, data.thirdMissionConditions, isThirdCleared);
        
        RefreshThirdDetailPanel(data, isThirdCleared);
    }

    /// <summary>
    /// 기존 결과창에 있던 "12.3초" 등의 실제 달성 수치를 가져오도록 오버라이드
    /// </summary>
    protected override string GetThirdConditionProgressString(ThirdMissionCondition condition, SO_StageData data)
    {
        var gm = GameManager.Instance;
        if (gm == null) return string.Empty;

        Debug.Log($"[ClearPanel] 3번째 조건 달성 텍스트 갱신 중... (조건: {condition})");

        switch (condition)
        {
            case ThirdMissionCondition.TimeLimit:
                string timeText = string.Format(L("Mission_Clear_Actual_Time"), gm.currentTime.ToString("F1"));
                Debug.Log($"[ClearPanel] - 산출된 시간 텍스트: {timeText}");
                return timeText;

            case ThirdMissionCondition.MoveCountLimit:
                int total = gm.pushedNumberALT + gm.pushedNumberF4 + gm.pushedNumberTAB;
                string moveText = string.Format(L("Mission_Clear_Actual_Count"), total);
                Debug.Log($"[ClearPanel] - 산출된 이동 횟수 텍스트: {moveText}");
                return moveText;

            case ThirdMissionCondition.NoSpecificFeature:
                int pushed = data.forbiddenFeature switch
                {
                    ForbiddenFeature.ALT => gm.pushedNumberALT,
                    ForbiddenFeature.F4  => gm.pushedNumberF4,
                    ForbiddenFeature.TAB => gm.pushedNumberTAB,
                    _                    => -1
                };
                if (pushed < 0) return string.Empty;
                string featureText = string.Format(L("Mission_Clear_Actual_Count"), pushed);
                Debug.Log($"[ClearPanel] - 산출된 제한 기능 사용 횟수 텍스트: {featureText}");
                return featureText;

            default:
                return string.Empty;
        }
    }

    // ─── 내부 애니메이션 및 헬퍼 ──────────────────────────────────────────

    private void AnimateClearMark(MissionRowUI ui, int index)
    {
        if (ui.clearMark != null && ui.clearMark.gameObject.activeSelf)
        {
            Debug.Log($"[ClearPanel] 미션 인덱스 {index}번 클리어 마크 연출 시작 대기. (Delay: {rowInterval * index}초)");
            
            ui.clearMark.DOKill();
            Color c = ui.clearMark.color;
            c.a = 0f;
            ui.clearMark.color = c;

            DOVirtual.DelayedCall(rowInterval * index, () => 
            {
                Debug.Log($"[ClearPanel] 미션 인덱스 {index}번 클리어 마크 페이드인 실행!");
                ui.clearMark.DOFade(1f, fadeInDuration);
            });
        }
        else
        {
            Debug.Log($"[ClearPanel] 미션 인덱스 {index}번 클리어 마크 연출 스킵. (조건 미달성 또는 컴포넌트 없음)");
        }
    }

    private int GetActiveMissionCount()
    {
        if (_currentData == null) return 0;
        int count = 0;
        if (_currentData.firstMissionType != MissionType.None) count++;
        if (_currentData.secondMissionType != MissionType.None) count++;
        if (_currentData.thirdMissionConditions != ThirdMissionCondition.None) count++;
        return count;
    }
}