using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// 키 사용 한도(ALT/F4/TAB), 적 처치 현황, 제한 시간, 전체 행동 횟수를 표시하는 HUD 컴포넌트.
/// </summary>
public class KeyLimitUI : MonoBehaviour
{
    [Header("ALT 사용 한도 (limitNumberALT > 0 일 때만 활성)")]
    [SerializeField] private GameObject altDisplay;
    [SerializeField] private TMP_Text   altText;

    [Header("F4 사용 한도 (limitNumberF4 > 0 일 때만 활성)")]
    [SerializeField] private GameObject f4Display;
    [SerializeField] private TMP_Text   f4Text;

    [Header("TAB 사용 한도 (limitNumberTAB > 0 일 때만 활성)")]
    [SerializeField] private GameObject tabDisplay;
    [SerializeField] private TMP_Text   tabText;

    [Header("적 처치 현황 (적이 존재할 때만 활성)")]
    [SerializeField] private GameObject killDisplay;
    [SerializeField] private TMP_Text   killText;

    [Header("제한 시간 (limitTime > 0 일 때만 활성)")]
    [SerializeField] private GameObject timeDisplay;
    [SerializeField] private TMP_Text   timeText;
    [SerializeField] private float      timeRefreshInterval = 0.1f;

    [Header("전체 행동 횟수")]
    [SerializeField] private GameObject actionCountDisplay;
    [SerializeField] private TMP_Text   actionCountText;

    private float _timeTimer;

    private void OnEnable()
    {
        GameEvents.KeyUsed        += OnKeyUsed;
        GameEvents.UndoTriggered  += Refresh;
        GameEvents.StageRestarted += Refresh;
        GameEvents.EnemyDied      += Refresh;
    }

    private void OnDisable()
    {
        GameEvents.KeyUsed        -= OnKeyUsed;
        GameEvents.UndoTriggered  -= Refresh;
        GameEvents.StageRestarted -= Refresh;
        GameEvents.EnemyDied      -= Refresh;
    }

    private IEnumerator Start()
    {
        yield return new WaitUntil(() => GameManager.Instance?.currentStageData != null);
        Refresh();
    }

    private void Update()
    {
        if (timeDisplay == null || !timeDisplay.activeSelf) return;
        _timeTimer += Time.deltaTime;
        if (_timeTimer < timeRefreshInterval) return;
        _timeTimer = 0f;
        RefreshTime();
    }

    private void OnKeyUsed(KeyType _) => Refresh();

    private void Refresh()
    {
        var gm = GameManager.Instance;
        if (gm?.currentStageData == null) return;
        var data = gm.currentStageData;

        RefreshKeyLimit(altDisplay, altText, data.limitNumberALT, gm.pushedNumberALT, "<color=red>ALT</color>");
        RefreshKeyLimit(f4Display,  f4Text,  data.limitNumberF4,  gm.pushedNumberF4,  "<color=white>F4</color>");
        RefreshKeyLimit(tabDisplay, tabText, data.limitNumberTAB, gm.pushedNumberTAB, "<color=blue>TAB</color>");

        RefreshKill();
        RefreshTime();

        if (actionCountDisplay != null) actionCountDisplay.SetActive(true);
        if (actionCountText != null)
        {
            int total = gm.pushedNumberALT + gm.pushedNumberF4 + gm.pushedNumberTAB;
            actionCountText.text = $"<color=white>Action Count</color>\n{total}";
        }
    }

    private void RefreshKeyLimit(GameObject display, TMP_Text text, int limit, int pushed, string keyLabel)
    {
        if (display != null) display.SetActive(limit > 0);
        if (limit <= 0 || text == null) return;
        int remaining = Mathf.Max(0, limit - pushed);
        text.text = $"{keyLabel} : {remaining}/{limit}";
    }

    private void RefreshKill()
    {
        if (killDisplay == null) return;

        var enemies = FindObjectsByType<EnemyBehaviour>(FindObjectsSortMode.None);
        int total = 0, dead = 0;
        foreach (var e in enemies)
        {
            if (!e.gameObject.activeSelf) continue;
            total++;
            if (e.IsDead) dead++;
        }

        killDisplay.SetActive(total > 0);
        if (total > 0 && killText != null)
            killText.text = $"<color=red>Kill Enemies</color>\n{dead}/{total}";
    }

    private void RefreshTime()
    {
        var gm = GameManager.Instance;
        if (gm?.currentStageData == null) return;
        var data = gm.currentStageData;

        bool hasLimit = data.limitTime > 0f;
        if (timeDisplay != null) timeDisplay.SetActive(hasLimit);
        if (!hasLimit || timeText == null) return;

        timeText.text = $"<color=white>Time</color>\n{gm.currentTime:F1}s / {(int)data.limitTime}s";
    }
}
