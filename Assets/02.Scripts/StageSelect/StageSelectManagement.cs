using System;
using UnityEngine;
using UnityEngine.EventSystems;
using Eflatun.SceneReference;
using System.Collections;
using System.Linq;
using UnityEngine.UI;


/// <summary>
/// 스테이지 셀렉트 씬의 진입점.
/// 초기 포커스, StageNode 이벤트 수신, 패널 관리, 씬 전환, ESC 처리를 중앙에서 담당합니다.
///
/// 패널 상태를 _panelOpenNode 단일 변수로 관리하여,
/// 기존 StageNode 인스턴스별 _isPanelOpen 분산 관리에서 발생하던
/// 상태 불일치 / DOTween 충돌 / 동일 프레임 Select+Click 경합 버그를 해결합니다.
/// </summary>
public class StageSelectManagement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CutoutFade        cutoutFade;
    [SerializeField] private StageSelectPlayer selectPlayer;
    [SerializeField] private SceneReference    gameScene;
    [SerializeField] private SceneReference    introScene;
    [SerializeField] private SceneReference    stageSelectScene;

    /// <summary>씬 시작 시 포커스를 맞출 첫 번째 노드</summary>
    [SerializeField] private StageNode defaultNode;

    [Header("Chapter Navigation")]
    [SerializeField] private GameObject[] chapters;
    [SerializeField] private GameObject   returnButton;

    private bool        _isTransitioning = false;
    private int         _currentChapter  = 0;
    private StageNode[] _allNodes;

    // ── 패널 상태 (중앙 관리) ──────────────────────────────────
    /// <summary>현재 패널이 열려 있는 노드. null이면 패널 닫힘.</summary>
    private StageNode _panelOpenNode = null;

    /// <summary>현재 StageSelect에서 포커스(선택)된 노드.</summary>
    private StageNode _currentFocusNode = null;

    // ═══════════════════════════════════════════════════════════
    // Lifecycle
    // ═══════════════════════════════════════════════════════════

    private void Start()
{
    // 1. 씬 내의 모든 노드 수집 (비활성 상태 포함)
    _allNodes = FindObjectsByType<StageNode>(FindObjectsInactive.Include, FindObjectsSortMode.None);

    // 2. GameManager에서 직전 플레이 정보 가져오기
    int lastChapter = GameManager.Instance.chapter;
    int lastStage = GameManager.Instance.stage;

    StageNode targetNode = null;

    // 만약 저장된 정보가 있다면 해당 노드를 검색
    if (lastChapter > 0 && lastStage > 0)
    {
        targetNode = _allNodes.FirstOrDefault(n => 
            n.stageData != null && 
            n.stageData.chapterNum == lastChapter && 
            n.stageData.stageNum == lastStage);
    }

    // 3. 현재 챕터 인덱스 설정
    if (targetNode != null)
    {
        // 데이터는 1번부터 시작하지만 배열은 0번부터이므로 -1
        _currentChapter = Mathf.Clamp(lastChapter - 1, 0, chapters.Length - 1);
    }
    else
    {
        // 데이터가 없으면 기존 방식대로 활성화된 챕터 찾기
        _currentChapter = 0;
        for (int i = 0; i < chapters.Length; i++)
        {
            if (chapters[i] != null && chapters[i].activeSelf) { _currentChapter = i; break; }
        }
    }

    // 4. 결정된 챕터만 활성화
    for (int i = 0; i < chapters.Length; i++)
    {
        if (chapters[i] != null)
            chapters[i].SetActive(i == _currentChapter);
    }

    // 5. 노드 이벤트 구독 및 비주얼 동기화
    SubscribeAllNodes();

    // 6. 포커스 타겟 최종 결정
    // 직전 노드 -> 에디터에서 설정한 기본 노드 -> 해금된 첫 노드 순서
    StageNode firstFocus = targetNode;
    if (firstFocus == null || !firstFocus.gameObject.activeInHierarchy)
    {
        firstFocus = (defaultNode != null && defaultNode.gameObject.activeInHierarchy)
            ? defaultNode
            : FindFirstAvailableNode();
    }

    // 7. 포커스 적용 및 캐릭터 이동
    if (firstFocus != null)
    {
        EventSystem.current.SetSelectedGameObject(firstFocus.gameObject);
        // 즉시 이동을 위해 SnapTo 사용
        selectPlayer?.SnapTo(firstFocus.GetComponent<RectTransform>());
    }

    // 8. 입장 연출
    selectPlayer.Lock();
    _isTransitioning = true;
    cutoutFade.FadeIn(() =>
    {
        selectPlayer.Unlock();
        _isTransitioning = false;
    });
}

    private void OnEnable()
    {
        GameEvents.OnSaveDataChanged+=RefreshAllVisuals;
    }

    private void OnDisable()
    {
        GameEvents.OnSaveDataChanged-=RefreshAllVisuals;
    }

    private void OnDestroy()
    {
        UnsubscribeAllNodes();
    }

    private void Update()
    {
        if (_isTransitioning) return;

        if (Input.GetKeyDown(KeyCode.Q)) TrySwitchChapter(-1);
        if (Input.GetKeyDown(KeyCode.E)) TrySwitchChapter(+1);

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_panelOpenNode != null)
                ClosePanel();
            else
                ReturnToMenu();
        }
    }

    // ═══════════════════════════════════════════════════════════
    // 노드 이벤트 구독 / 해제
    // ═══════════════════════════════════════════════════════════

    private void SubscribeAllNodes()
    {
        foreach (var node in _allNodes)
        {
            if (node == null) continue;
            node.RefreshVisuals();
            node.OnConfirmed  -= OnNodeConfirmed;
            node.OnSelected   -= OnNodeSelected;
            node.OnDeselected -= OnNodeDeselected;
            node.OnConfirmed  += OnNodeConfirmed;
            node.OnSelected   += OnNodeSelected;
            node.OnDeselected += OnNodeDeselected;
        }
    }

    private void UnsubscribeAllNodes()
    {
        if (_allNodes == null) return;
        foreach (var node in _allNodes)
        {
            if (node == null) continue;
            node.OnConfirmed  -= OnNodeConfirmed;
            node.OnSelected   -= OnNodeSelected;
            node.OnDeselected -= OnNodeDeselected;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // 노드 이벤트 핸들러
    // ═══════════════════════════════════════════════════════════

    private void OnNodeSelected(StageNode node)
    {
        _currentFocusNode = node;

        // 다른 노드가 선택되면 열려있던 패널을 닫는다
        if (_panelOpenNode != null && _panelOpenNode != node)
            ClosePanel();

        // 플레이어 캐릭터를 해당 노드로 이동
        selectPlayer?.MoveTo(node.GetComponent<RectTransform>());
    }

    private void OnNodeDeselected(StageNode node)
    {
        // 패널 닫기는 OnNodeSelected(다른 노드)와 ESC에서만 처리.
        // 포커스 이동으로 Deselect가 발동되면 패널이 즉시 닫히는 문제가 생긴다.
    }

    private void OnNodeConfirmed(StageNode node)
    {
        Debug.Log($"[Management] OnNodeConfirmed | node={node.name} | isTransitioning={_isTransitioning} | isLocked={selectPlayer?.IsLocked} | panelOpenNode={_panelOpenNode?.name ?? "null"}");
        
        if (_isTransitioning) return;
        if (selectPlayer != null && selectPlayer.IsLocked) return;

        if (_panelOpenNode != node)
        {
            Debug.Log($"[Management] → 패널 열기");
            // ── 패널 열기 ──
            ClosePanel();
            node.ShowPanel();
            _panelOpenNode = node;
        }
        else
        {
            Debug.Log($"[Management] → EnterStage 진입");
            // ── 스테이지 진입 ──
            ClosePanel();
            EnterStage(node);
        }
    }

    // ═══════════════════════════════════════════════════════════
    // 패널 닫기 (중앙 관리)
    // ═══════════════════════════════════════════════════════════

    private void ClosePanel()
    {
        if (_panelOpenNode == null) return;

        _panelOpenNode.HidePanel();
        _panelOpenNode = null;
    }

    // ═══════════════════════════════════════════════════════════
    // 스테이지 진입
    // ═══════════════════════════════════════════════════════════

    private void EnterStage(StageNode node)
    {
        if (node?.stageData == null) return;
        _isTransitioning = true;

        selectPlayer?.PlayEnterSound();
        GameEvents.RaiseTeleportTriggered();

        EventSystem.current.SetSelectedGameObject(returnButton);
        if (returnButton != null)
            returnButton.GetComponent<Button>().interactable = false;

        selectPlayer.Lock();

        GameManager.Instance.chapter = node.stageData.chapterNum;
        GameManager.Instance.stage   = node.stageData.stageNum;

        StartCoroutine(IFadeOut(1.5f));
    }

    // ═══════════════════════════════════════════════════════════
    // 씬 전환
    // ═══════════════════════════════════════════════════════════

    public void ReturnToMenu()
    {
        if (_isTransitioning) return;
        _isTransitioning = true;

        ClosePanel();
        selectPlayer?.Lock();
        cutoutFade.FadeOut(() =>
            StartCoroutine(SceneLoader.LoadScene(introScene)));
    }

    public void ReloadStageSelectScene()
    {
        ClosePanel();
        EventSystem.current.SetSelectedGameObject(null);
        selectPlayer?.Lock();

        cutoutFade.FadeOut(() =>
            StartCoroutine(SceneLoader.LoadScene(stageSelectScene)));
    }

    // ═══════════════════════════════════════════════════════════
    // 챕터 전환
    // ═══════════════════════════════════════════════════════════

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
        if (_isTransitioning) return;
        
        int target = _currentChapter + delta;
        if (target < 0 || target >= chapters.Length) return;
        if (delta > 0 && !CanEnterChapter(target)) return;

        _isTransitioning = true;
        ClosePanel();
        EventSystem.current.SetSelectedGameObject(returnButton);
        selectPlayer.Lock();

        cutoutFade.FadeOut(() =>
        {
            chapters[_currentChapter].SetActive(false);
            _currentChapter = target;
            chapters[_currentChapter].SetActive(true);

            // 챕터 전환 시 노드 재수집
            UnsubscribeAllNodes();
            _allNodes = FindObjectsByType<StageNode>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            SubscribeAllNodes();

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
                if (firstFocus != null && firstFocus.gameObject.activeInHierarchy)
                    EventSystem.current.SetSelectedGameObject(firstFocus.gameObject);

                selectPlayer.Unlock();
                _isTransitioning = false;
            });
        });
    }

    // ═══════════════════════════════════════════════════════════
    // 유틸
    // ═══════════════════════════════════════════════════════════

    private StageNode FindFirstAvailableNode()
    {
        StageNode best = null;
        foreach (var node in _allNodes)
        {
            if (node == null) continue;
            if (!node.gameObject.activeInHierarchy) continue;
            if (!node.CanEnter()) continue;
            if (best == null) { best = node; continue; }

            bool earlierChapter = node.stageData.chapterNum < best.stageData.chapterNum;
            bool sameChapterEarlierStage = node.stageData.chapterNum == best.stageData.chapterNum &&
                                           node.stageData.stageNum   <  best.stageData.stageNum;
            if (earlierChapter || sameChapterEarlierStage) best = node;
        }
        return best;
    }

    private IEnumerator IFadeOut(float time)
    {
        yield return new WaitForSeconds(time);
        cutoutFade.FadeOut(() => StartCoroutine(SceneLoader.LoadScene(gameScene)));
    }

    /// <summary>
    /// 현재 스테이지가 비활성화(isAppeared=false)됐을 때,
    /// isAppeared인 노드 중 가장 높은 챕터·스테이지로 즉시 포커스를 이동합니다.
    /// 개발자 패널에서 저장 데이터를 변경한 뒤 호출합니다.
    /// </summary>
    public void TryFocusBestAvailableNode()
    {
        var gm  = GameManager.Instance;
        var jdm = gm?.jsonDataManager;
        if (jdm == null) return;

        // _currentFocusNode : StageSelect에서 마지막으로 OnSelect된 노드.
        // gm.chapter/stage는 마지막으로 '실제 입장'한 스테이지이므로
        // Q/E 챕터 전환 후 다른 챕터 노드를 보고 있을 때 틀린 값을 가리킬 수 있음.
        if (_currentFocusNode != null && _currentFocusNode.stageData != null)
        {
            var pd = jdm.GetStageData(_currentFocusNode.stageData.chapterNum,
                                      _currentFocusNode.stageData.stageNum);
            if (pd != null && pd.isAppeared) return;
        }
        else
        {
            // 포커스 노드 정보가 없으면 gm.chapter/stage로 폴백
            var pd = jdm.GetStageData(gm.chapter, gm.stage);
            if (pd != null && pd.isAppeared) return;
        }

        // 전체 노드(비활성 챕터 포함) 중 isAppeared인 최고 스테이지 탐색
        var allNodes = FindObjectsByType<StageNode>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        StageNode best = null;
        foreach (var node in allNodes)
        {
            if (node == null || node.stageData == null) continue;
            var pd = jdm.GetStageData(node.stageData.chapterNum, node.stageData.stageNum);
            if (pd == null || !pd.isAppeared) continue;

            if (best == null) { best = node; continue; }

            bool higherCh     = node.stageData.chapterNum > best.stageData.chapterNum;
            bool sameChHigher = node.stageData.chapterNum == best.stageData.chapterNum
                             && node.stageData.stageNum   >  best.stageData.stageNum;
            if (higherCh || sameChHigher) best = node;
        }

        if (best == null) return;

        // 챕터가 다르면 애니메이션 없이 즉시 전환 (개발자 도구용)
        int targetChIdx = Mathf.Clamp(best.stageData.chapterNum - 1, 0, chapters.Length - 1);
        if (targetChIdx != _currentChapter)
        {
            if (chapters[_currentChapter] != null) chapters[_currentChapter].SetActive(false);
            _currentChapter = targetChIdx;
            if (chapters[_currentChapter] != null) chapters[_currentChapter].SetActive(true);

            UnsubscribeAllNodes();
            _allNodes = FindObjectsByType<StageNode>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            SubscribeAllNodes();
        }

        // GameManager 상태 갱신
        gm.chapter = best.stageData.chapterNum;
        gm.stage   = best.stageData.stageNum;

        // 포커스·플레이어 이동
        EventSystem.current?.SetSelectedGameObject(best.gameObject);
        selectPlayer?.SnapTo(best.GetComponent<RectTransform>());
    }
    
    private void RefreshAllVisuals()
    {
        // 노드 먼저, 선 나중
        foreach (var node in _allNodes)
        {
            if (node == null) continue;
            node.RefreshVisuals();
        }

        var paths = FindObjectsByType<StagePathRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var path in paths)
            path.Refresh();
    }
}