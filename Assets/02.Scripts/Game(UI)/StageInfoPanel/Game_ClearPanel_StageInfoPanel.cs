using UnityEngine;

/// <summary>
/// 스테이지 클리어 후 나타나는 최종 결과 패널입니다.
/// 달성한 미션을 노란색 텍스트(완료)로 강조하여 보여주는 역할만 담당합니다.
/// </summary>
public class Game_ClearPanel_StageInfoPanel : Base_StageInfoPanel
{
    // 로비(월드맵)처럼 달성한 미션을 노란색 텍스트로 하이라이트 하기 위해 false로 설정합니다.
    protected override bool IsInGameScene => false;

    public override void ShowInfo(SO_StageData data)
    {
        // 1. 부모 클래스의 기본 UI 세팅 및 텍스트 갱신 (달성 여부 체크 포함)
        base.ShowInfo(data);
        
        // 2. 클리어 결과창 내부이므로 불필요한 월드맵용 노드 뱃지는 강제로 끕니다.
        if (clearBadge != null) clearBadge.SetActive(false);
        if (lockedBadge != null) lockedBadge.SetActive(false);
    }
}