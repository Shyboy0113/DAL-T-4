using UnityEngine;

/// <summary>
/// 스테이지별 플레이 시간과 이탈율을 JsonDataManager에 기록합니다.
/// GameEvents를 구독하며 싱글톤을 사용하지 않습니다.
/// </summary>
public class StageRecorder : MonoBehaviour
{
    private JsonDataManager DataManager => GameManager.Instance.jsonDataManager;

    private float _sessionStart;
    private int   _currentChapter;
    private int   _currentStage;
    private bool  _sessionEnded; // 종료/이탈 이벤트 중복 저장 방지

    private void OnEnable()
    {
        GameEvents.StageRecordStarted += OnSessionStarted;
        GameEvents.StageRecordEnded   += OnSessionEnded;
        GameEvents.StageAbandoned     += OnAbandoned;
    }

    private void OnDisable()
    {
        GameEvents.StageRecordStarted -= OnSessionStarted;
        GameEvents.StageRecordEnded   -= OnSessionEnded;
        GameEvents.StageAbandoned     -= OnAbandoned;
    }

    // 강제 종료 시 현재 세션이 아직 저장되지 않았으면 이탈로 기록
    private void OnApplicationQuit()
    {
        if (_sessionEnded) return;
        SavePlayTime();
        IncrementAbandon(_currentChapter, _currentStage);
    }

    // ─────────────────────────────────────────────────────────────────
    // 이벤트 핸들러
    // ─────────────────────────────────────────────────────────────────

    // StageLoader.LoadStage 성공 후 발화
    private void OnSessionStarted(int ch, int st)
    {
        _currentChapter = ch;
        _currentStage   = st;
        _sessionStart   = Time.time;
        _sessionEnded   = false;
    }

    // GameStateManagement.ChangeStage (클리어 or 리스타트) 시작 시 발화
    private void OnSessionEnded()
    {
        if (_sessionEnded) return;
        _sessionEnded = true;

        SavePlayTime();

        var data = DataManager.GetStageData(_currentChapter, _currentStage);
        data.attemptCount++;
        DataManager.SaveStageData(data);
    }

    // Game_PauseCanvas의 StageSelect / 타이틀 / 종료 버튼 클릭 시 발화
    private void OnAbandoned(int ch, int st)
    {
        if (_sessionEnded) return; // OnSessionEnded가 먼저 불렸으면 중복 저장 방지
        _sessionEnded = true;

        SavePlayTime();
        IncrementAbandon(ch, st);
    }

    // ─────────────────────────────────────────────────────────────────
    // 내부 유틸
    // ─────────────────────────────────────────────────────────────────

    private void SavePlayTime()
    {
        float elapsed = Time.time - _sessionStart;
        var data = DataManager.GetStageData(_currentChapter, _currentStage);
        data.totalPlayTime += elapsed;
        DataManager.SaveStageData(data);
    }

    private void IncrementAbandon(int ch, int st)
    {
        var data = DataManager.GetStageData(ch, st);
        data.abandonCount++;
        DataManager.SaveStageData(data);
    }
}
