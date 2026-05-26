using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DevelopmentPanel : MonoBehaviour
{
    private const int TotalChapters    = 4;
    private const int StagesPerChapter = 15;

    // ─────────────────────────────────────────
    // 씬별 섹션
    // ─────────────────────────────────────────

    [Header("씬별 섹션")]
    [SerializeField] private GameObject stageSection;
    [SerializeField] private GameObject lobbySection;

    // ─────────────────────────────────────────
    // Stage Section - Manage Speed
    // ─────────────────────────────────────────

    [Header("Manage Speed")]
    [SerializeField] private TMP_Text speedText;
    [SerializeField] private Button   speedUpButton;
    [SerializeField] private Button   speedDownButton;

    private readonly float[] _speedSteps = { 0.25f, 0.5f, 1f, 2f, 4f };
    private int _currentSpeedIndex = 2;

    // ─────────────────────────────────────────
    // Stage Section - Key Limit
    // ─────────────────────────────────────────

    [Header("Key Limit")]
    [SerializeField] private Button   tabToggleButton;
    [SerializeField] private TMP_Text tabToggleText;

    public static bool IsUnlimitedTab { get; private set; } = false;

    // ─────────────────────────────────────────
    // Stage Section - Manage Mission
    // ─────────────────────────────────────────

    [Header("Manage Mission")]
    [SerializeField] private Button   mission1Button;
    [SerializeField] private Button   mission2Button;
    [SerializeField] private Button   mission3Button;
    [SerializeField] private TMP_Text mission1Text;
    [SerializeField] private TMP_Text mission2Text;
    [SerializeField] private TMP_Text mission3Text;

    // ─────────────────────────────────────────
    // Stage Section - Other Stage
    // ─────────────────────────────────────────

    [Header("Other Stage")]
    [SerializeField] private TMP_Dropdown chapterDropdown;
    [SerializeField] private TMP_Dropdown stageDropdown;
    [SerializeField] private StageLoader  stageLoader;

    // ─────────────────────────────────────────
    // Stage Section - Current Stage
    // ─────────────────────────────────────────

    // Force Clear / Next Stage는 함수만 존재, 별도 레퍼런스 불필요

    // ─────────────────────────────────────────
    // Lobby Section - 범위 해금/잠금
    // ─────────────────────────────────────────

    [Header("범위 해금/잠금")]
    [SerializeField] private TMP_Dropdown rangeStartChapter;
    [SerializeField] private TMP_Dropdown rangeStartStage;
    [SerializeField] private TMP_Dropdown rangeEndChapter;
    [SerializeField] private TMP_Dropdown rangeEndStage;

    // ─────────────────────────────────────────
    // Lobby Section - 미션 올클리어
    // ─────────────────────────────────────────

    [Header("미션 올클리어")]
    [SerializeField] private Toggle clearRangeMission1;
    [SerializeField] private Toggle clearRangeMission2;
    [SerializeField] private Toggle clearRangeMission3;

    // ─────────────────────────────────────────
    // 상태 표시
    // ─────────────────────────────────────────

    [Header("상태 표시")]
    [SerializeField] private TMP_Text   stateDisplayText;
    [SerializeField] private GameObject stateDisplayPanel;

    // ═════════════════════════════════════════
    // Unity Lifecycle
    // ═════════════════════════════════════════

    private void OnEnable()
    {
        if (stageLoader == null)
            stageLoader = FindFirstObjectByType<StageLoader>();

        bool isInGame = stageLoader != null;
        if (stageSection != null) stageSection.SetActive(isInGame);
        if (lobbySection != null) lobbySection.SetActive(!isInGame);

        RefreshSpeedUI();
        RefreshTabUI();
        RefreshMissionUI();
    }

    private void Update()
    {
        UpdateStateDisplay();
    }

    private void OnDisable()
    {
        Time.timeScale      = 1f;
        Time.fixedDeltaTime = 0.02f;
        _currentSpeedIndex  = 2;
    }

    // ═════════════════════════════════════════
    // Manage Speed
    // ═════════════════════════════════════════

    public void SpeedUp()
    {
        _currentSpeedIndex = Mathf.Min(_speedSteps.Length - 1, _currentSpeedIndex + 1);
        ApplySpeed();
    }

    public void SpeedDown()
    {
        _currentSpeedIndex = Mathf.Max(0, _currentSpeedIndex - 1);
        ApplySpeed();
    }

    private void ApplySpeed()
    {
        Time.timeScale      = _speedSteps[_currentSpeedIndex];
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
        RefreshSpeedUI();
    }

    private void RefreshSpeedUI()
    {
        if (speedText != null)
            speedText.text = $"Current Speed : {_speedSteps[_currentSpeedIndex]:G3}";

        if (speedUpButton   != null) speedUpButton.interactable   = _currentSpeedIndex < _speedSteps.Length - 1;
        if (speedDownButton != null) speedDownButton.interactable = _currentSpeedIndex > 0;
    }

    // ═════════════════════════════════════════
    // Key Limit - TAB 무제한
    // ═════════════════════════════════════════

    public void ToggleUnlimitedTab()
    {
        IsUnlimitedTab = !IsUnlimitedTab;
        RefreshTabUI();
    }

    private void RefreshTabUI()
    {
        if (tabToggleText != null)
            tabToggleText.text = IsUnlimitedTab ? "Unlimited\nTAB: ON" : "Unlimited\nTAB Toggle";

        if (tabToggleButton != null)
        {
            var colors = tabToggleButton.colors;
            colors.normalColor = IsUnlimitedTab
                ? new Color(0.2f, 0.8f, 0.2f, 1f)
                : new Color(1f, 1f, 1f, 1f);
            tabToggleButton.colors = colors;
        }
    }

    // ═════════════════════════════════════════
    // Manage Mission
    // ═════════════════════════════════════════

    public void ToggleMission1() => ToggleMission(1);
    public void ToggleMission2() => ToggleMission(2);
    public void ToggleMission3() => ToggleMission(3);

    private void ToggleMission(int index)
    {
        var gm = GameManager.Instance;
        if (gm.currentProgressData == null || gm.currentStageData == null)
        {
            Debug.LogWarning("[Dev] 현재 스테이지 데이터가 없습니다.");
            return;
        }

        var pd = gm.currentProgressData;
        switch (index)
        {
            case 1: pd.isFirstMissionCleared  = !pd.isFirstMissionCleared;  break;
            case 2: pd.isSecondMissionCleared = !pd.isSecondMissionCleared; break;
            case 3: pd.isThirdMissionCleared  = !pd.isThirdMissionCleared;  break;
        }

        gm.jsonDataManager.SaveStageData(pd);
        RefreshMissionUI();
    }

    public void CollectAllStars()
    {
        var player = FindFirstObjectByType<PlayerBehaviour>();
        var tiles  = FindObjectsByType<TileBehaviour>(FindObjectsSortMode.None);
        foreach (var tile in tiles)
        {
            if (tile.currentTileType == TileType.Star && !tile.IsCollected)
                tile.ApplyTileCommand(player);
        }
    }

    private void RefreshMissionUI()
    {
        RefreshSingleMissionUI(1, mission1Text, mission1Button);
        RefreshSingleMissionUI(2, mission2Text, mission2Button);
        RefreshSingleMissionUI(3, mission3Text, mission3Button);
    }

    private void RefreshSingleMissionUI(int index, TMP_Text label, Button btn)
    {
        if (label == null) return;

        var pd = GameManager.Instance.currentProgressData;
        var sd = GameManager.Instance.currentStageData;

        bool cleared = index switch
        {
            1 => pd?.isFirstMissionCleared  ?? false,
            2 => pd?.isSecondMissionCleared ?? false,
            3 => pd?.isThirdMissionCleared  ?? false,
            _ => false
        };

        string typeLabel = index switch
        {
            1 => (sd?.firstMissionType  ?? MissionType.None).ToString(),
            2 => (sd?.secondMissionType ?? MissionType.None).ToString(),
            3 => (sd?.thirdMissionConditions ?? ThirdMissionCondition.None).ToString(),
            _ => "None"
        };

        label.text = $"{index}번째\n[{typeLabel}] {(cleared ? "✓" : "✗")}";

        if (btn != null)
        {
            var colors = btn.colors;
            colors.normalColor = cleared
                ? new Color(0.2f, 0.7f, 0.3f, 1f)
                : new Color(1f, 1f, 1f, 1f);
            btn.colors = colors;
        }
    }

    // ═════════════════════════════════════════
    // Other Stage
    // ═════════════════════════════════════════

    public void MoveToStage()
    {
        if (stageLoader == null) return;
        stageLoader.LoadStage(chapterDropdown.value + 1, stageDropdown.value + 1);
    }

    // ═════════════════════════════════════════
    // Current Stage
    // ═════════════════════════════════════════

    public void ForceClear()
    {
        FindAnyObjectByType<PlayerBehaviour>().ReachedDestination();
    }

    public void NextStage()
    {
        if (stageLoader == null) return;
        var gm = GameManager.Instance;
        if (!stageLoader.LoadStage(gm.chapter, gm.stage + 1))
            stageLoader.LoadStage(gm.chapter + 1, 1);
    }

    // ═════════════════════════════════════════
    // Lobby Section - 범위 해금/잠금
    // ═════════════════════════════════════════

    public void UnlockRange()
    {
        GameManager.Instance.jsonDataManager.UnlockSpecificRange(
            rangeStartChapter.value + 1, rangeStartStage.value + 1,
            rangeEndChapter.value + 1,   rangeEndStage.value + 1,
            StagesPerChapter);
        RefreshStageNodes();
    }

    public void LockRange()
    {
        GameManager.Instance.jsonDataManager.LockSpecificRange(
            rangeStartChapter.value + 1, rangeStartStage.value + 1,
            rangeEndChapter.value + 1,   rangeEndStage.value + 1,
            StagesPerChapter);
        RefreshStageNodes();
    }

    public void UnlockAll()
    {
        GameManager.Instance.jsonDataManager.UnlockAllStages(TotalChapters, StagesPerChapter);
        RefreshStageNodes();
    }

    public void ResetSaveData()
    {
        GameManager.Instance.jsonDataManager.ResetAllData();
        RefreshStageNodes();
    }

    // ═════════════════════════════════════════
    // Lobby Section - 미션 올클리어
    // ═════════════════════════════════════════

    public void ClearRange()
    {
        GameManager.Instance.jsonDataManager.ClearSpecificRange(
            rangeStartChapter.value + 1, rangeStartStage.value + 1,
            rangeEndChapter.value + 1,   rangeEndStage.value + 1,
            StagesPerChapter,
            clearRangeMission1.isOn, clearRangeMission2.isOn, clearRangeMission3.isOn);
        RefreshStageNodes();
    }

    // ═════════════════════════════════════════
    // 공통 유틸
    // ═════════════════════════════════════════

    private void RefreshStageNodes()
    {
        var nodes = FindObjectsByType<StageNode>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var node in nodes)
            node.RefreshVisuals();

        var paths = FindObjectsByType<StagePathRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var path in paths)
            path.Refresh();

        // 현재 스테이지가 비활성화됐다면 isAppeared인 최고 스테이지로 포커스 이동
        FindFirstObjectByType<StageSelectManagement>()?.TryFocusBestAvailableNode();
    }

    // ═════════════════════════════════════════
    // 상태 표시
    // ═════════════════════════════════════════

    private void UpdateStateDisplay()
{
    if (stateDisplayText == null) return;

    var gm = GameManager.Instance;
    if (gm == null) { stateDisplayText.text = "GameManager 없음"; return; }

    var player      = FindFirstObjectByType<PlayerBehaviour>();
    var mapManager  = FindFirstObjectByType<MapManager>();
    var behaviourManager = FindFirstObjectByType<BehaviourManager>();
    var sd = gm.currentStageData;
    var pd = gm.currentProgressData;

    // ── 플레이어 ──
    string playerLayer = player != null ? LayerMask.LayerToName(player.gameObject.layer) : "N/A";
    string playerPos   = player != null
        ? $"({player.transform.position.x:F1}, {player.transform.position.y:F1})"
        : "N/A";
    string iceState    = player != null && player.IsOnIce() ? " [ICE]" : "";

    // ── 카운트 ──
    string map1Count = player != null
        ? $"Move:{player.map1MoveCount} Rot:{player.map1RotationCount}"
        : "N/A";
    string map2Count = player != null
        ? $"Move:{player.map2MoveCount} Rot:{player.map2RotationCount}"
        : "N/A";

    // ── 미션 ──
    string m1 = pd?.isFirstMissionCleared  == true ? "✓" : "✗";
    string m2 = pd?.isSecondMissionCleared == true ? "✓" : "✗";
    string m3 = pd?.isThirdMissionCleared  == true ? "✓" : "✗";

    // ── 스테이지 제한 ──
    string keyLimit = sd != null
        ? $"ALT:{(sd.canUseLeftALT ? $"{gm.pushedNumberALT}/{(sd.limitNumberALT > 0 ? sd.limitNumberALT.ToString() : "∞")}" : "X")}  " +
          $"F4:{(sd.canUseF4       ? $"{gm.pushedNumberF4}/{(sd.limitNumberF4   > 0 ? sd.limitNumberF4.ToString()   : "∞")}" : "X")}  " +
          $"TAB:{(sd.canUseTAB     ? $"{gm.pushedNumberTAB}/{(sd.limitNumberTAB > 0 ? sd.limitNumberTAB.ToString() : "∞")}" : "X")}"
        : "N/A";

    // ── 진행 기록 ──
    string record = pd != null
        ? $"시도:{pd.attemptCount}  이탈:{pd.abandonCount}  " +
          $"최단:{(pd.minClearTime < float.MaxValue ? $"{pd.minClearTime:F1}s" : "-")}  " +
          $"총플레이:{pd.totalPlayTime:F0}s"
        : "N/A";

    // ── 최소 키 기록 ──
    string minKeys = pd != null
        ? $"minALT:{(pd.minALT < int.MaxValue ? pd.minALT.ToString() : "-")}  " +
          $"minF4:{(pd.minF4   < int.MaxValue ? pd.minF4.ToString()  : "-")}  " +
          $"minTAB:{(pd.minTAB < int.MaxValue ? pd.minTAB.ToString() : "-")}"
        : "N/A";

    // ── 게임 상태 플래그 ──
    string flags = "";
    if (gm.isGameOver) flags += "[DEAD] ";
    if (gm.isCleared)  flags += "[CLEAR] ";
    if (gm.isPaused)   flags += "[PAUSE] ";
    if (gm.isOption)   flags += "[OPTION] ";
    if (gm.isChatting) flags += "[CHAT] ";
    if (string.IsNullOrEmpty(flags)) flags = "-";

    // ── 턴 상태 ──
    string turnState = behaviourManager != null
        ? behaviourManager.currentTurn.ToString()
        : "N/A";

    // ── 맵 상태 ──
    string mapState = mapManager != null
        ? $"{(mapManager.IsFirstRoot() ? "Map 1" : "Map 2")}{(mapManager.IsRotating ? " [ROTATING]" : "")}"
        : "N/A";

    stateDisplayText.text =
        $"━━ STAGE ━━━━━━━━━━━━━━━━━━━\n" +
        $"Stage : {gm.chapter}-{gm.stage}  Time : {gm.currentTime:F1}s\n" +
        $"Flags : {flags}\n" +
        $"Turn  : {turnState}  Map : {mapState}\n" +
        $"\n━━ KEY ━━━━━━━━━━━━━━━━━━━━━\n" +
        $"{keyLimit}\n" +
        $"Tab∞ : {(IsUnlimitedTab ? "ON" : "OFF")}\n" +
        $"\n━━ PLAYER ━━━━━━━━━━━━━━━━━━\n" +
        $"Layer : {playerLayer}  Pos : {playerPos}{iceState}\n" +
        $"Map1  : {map1Count}\n" +
        $"Map2  : {map2Count}\n" +
        $"Total : Move:{player?.TotalMoveCount ?? 0} Rot:{player?.TotalRotationCount ?? 0} Act:{player?.TotalActionCount ?? 0}\n" +
        $"\n━━ MISSION ━━━━━━━━━━━━━━━━━\n" +
        $"1:{m1}  2:{m2}  3:{m3}\n" +
        $"\n━━ RECORD ━━━━━━━━━━━━━━━━━━\n" +
        $"{record}\n" +
        $"{minKeys}\n" +
        $"\n━━ SYSTEM ━━━━━━━━━━━━━━━━━━\n" +
        $"Speed : x{Time.timeScale:G3}  FPS : {(1f / Time.unscaledDeltaTime):F0}";
}

    public void ToggleStateDisplay()
    {
        if (stateDisplayPanel != null)
            stateDisplayPanel.SetActive(!stateDisplayPanel.activeSelf);
    }
}