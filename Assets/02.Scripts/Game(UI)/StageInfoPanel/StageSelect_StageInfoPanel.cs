using UnityEngine;
using DG.Tweening;

/// <summary>
/// Stage Select 씬의 월드맵 노드를 클릭했을 때 나타나는 팝업 패널입니다.
/// 실시간 미션 갱신 없이, 연출과 노드 상태 표시만 책임집니다.
/// </summary>
public class StageSelect_StageInfoPanel : Base_StageInfoPanel
{
    [Header("Stage Select Specific")]
    [SerializeField] private float fadeTime = 0.18f;
    [SerializeField] private GameObject confirmHint;

    // 월드맵에서는 실시간 인게임 미션 카운트가 필요 없으므로 false
    protected override bool IsInGameScene => false;

    /// <summary>
    /// StageNode에서 호출하여 패널을 띄웁니다.
    /// </summary>
    public void ShowAtNode(StageNode node, Vector2 offset)
    {
        if (node?.stageData == null) return;

        // 1. 부모 클래스의 기본 UI 세팅 (텍스트 및 기본 데이터 할당)
        base.ShowInfo(node.stageData);

        // 2. Stage Select 전용 상태(Lock, Clear) UI 처리
        bool isLocked = node.CurrentState == StageNode.NodeState.Locked;
        bool isCleared = node.CurrentState == StageNode.NodeState.Cleared;

        if (clearBadge != null) clearBadge.SetActive(isCleared);
        if (lockedBadge != null) lockedBadge.SetActive(isLocked);
        if (confirmHint != null) confirmHint.SetActive(!isLocked);

        // 3. 화면 중앙 기준 오프셋 위치 계산
        var panelRect = GetComponent<RectTransform>();
        Canvas rootCanvas = panelRect.GetComponentInParent<Canvas>().rootCanvas;

        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            rootCanvas.GetComponent<RectTransform>(),
            new Vector2(Screen.width * 0.5f, Screen.height * 0.5f),
            rootCanvas.worldCamera,
            out Vector3 worldPos
        );

        panelRect.position = worldPos;
        panelRect.anchoredPosition += offset;

        // 4. 등장 애니메이션 (DOTween)
        if (canvasGroup != null)
        {
            DOTween.Kill(canvasGroup);
            canvasGroup.alpha = 0f;
            canvasGroup.DOFade(1f, fadeTime);
        }
    }

    public override void Hide()
    {
        if (canvasGroup != null)
        {
            canvasGroup.DOKill();
            canvasGroup.alpha = 0f;
        }
        base.Hide();
    }
}