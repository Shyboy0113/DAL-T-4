using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 전시회 분석 로거 — 로컬 JSON 저장
///
/// [세션 = 한 번의 시도]
///   진입 → 세션 시작 (카운터 리셋)
///   클리어 / 재시작 / 이탈 → 세션 종료 (데이터 기록)
///
/// [씬 배치] GameManager 옆 GameObject 에 추가
/// </summary>
public class ExhibitionLogger : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────
    // 설정
    // ─────────────────────────────────────────────────────────────────

    [Header("저장 파일명 (persistentDataPath 기준)")]
    [SerializeField] private string fileName = "ExhibitionLog.json";

    [Tooltip("변경 있을 때마다 즉시 저장 (전시 중 강제 종료 대비)")]
    [SerializeField] private bool saveOnEveryChange = true;

    [Header("에디터 모드")]
    [Tooltip("에디터에서도 로깅할지 여부 (전시 데이터 오염 주의)")]
    [SerializeField] private bool enableInEditor = false;

    // ─────────────────────────────────────────────────────────────────
    // 내부 상태
    // ─────────────────────────────────────────────────────────────────

    private ExhibitionSaveData _data;
    private string             _filePath;

    // 세션 추적
    private int   _currentChapter;
    private int   _currentStage;
    private float _sessionStartTime;
    private bool  _sessionActive;

    // 세션 내 카운터 (세션 시작마다 리셋)
    private int _sessionDeathCount;
    private int _sessionAltCount;
    private int _sessionTabCount;
    private int _sessionF4Count;
    private int _sessionUndoCount;

    // ─────────────────────────────────────────────────────────────────
    // 생명주기
    // ─────────────────────────────────────────────────────────────────

    private void Awake()
    {
#if UNITY_EDITOR
        if (!enableInEditor)
        {
            Debug.Log("[ExhibitionLogger] 에디터 모드 — 로깅 비활성화");
            enabled = false;
            return;
        }
#endif
        _filePath = Path.Combine(Application.persistentDataPath, fileName);
        Load();
    }

    private void OnEnable()
    {
        GameEvents.StageRecordStarted += OnStageEntered;
        GameEvents.StageRecordEnded   += OnStageCleared;
        GameEvents.StageAbandoned     += OnStageAbandoned;
        GameEvents.PlayerDied         += OnPlayerDied;
        GameEvents.KeyUsed            += OnKeyUsed;
        GameEvents.UndoTriggered      += OnUndoUsed;
    }

    private void OnDisable()
    {
        GameEvents.StageRecordStarted -= OnStageEntered;
        GameEvents.StageRecordEnded   -= OnStageCleared;
        GameEvents.StageAbandoned     -= OnStageAbandoned;
        GameEvents.PlayerDied         -= OnPlayerDied;
        GameEvents.KeyUsed            -= OnKeyUsed;
        GameEvents.UndoTriggered      -= OnUndoUsed;
    }

    private void OnApplicationQuit()
    {
        if (_sessionActive)
            EndSession("abandon");

        ForceSave();
    }

    // ─────────────────────────────────────────────────────────────────
    // 이벤트 핸들러
    // ─────────────────────────────────────────────────────────────────

    private void OnStageEntered(int chapter, int stage)
    {
        // 이전 세션이 열려있으면 종료 처리
        if (_sessionActive)
        {
            bool sameStage = _currentChapter == chapter && _currentStage == stage;
            EndSession(sameStage ? "retry" : "abandon");
        }

        _currentChapter   = chapter;
        _currentStage     = stage;
        _sessionStartTime = Time.realtimeSinceStartup;
        _sessionActive    = true;

        // 세션 카운터 리셋
        _sessionDeathCount = 0;
        _sessionAltCount   = 0;
        _sessionTabCount   = 0;
        _sessionF4Count    = 0;
        _sessionUndoCount  = 0;

        // entry 카운트 증가
        var rec = GetOrCreate(chapter, stage);
        rec.entryCount++;

        if (chapter == 1 && stage == 1)
            _data.summary.totalVisitors++;

        DirtyAndSave();
    }

    private void OnStageCleared()
    {
        if (!_sessionActive) return;
        EndSession("clear");
        DirtyAndSave();
    }

    private void OnStageAbandoned(int chapter, int stage)
    {
        if (!_sessionActive) return;
        EndSession("abandon");
        DirtyAndSave();
    }

    private void OnPlayerDied()
    {
        if (!_sessionActive) return;
        _sessionDeathCount++;
        _data.summary.totalDeaths++;
    }

    private void OnKeyUsed(KeyType keyType)
    {
        if (!_sessionActive) return;
        switch (keyType)
        {
            case KeyType.Alt: _sessionAltCount++; break;
            case KeyType.Tab: _sessionTabCount++; break;
            case KeyType.F4:  _sessionF4Count++;  break;
        }
    }

    private void OnUndoUsed()
    {
        if (!_sessionActive) return;
        _sessionUndoCount++;
    }

    // ─────────────────────────────────────────────────────────────────
    // 세션 종료 공통 로직
    // ─────────────────────────────────────────────────────────────────

    private void EndSession(string result)
    {
        float elapsed = Time.realtimeSinceStartup - _sessionStartTime;
        var rec = GetOrCreate(_currentChapter, _currentStage);

        // 플레이 시간 (모든 result에 기록)
        rec.totalPlayTime += elapsed;
        _data.summary.totalPlaySeconds += elapsed;

        // result별 처리
        switch (result)
        {
            case "clear":
                rec.clearCount++;
                rec.totalClearTime += elapsed;
                rec.minClearTime = Mathf.Min(rec.minClearTime, elapsed);
                rec.maxClearTime = Mathf.Max(rec.maxClearTime, elapsed);
                _data.summary.totalClears++;
                break;

            case "retry":
                rec.retryCount++;
                break;

            case "abandon":
                rec.abandonCount++;
                break;
        }

        // 세션 카운터 누적
        rec.deathCount    += _sessionDeathCount;
        rec.totalAltCount += _sessionAltCount;
        rec.totalTabCount += _sessionTabCount;
        rec.totalF4Count  += _sessionF4Count;
        rec.totalUndoCount += _sessionUndoCount;

        _sessionActive = false;
    }

    // ─────────────────────────────────────────────────────────────────
    // 헬퍼
    // ─────────────────────────────────────────────────────────────────

    private ExhibitionStageRecord GetOrCreate(int chapter, int stage)
    {
        var rec = _data.stages.Find(r => r.chapter == chapter && r.stage == stage);
        if (rec == null)
        {
            rec = new ExhibitionStageRecord { chapter = chapter, stage = stage };
            _data.stages.Add(rec);
        }
        return rec;
    }

    private void DirtyAndSave()
    {
        _data.summary.lastUpdatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        if (saveOnEveryChange)
            ForceSave();
    }

    // ─────────────────────────────────────────────────────────────────
    // 직렬화
    // ─────────────────────────────────────────────────────────────────

    private void Load()
    {
        if (File.Exists(_filePath))
        {
            try
            {
                string json = File.ReadAllText(_filePath);
                _data = JsonUtility.FromJson<ExhibitionSaveData>(json) ?? new ExhibitionSaveData();
                Debug.Log($"[ExhibitionLogger] 로그 불러옴: {_filePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[ExhibitionLogger] 로드 실패: {e.Message}");
                _data = new ExhibitionSaveData();
            }
        }
        else
        {
            _data = new ExhibitionSaveData();
            _data.summary.recordStartedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            Debug.Log($"[ExhibitionLogger] 새 로그 파일 생성: {_filePath}");
        }
    }

    public void ForceSave()
    {
        foreach (var r in _data.stages)
            r.NormalizeMinMax();

        try
        {
            string json = JsonUtility.ToJson(_data, true);
            File.WriteAllText(_filePath, json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[ExhibitionLogger] 저장 실패: {e.Message}");
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // 공개 API
    // ─────────────────────────────────────────────────────────────────

    public ExhibitionSaveData          GetData()                      => _data;
    public ExhibitionSummary           GetSummary()                   => _data.summary;
    public List<ExhibitionStageRecord> GetAllStageRecords()           => _data.stages;
    public ExhibitionStageRecord       GetStageRecord(int ch, int st) => _data.stages.Find(r => r.chapter == ch && r.stage == st);

    public float GetEntryRate(int chapter, int stage)
    {
        if (_data.summary.totalVisitors <= 0) return 0f;
        var rec = GetStageRecord(chapter, stage);
        return rec == null ? 0f : (float)rec.entryCount / _data.summary.totalVisitors;
    }
}