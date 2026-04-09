using UnityEngine;
using UnityEngine.EventSystems;
using Eflatun.SceneReference;
using System.Collections;

/// <summary>
/// 스테이지 셀렉트 씬의 진입점.
/// 초기 포커스 설정, StageNode의 OnConfirmed 이벤트 수신, 씬 전환, ESC 처리를 담당합니다.
/// </summary>
public class StageSelectManagement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CutoutFade        cutoutFade;
    [SerializeField] private StageSelectPlayer selectPlayer;
    [SerializeField] private SceneReference    gameScene;
    [SerializeField] private SceneReference    introScene;
    [SerializeField] private SceneReference    stageSelectScene;
    
    /// <summary>씬 시작 시 포커스를 맞출 첫 번째 노드 (보통 마지막으로 클리어한 스테이지)</summary>
    [SerializeField] private StageNode defaultNode;

    private bool       _isTransitioning = false;
    private StageNode[] _allNodes;

    private void Start()
    {
        _allNodes = FindObjectsByType<StageNode>(FindObjectsSortMode.None);

        // 세이브 데이터 기준으로 모든 노드 시각 갱신 + 이벤트 구독
        foreach (var node in _allNodes)
        {
            node.RefreshVisuals();
            node.OnConfirmed += OnNodeConfirmed;
        }

        // 초기 포커스 설정
        // defaultNode가 없으면 진행 가능한 가장 빠른 노드를 자동으로 찾음
        StageNode firstFocus = defaultNode != null ? defaultNode : FindFirstAvailableNode();
        if (firstFocus != null)
        {
            EventSystem.current.SetSelectedGameObject(firstFocus.gameObject);
            selectPlayer.SnapTo(firstFocus.GetComponent<RectTransform>());
        }

        cutoutFade.FadeIn();
    }

    private void OnDestroy()
    {
        if (_allNodes == null) return;
        foreach (var node in _allNodes)
        {
            if (node != null)
                node.OnConfirmed -= OnNodeConfirmed;
        }
    }

    private void Update()
    {
        if (_isTransitioning) return;
        
        if (Input.GetKeyDown(KeyCode.Escape))
            ReturnToMenu();
    }

    private void OnNodeConfirmed(StageNode node)
    {
        if (_isTransitioning || node?.stageData == null) return;
        _isTransitioning = true;

        EventSystem.current.SetSelectedGameObject(null);
        selectPlayer.Lock();

        GameManager.Instance.chapter = node.stageData.chapterNum;
        GameManager.Instance.stage   = node.stageData.stageNum;
        
        StartCoroutine(IFadeOut(1.0f));
        
    }

    public void ReturnToMenu()
    {
        if (_isTransitioning) return;
        _isTransitioning = true;

        selectPlayer?.Lock();
        cutoutFade.FadeOut(() =>
            StartCoroutine(SceneLoader.LoadScene(introScene)));
    }

    public void ReloadStageSelectScene()
    {
        EventSystem.current.SetSelectedGameObject(null);
        selectPlayer?.Lock();
        
        cutoutFade.FadeOut(() =>
            StartCoroutine(SceneLoader.LoadScene(stageSelectScene)));
    }

    // 잠기지 않은 노드 중 stageNum이 가장 낮은 것을 반환
    private StageNode FindFirstAvailableNode()
    {
        StageNode best = null;
        foreach (var node in _allNodes)
        {
            if (!node.CanEnter()) continue;
            if (best == null) { best = node; continue; }

            bool earlierChapter = node.stageData.chapterNum < best.stageData.chapterNum;
            bool sameChapterEarlierStage = node.stageData.chapterNum == best.stageData.chapterNum &&
                                           node.stageData.stageNum   <  best.stageData.stageNum;
            if (earlierChapter || sameChapterEarlierStage) best = node;
        }
        return best;
    }

    private IEnumerator IWait(float time)
    {
        yield return new WaitForSeconds(time);
    }

    private IEnumerator IFadeOut(float time)
    {
        yield return IWait(time);
        
        cutoutFade.FadeOut(() => StartCoroutine(SceneLoader.LoadScene(gameScene)));
        
    }
    
}
