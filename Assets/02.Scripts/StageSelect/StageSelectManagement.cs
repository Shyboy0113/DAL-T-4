using UnityEngine;
using UnityEngine.EventSystems;
using Eflatun.SceneReference;
using System.Collections;
using UnityEngine.UI;

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

    [Header("Chapter Navigation")]
    [SerializeField] private GameObject[] chapters;
    [SerializeField] private GameObject   returnButton;

    private bool        _isTransitioning = false;
    private int         _currentChapter  = 0;
    private StageNode[] _allNodes;

    private void Start()
    {
        _allNodes = FindObjectsByType<StageNode>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        _currentChapter = 0;
        for (int i = 0; i < chapters.Length; i++)
        {
            if (chapters[i] != null && chapters[i].activeSelf) { _currentChapter = i; break; }
        }

        for (int i = 0; i < chapters.Length; i++)
        {
            if (chapters[i] != null)
                chapters[i].SetActive(i == _currentChapter);
        }

        foreach (var node in _allNodes)
        {
            node.RefreshVisuals();
            node.OnConfirmed += OnNodeConfirmed;
        }

        StageNode firstFocus = (defaultNode != null && defaultNode.gameObject.activeInHierarchy)
            ? defaultNode
            : FindFirstAvailableNode();

        if (firstFocus != null)
        {
            EventSystem.current.SetSelectedGameObject(firstFocus.gameObject);
            selectPlayer.SnapTo(firstFocus.GetComponent<RectTransform>());
        }

        _isTransitioning = true; // FadeIn 중 Q/E 입력 차단
        cutoutFade.FadeIn(() =>
        {
            _isTransitioning = false;
        });
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

        if (Input.GetKeyDown(KeyCode.Q)) TrySwitchChapter(-1);
        if (Input.GetKeyDown(KeyCode.E)) TrySwitchChapter(+1);

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // 패널이 열려 있으면 패널만 닫고, 닫혀 있으면 메뉴로 복귀
            var selectedNode = EventSystem.current.currentSelectedGameObject?.GetComponent<StageNode>();
            if (selectedNode != null && selectedNode.IsPanelOpen)
                selectedNode.ClosePanel();
            else
                ReturnToMenu();
        }
    }

    private void OnNodeConfirmed(StageNode node)
    {
        if (_isTransitioning || node?.stageData == null) return;
        _isTransitioning = true;

        // null 대신 Return Button — null이면 FocusKeeper가 즉시 _lastSelectedObject로 복구해 OnSelect 재발화함
        EventSystem.current.SetSelectedGameObject(returnButton);
        if (returnButton != null)
            returnButton.GetComponent<Button>().interactable = false;
        
        selectPlayer.Lock();

        GameManager.Instance.chapter = node.stageData.chapterNum;
        GameManager.Instance.stage   = node.stageData.stageNum;
        
        StartCoroutine(IFadeOut(2.0f));
        
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

    public static bool CanEnterChapter(int chapterIndex)
    {
        int chapterNum = chapterIndex + 1;
        var nodes = FindObjectsByType<StageNode>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var node in nodes)
        {
            if (node?.stageData == null) continue;
            if (node.stageData.chapterNum == chapterNum && node.CanEnter()) return true;
        }
        return false;
    }

    public void TrySwitchChapter(int delta)
    {
        int target = _currentChapter + delta;
        if (target < 0 || target >= chapters.Length) return;
        if (delta > 0 && !CanEnterChapter(target)) return;

        _isTransitioning = true;
        EventSystem.current.SetSelectedGameObject(returnButton);
        selectPlayer.Lock();

        cutoutFade.FadeOut(() =>
        {
            chapters[_currentChapter].SetActive(false);
            _currentChapter = target;
            chapters[_currentChapter].SetActive(true);

            // 챕터 전환 시점에 _allNodes 재수집
            // 에디터 툴 조작, 씬 리로드 등으로 스테일 참조가 생길 수 있으므로
            _allNodes = FindObjectsByType<StageNode>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var node in _allNodes)
            {
                node.OnConfirmed -= OnNodeConfirmed; // 중복 구독 방지
                node.OnConfirmed += OnNodeConfirmed;

                if (node.gameObject.activeInHierarchy)
                    node.RefreshVisuals();
            }

            if (returnButton != null)
                EventSystem.current.SetSelectedGameObject(returnButton);

            StageNode firstFocus = (defaultNode != null && defaultNode.gameObject.activeInHierarchy)
                ? defaultNode
                : FindFirstAvailableNode();

            if (firstFocus != null)
            {
                EventSystem.current.SetSelectedGameObject(firstFocus.gameObject);
                selectPlayer.SnapTo(firstFocus.GetComponent<RectTransform>());
            }

            cutoutFade.FadeIn(() =>
            {
                selectPlayer.Unlock();
                _isTransitioning = false;
            });
        });
    }

    // 잠기지 않은 노드 중 stageNum이 가장 낮은 것을 반환 (현재 활성 챕터 한정)
    private StageNode FindFirstAvailableNode()
    {
        StageNode best = null;
        foreach (var node in _allNodes)
        {
            if (!node.gameObject.activeInHierarchy) continue; // 비활성 챕터 노드 제외
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
