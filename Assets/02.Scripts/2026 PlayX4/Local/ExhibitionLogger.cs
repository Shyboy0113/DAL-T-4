using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// 전시회 분석 로거 — 로컬 JSON 저장 + CSV 행 저장
///
/// [세션 = 한 번의 시도]
///   진입 → 세션 시작 (카운터 리셋)
///   클리어 / 재시작 / 이탈 → 세션 종료 (데이터 기록)
///
/// [CSV 저장]
///   구글 폼 응답시트와 동일한 컬럼 순서로 세션마다 1행 append
///   → 나중에 응답시트에 그대로 붙여넣기 가능
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
    [SerializeField] private string csvFileName = "ExhibitionLog.csv";

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
    private string             _csvFilePath;

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

    // CSV 용 visitor_id
    private string _visitorId;

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
        _filePath    = Path.Combine(Application.persistentDataPath, fileName);
        _csvFilePath = Path.Combine(Application.persistentDataPath, csvFileName);
        Load();
        EnsureCsvHeader();
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

        // 1-1 진입 시 새 visitor_id 발급
        if (chapter == 1 && stage == 1)
            _visitorId = GenerateVisitorId();

        // visitor_id가 없으면 생성 (게임 중간부터 컴포넌트 활성화된 경우 방어)
        if (string.IsNullOrEmpty(_visitorId))
            _visitorId = GenerateVisitorId();

        // ── JSON 집계 ──
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
        DirtyAndSave();
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

        // ── CSV 행 저장 (구글 폼 응답시트와 동일 양식) ──
        AppendCsvRow(result, elapsed);

        // ── JSON 집계 ──
        var rec = GetOrCreate(_currentChapter, _currentStage);

        rec.totalPlayTime += elapsed;
        _data.summary.totalPlaySeconds += elapsed;

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

        rec.deathCount     += _sessionDeathCount;
        rec.totalAltCount  += _sessionAltCount;
        rec.totalTabCount  += _sessionTabCount;
        rec.totalF4Count   += _sessionF4Count;
        rec.totalUndoCount += _sessionUndoCount;

        _sessionActive = false;
    }

    // ─────────────────────────────────────────────────────────────────
    // CSV 저장
    // ─────────────────────────────────────────────────────────────────

    // 구글 폼 응답시트 컬럼 순서와 동일
    private const string CsvHeader =
        "타임스탬프,visitor_id,stage,death_count,alt_count,chapter,result,play_time,tab_count,f4_count,undo_count";

    private void EnsureCsvHeader()
    {
        if (!File.Exists(_csvFilePath))
        {
            try
            {
                File.WriteAllText(_csvFilePath, CsvHeader + "\n", Encoding.UTF8);
                Debug.Log($"[ExhibitionLogger] CSV 파일 생성: {_csvFilePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[ExhibitionLogger] CSV 헤더 쓰기 실패: {e.Message}");
            }
        }
    }

    private void AppendCsvRow(string result, float playTime)
    {
        // 구글 폼 타임스탬프 형식과 동일: "2026. 5. 20 오후 3:20:10"
        string timestamp = FormatTimestamp(DateTime.Now);

        string row =
            $"{timestamp}," +
            $"{_visitorId}," +
            $"{_currentStage}," +
            $"{_sessionDeathCount}," +
            $"{_sessionAltCount}," +
            $"{_currentChapter}," +
            $"{result}," +
            $"{playTime:F1}," +
            $"{_sessionTabCount}," +
            $"{_sessionF4Count}," +
            $"{_sessionUndoCount}";

        try
        {
            File.AppendAllText(_csvFilePath, row + "\n", Encoding.UTF8);
        }
        catch (Exception e)
        {
            Debug.LogError($"[ExhibitionLogger] CSV 행 쓰기 실패: {e.Message}");
        }
    }

    /// <summary>
    /// 구글 폼 타임스탬프 형식으로 변환
    /// 예: "2026. 5. 20 오후 3:20:10"
    /// </summary>
    private string FormatTimestamp(DateTime dt)
    {
        string amPm   = dt.Hour < 12 ? "오전" : "오후";
        int    hour12 = dt.Hour % 12;
        if (hour12 == 0) hour12 = 12;

        return $"{dt.Year}. {dt.Month}. {dt.Day} {amPm} {hour12}:{dt.Minute:D2}:{dt.Second:D2}";
    }

    // ─────────────────────────────────────────────────────────────────
    // visitor_id 생성
    // ─────────────────────────────────────────────────────────────────

    private string GenerateVisitorId()
    {
        string time   = DateTime.Now.ToString("MMddHHmmss");
        string random = UnityEngine.Random.Range(0, 0xFFFF).ToString("X4");
        return $"V{time}_{random}";
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
    // JSON 직렬화
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

    public string                       CurrentVisitorId               => _visitorId;
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