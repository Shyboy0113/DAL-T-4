using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DevelopmentPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private StageLoader stageLoader;

    [Header("스테이지 선택")]
    [SerializeField] private TMP_Dropdown chapterDropdown;
    [SerializeField] private TMP_Dropdown stageDropdown;

    [Header("씬별 섹션")]
    [SerializeField] private GameObject stageSection;
    [SerializeField] private GameObject lobbySection;

    // ─────────────────────────────────────────
    // Undo 무제한
    // ─────────────────────────────────────────

    [Header("Undo 무제한")]
    [SerializeField] private Button undoToggleButton;
    [SerializeField] private TMP_Text undoToggleText;

    /// <summary>
    /// 다른 시스템에서 DevPanel.IsUnlimitedUndo 로 체크.
    /// Undo 횟수 제한 로직에서 이 값이 true이면 제한을 무시하도록 사용.
    /// </summary>
    public static bool IsUnlimitedUndo { get; private set; } = false;

    // ─────────────────────────────────────────
    // 미션 강제 클리어 / 리셋
    // ─────────────────────────────────────────

    [Header("미션 제어")]
    [SerializeField] private Button mission1Button;
    [SerializeField] private Button mission2Button;
    [SerializeField] private Button mission3Button;
    [SerializeField] private TMP_Text mission1Text;
    [SerializeField] private TMP_Text mission2Text;
    [SerializeField] private TMP_Text mission3Text;

    // ─────────────────────────────────────────
    // 속도 조절
    // ─────────────────────────────────────────

    [Header("속도 조절")]
    [SerializeField] private Button speedDownButton;
    [SerializeField] private Button speedUpButton;
    [SerializeField] private TMP_Text speedText;

    private readonly float[] _speedSteps = { 0.25f, 0.5f, 1f, 2f, 4f };
    private int _currentSpeedIndex = 2; // 기본값 1x

    // ─────────────────────────────────────────
    // 상태 표시
    // ─────────────────────────────────────────

    [Header("상태 표시")]
    [SerializeField] private TMP_Text stateDisplayText;
    [SerializeField] private GameObject stateDisplayPanel;

    // ─────────────────────────────────────────
    // Unity Lifecycle
    // ─────────────────────────────────────────

    private void OnEnable()
    {
        if (stageLoader == null)
            stageLoader = FindFirstObjectByType<StageLoader>();

        bool isInGame = stageLoader != null;

        if (stageSection != null) stageSection.SetActive(isInGame);
        if (lobbySection != null) lobbySection.SetActive(!isInGame);

        RefreshUndoUI();
        RefreshMissionUI();
        RefreshSpeedUI();
    }

    private void Update()
    {
        UpdateStateDisplay();
    }

    private void OnDisable()
    {
        // 패널을 닫을 때 속도를 1x로 복원 (의도치 않은 속도 유지 방지)
        // 필요 없으면 이 블록 제거
        // Time.timeScale = 1f;
        // _currentSpeedIndex = 2;
    }

    // ═════════════════════════════════════════
    // 기존 기능
    // ═════════════════════════════════════════

    public void MoveToStage()
    {
        if (stageLoader == null) return;
        stageLoader.LoadStage(chapterDropdown.value + 1, stageDropdown.value + 1);
    }

    public void ForceClear()
    {
        GameManager.Instance.GameClear();
    }

    public void CollectAllStars()
    {
        var player = FindFirstObjectByType<PlayerBehaviour>();
        var tiles = FindObjectsByType<TileBehaviour>(FindObjectsSortMode.None);
        foreach (var tile in tiles)
        {
            if (tile.currentTileType == TileType.Star && !tile.IsCollected)
                tile.ApplyTileCommand(player);
        }
    }

    public void ResetTimer()
    {
        GameManager.Instance.currentTime = 0f;
    }

    public void NextStage()
    {
        var gm = GameManager.Instance;
        if (!stageLoader.LoadStage(gm.chapter, gm.stage + 1))
            stageLoader.LoadStage(gm.chapter + 1, 1);
    }

    public void PrevStage()
    {
        var gm = GameManager.Instance;
        if (gm.stage > 1)
            stageLoader.LoadStage(gm.chapter, gm.stage - 1);
        else if (gm.chapter > 1)
            stageLoader.LoadStage(gm.chapter - 1, 1);
    }

    public void UnlockAll()
    {
        GameManager.Instance.jsonDataManager.UnlockAllStages(5, 10);
    }

    public void ResetSaveData()
    {
        GameManager.Instance.jsonDataManager.ResetAllData();
    }

    // ═════════════════════════════════════════
    // 1. Undo 무제한 토글
    // ═════════════════════════════════════════

    /// <summary>
    /// 버튼 OnClick에 연결.
    /// Undo 횟수 제한 로직에서 다음과 같이 사용:
    /// <code>
    /// if (DevelopmentPanel.IsUnlimitedUndo || currentUndoCount < maxUndo)
    ///     PerformUndo();
    /// </code>
    /// </summary>
    public void ToggleUnlimitedUndo()
    {
        IsUnlimitedUndo = !IsUnlimitedUndo;
        RefreshUndoUI();
        Debug.Log($"[Dev] Undo 무제한: {IsUnlimitedUndo}");
    }

    private void RefreshUndoUI()
    {
        if (undoToggleText != null)
            undoToggleText.text = IsUnlimitedUndo ? "Undo 무제한: ON" : "Undo 무제한: OFF";

        if (undoToggleButton != null)
        {
            var colors = undoToggleButton.colors;
            colors.normalColor = IsUnlimitedUndo
                ? new Color(0.2f, 0.8f, 0.2f, 1f)   // 초록
                : new Color(0.8f, 0.2f, 0.2f, 1f);   // 빨강
            undoToggleButton.colors = colors;
        }
    }

    // ═════════════════════════════════════════
    // 2. 미션 강제 클리어 / 리셋
    // ═════════════════════════════════════════

    /// <summary>
    /// 미션 1/2/3 버튼 OnClick에 각각 연결.
    /// 누를 때마다 해당 미션의 클리어 상태를 토글한다.
    /// </summary>
    public void ToggleMission1() => ToggleMission(1);
    public void ToggleMission2() => ToggleMission(2);
    public void ToggleMission3() => ToggleMission(3);

    private void ToggleMission(int missionIndex)
    {
        var gm = GameManager.Instance;
        if (gm.currentProgressData == null || gm.currentStageData == null)
        {
            Debug.LogWarning("[Dev] 현재 스테이지 데이터가 없습니다.");
            return;
        }

        var pd = gm.currentProgressData;

        switch (missionIndex)
        {
            case 1: pd.isFirstMissionCleared  = !pd.isFirstMissionCleared;  break;
            case 2: pd.isSecondMissionCleared = !pd.isSecondMissionCleared; break;
            case 3: pd.isThirdMissionCleared  = !pd.isThirdMissionCleared;  break;
        }

        // 즉시 세이브
        gm.jsonDataManager.SaveStageData(pd);
        RefreshMissionUI();

        Debug.Log($"[Dev] 미션{missionIndex} → {GetMissionCleared(missionIndex)}");
    }

    private bool GetMissionCleared(int index)
    {
        var pd = GameManager.Instance.currentProgressData;
        if (pd == null) return false;
        return index switch
        {
            1 => pd.isFirstMissionCleared,
            2 => pd.isSecondMissionCleared,
            3 => pd.isThirdMissionCleared,
            _ => false
        };
    }

    private string GetMissionTypeName(int index)
    {
        var sd = GameManager.Instance.currentStageData;
        if (sd == null) return "???";
        MissionType type = index switch
        {
            1 => sd.firstMissionType,
            2 => sd.secondMissionType,
            3 => sd.thirdMissionType,
            _ => MissionType.StageClear
        };
        return type.ToString();
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

        bool cleared = GetMissionCleared(index);
        string typeName = GetMissionTypeName(index);
        string state = cleared ? "✓" : "✗";

        label.text = $"미션{index} [{typeName}] {state}";

        if (btn != null)
        {
            var colors = btn.colors;
            colors.normalColor = cleared
                ? new Color(0.2f, 0.7f, 0.3f, 1f)
                : new Color(0.5f, 0.5f, 0.5f, 1f);
            btn.colors = colors;
        }
    }

    // ═════════════════════════════════════════
    // 3. 속도 조절
    // ═════════════════════════════════════════

    /// <summary>
    /// 속도 감소/증가 버튼 OnClick에 연결.
    /// </summary>
    public void SpeedDown()
    {
        _currentSpeedIndex = Mathf.Max(0, _currentSpeedIndex - 1);
        ApplySpeed();
    }

    public void SpeedUp()
    {
        _currentSpeedIndex = Mathf.Min(_speedSteps.Length - 1, _currentSpeedIndex + 1);
        ApplySpeed();
    }

    public void ResetSpeed()
    {
        _currentSpeedIndex = 2; // 1x
        ApplySpeed();
    }

    private void ApplySpeed()
    {
        Time.timeScale = _speedSteps[_currentSpeedIndex];
        // fixedDeltaTime도 비례 조정 (물리 정확도 유지)
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
        RefreshSpeedUI();
        Debug.Log($"[Dev] 게임 속도: {_speedSteps[_currentSpeedIndex]}x");
    }

    private void RefreshSpeedUI()
    {
        if (speedText != null)
            speedText.text = $"x{_speedSteps[_currentSpeedIndex]:G3}";

        if (speedDownButton != null)
            speedDownButton.interactable = _currentSpeedIndex > 0;
        if (speedUpButton != null)
            speedUpButton.interactable = _currentSpeedIndex < _speedSteps.Length - 1;
    }

    // ═════════════════════════════════════════
    // 4. 현재 상태 표시
    // ═════════════════════════════════════════

    private void UpdateStateDisplay()
    {
        if (stateDisplayText == null) return;

        var gm = GameManager.Instance;
        if (gm == null)
        {
            stateDisplayText.text = "GameManager 없음";
            return;
        }

        var player = FindFirstObjectByType<PlayerBehaviour>();
        string playerLayer = player != null
            ? LayerMask.LayerToName(player.gameObject.layer)
            : "N/A";
        string playerPos = player != null
            ? $"({player.transform.position.x:F1}, {player.transform.position.y:F1})"
            : "N/A";

        // 미션 상태 간결 표시
        string m1 = gm.currentProgressData?.isFirstMissionCleared  == true ? "✓" : "✗";
        string m2 = gm.currentProgressData?.isSecondMissionCleared == true ? "✓" : "✗";
        string m3 = gm.currentProgressData?.isThirdMissionCleared  == true ? "✓" : "✗";

        // 게임 상태 플래그
        string flags = "";
        if (gm.isGameOver) flags += "[DEAD] ";
        if (gm.isCleared)  flags += "[CLEAR] ";
        if (gm.isPaused)   flags += "[PAUSE] ";

        stateDisplayText.text =
            $"Stage: {gm.chapter}-{gm.stage}  {flags}\n" +
            $"Time: {gm.currentTime:F1}s  Speed: x{Time.timeScale:G3}\n" +
            $"ALT: {gm.pushedNumberALT}  F4: {gm.pushedNumberF4}  TAB: {gm.pushedNumberTAB}\n" +
            $"Mission: {m1} {m2} {m3}\n" +
            $"Player: {playerLayer} {playerPos}\n" +
            $"Undo∞: {(IsUnlimitedUndo ? "ON" : "OFF")}  FPS: {(1f / Time.unscaledDeltaTime):F0}";
    }

    /// <summary>
    /// 상태 표시 패널 토글 (항상 보이게 하거나 숨기기).
    /// 개발자 패널이 닫혀있어도 상태 표시만 띄울 수 있도록 분리.
    /// </summary>
    public void ToggleStateDisplay()
    {
        if (stateDisplayPanel != null)
            stateDisplayPanel.SetActive(!stateDisplayPanel.activeSelf);
    }
}