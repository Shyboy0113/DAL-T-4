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

    [Header("Chapter Navigation")]
    [SerializeField] private GameObject[] chapters;
    [SerializeField] private GameObject   returnButton;

    private bool        _isTransitioning = false;
    private int         _currentChapter  = 0;
    private StageNode[] _allNodes;

    private void Start()
    {
        _allNodes = FindObjectsByType<StageNode>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        // 현재 활성화된 챕터 인덱스 초기화
        _currentChapter = 0;
        for (int i = 0; i < chapters.Length; i++)
        {
            if (chapters[i] != null && chapters[i].activeSelf) { _currentChapter = i; break; }
        }

        // 현재 챕터만 활성화, 나머지는 명시적으로 비활성화
        for (int i = 0; i < chapters.Length; i++)
        {
            if (chapters[i] != null)
                chapters[i].SetActive(i == _currentChapter);
        }

        // 세이브 데이터 기준으로 모든 노드 시각 갱신 + 이벤트 구독
        foreach (var node in _allNodes)
        {
            node.RefreshVisuals();
            node.OnConfirmed += OnNodeConfirmed;
        }

        // 초기 포커스 설정
        // defaultNode가 없으면 현재 챕터 내에서 진입 가능한 가장 빠른 노드를 찾음
        // activeInHierarchy 검사: defaultNode가 비활성 챕터에 속할 경우 대비
        StageNode firstFocus = (defaultNode != null && defaultNode.gameObject.activeInHierarchy)
            ? defaultNode
            : FindFirstAvailableNode();
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

        if (Input.GetKeyDown(KeyCode.Q)) TrySwitchChapter(-1);
        if (Input.GetKeyDown(KeyCode.E)) TrySwitchChapter(+1);

        if (Input.GetKeyDown(KeyCode.Escape))
            ReturnToMenu();
    }

    private void OnNodeConfirmed(StageNode node)
    {
        if (_isTransitioning || node?.stageData == null) return;
        _isTransitioning = true;

        // null 대신 Return Button — null이면 FocusKeeper가 즉시 _lastSelectedObject로 복구해 OnSelect 재발화함
        EventSystem.current.SetSelectedGameObject(returnButton);
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

    private bool HasAnyAvailableNodeInChapter(int chapterIndex)
    {
        int chapterNum = chapterIndex + 1;
        foreach (var node in _allNodes)
        {
            if (node?.stageData == null) continue;
            if (node.stageData.chapterNum == chapterNum && node.CanEnter()) return true;
        }
        return false;
    }

    private void TrySwitchChapter(int delta)
    {
        int target = _currentChapter + delta;
        if (target < 0 || target >= chapters.Length) return;

        // 다음 챕터(앞으로 이동)에 진입 가능한 스테이지가 없으면 차단
        if (delta > 0 && !HasAnyAvailableNodeInChapter(target)) return;

        _isTransitioning = true;
        EventSystem.current.SetSelectedGameObject(returnButton);
        selectPlayer.Lock();

        cutoutFade.FadeOut(() =>
        {
            chapters[_currentChapter].SetActive(false);
            _currentChapter = target;
            chapters[_currentChapter].SetActive(true);
            // CanvasEventSystemFocusKeeper.OnEnable()이 포커스를 자동 처리함

            // 새 챕터 활성화 직후 비주얼 갱신 (StageNode.Start()는 다음 프레임 실행이므로 명시적 호출)
            foreach (var node in _allNodes)
            {
                if (node.gameObject.activeInHierarchy)
                    node.RefreshVisuals();
            }
            
            if (returnButton != null)
                EventSystem.current.SetSelectedGameObject(returnButton);

            cutoutFade.FadeIn(() =>
            {
                // 챕터 전환 후 기본 포커스는 Return Button — 새 챕터의 stage 1이 잠겨있을 수 있으므로
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
