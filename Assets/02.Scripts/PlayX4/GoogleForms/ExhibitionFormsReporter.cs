using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Google Forms 에 시도 단위 데이터를 전송합니다.
///
/// [폼 항목 10개 — 모두 단답형]
///   visitor_id, chapter, stage, result, play_time,
///   death_count, alt_count, tab_count, f4_count, undo_count
///
/// [result 값]
///   clear   — 클리어 성공
///   retry   — 같은 스테이지 재시작
///   abandon — 다른 스테이지 이동 / 게임 종료
///
/// [씬 배치] ExhibitionLogger 와 같은 GameObject 에 추가
/// </summary>
public class ExhibitionFormsReporter : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────
    // 인스펙터 — Google Forms 설정
    // ─────────────────────────────────────────────────────────────────

    [Header("Google Forms 제출 URL")]
    [Tooltip("https://docs.google.com/forms/d/e/XXXXXX/formResponse")]
    [SerializeField] private string _formUrl;

    [Header("Entry ID (폼 미리보기 → F12 콘솔에서 확인)")]
    [SerializeField] private string _entryVisitorId;    // entry.XXXXXXXXX
    [SerializeField] private string _entryChapter;
    [SerializeField] private string _entryStage;
    [SerializeField] private string _entryResult;
    [SerializeField] private string _entryPlayTime;
    [SerializeField] private string _entryDeathCount;
    [SerializeField] private string _entryAltCount;
    [SerializeField] private string _entryTabCount;
    [SerializeField] private string _entryF4Count;
    [SerializeField] private string _entryUndoCount;

    [Header("옵션")]
    [Tooltip("false 로 끄면 전송 없이 로컬만 저장")]
    [SerializeField] private bool _enableUpload = true;

    [Header("에디터 모드")]
    [SerializeField] private bool _enableInEditor = false;

    // ─────────────────────────────────────────────────────────────────
    // 세션 상태
    // ─────────────────────────────────────────────────────────────────

    private string _visitorId;
    private int    _currentChapter;
    private int    _currentStage;
    private float  _sessionStartTime;
    private bool   _sessionActive;

    // 세션 내 카운터
    private int _sessionDeathCount;
    private int _sessionAltCount;
    private int _sessionTabCount;
    private int _sessionF4Count;
    private int _sessionUndoCount;

    // 전송 큐
    private readonly Queue<FormPayload> _queue = new();
    private bool _isSending;

    // ─────────────────────────────────────────────────────────────────
    // 생명주기
    // ─────────────────────────────────────────────────────────────────

    private void Awake()
    {
#if UNITY_EDITOR
        if (!_enableInEditor)
        {
            enabled = false;
            return;
        }
#endif
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
            EndSessionAndSend("abandon");
    }

    // ─────────────────────────────────────────────────────────────────
    // 이벤트 핸들러
    // ─────────────────────────────────────────────────────────────────

    private void OnStageEntered(int chapter, int stage)
    {
        // 이전 세션 종료 처리
        if (_sessionActive)
        {
            bool sameStage = _currentChapter == chapter && _currentStage == stage;
            EndSessionAndSend(sameStage ? "retry" : "abandon");
        }

        // 1-1 진입 시 새 visitor_id 발급
        if (chapter == 1 && stage == 1)
            _visitorId = GenerateVisitorId();

        // visitor_id가 없으면 생성 (게임 중간부터 컴포넌트 활성화된 경우 방어)
        if (string.IsNullOrEmpty(_visitorId))
            _visitorId = GenerateVisitorId();

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
    }

    private void OnStageCleared()
    {
        if (!_sessionActive) return;
        EndSessionAndSend("clear");
    }

    private void OnStageAbandoned(int chapter, int stage)
    {
        if (!_sessionActive) return;
        EndSessionAndSend("abandon");
    }

    private void OnPlayerDied()
    {
        if (!_sessionActive) return;
        _sessionDeathCount++;
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
    // 세션 종료 → 전송
    // ─────────────────────────────────────────────────────────────────

    private void EndSessionAndSend(string result)
    {
        float playTime = Time.realtimeSinceStartup - _sessionStartTime;
        _sessionActive = false;

        var payload = new FormPayload
        {
            visitorId  = _visitorId,
            chapter    = _currentChapter,
            stage      = _currentStage,
            result     = result,
            playTime   = playTime,
            deathCount = _sessionDeathCount,
            altCount   = _sessionAltCount,
            tabCount   = _sessionTabCount,
            f4Count    = _sessionF4Count,
            undoCount  = _sessionUndoCount,
        };

        Enqueue(payload);
    }

    // ─────────────────────────────────────────────────────────────────
    // visitor_id 생성
    // ─────────────────────────────────────────────────────────────────

    private string GenerateVisitorId()
    {
        // V + 날짜시분초 + 랜덤4자리 → 예: V0429143527_8A2F
        string time   = DateTime.Now.ToString("MMddHHmmss");
        string random = UnityEngine.Random.Range(0, 0xFFFF).ToString("X4");
        return $"V{time}_{random}";
    }

    // ─────────────────────────────────────────────────────────────────
    // 전송 큐
    // ─────────────────────────────────────────────────────────────────

    private void Enqueue(FormPayload payload)
    {
        if (!_enableUpload) return;

        _queue.Enqueue(payload);
        if (!_isSending)
            StartCoroutine(ProcessQueue());
    }

    private IEnumerator ProcessQueue()
    {
        _isSending = true;

        while (_queue.Count > 0)
        {
            var payload = _queue.Dequeue();
            yield return StartCoroutine(Submit(payload));
            yield return new WaitForSecondsRealtime(0.5f);
        }

        _isSending = false;
    }

    // ─────────────────────────────────────────────────────────────────
    // HTTP 제출
    // ─────────────────────────────────────────────────────────────────

    private IEnumerator Submit(FormPayload p)
    {
        var fields = new List<IMultipartFormSection>
        {
            new MultipartFormDataSection(_entryVisitorId,  p.visitorId),
            new MultipartFormDataSection(_entryChapter,    p.chapter.ToString()),
            new MultipartFormDataSection(_entryStage,      p.stage.ToString()),
            new MultipartFormDataSection(_entryResult,     p.result),
            new MultipartFormDataSection(_entryPlayTime,   p.playTime.ToString("F1")),
            new MultipartFormDataSection(_entryDeathCount, p.deathCount.ToString()),
            new MultipartFormDataSection(_entryAltCount,   p.altCount.ToString()),
            new MultipartFormDataSection(_entryTabCount,   p.tabCount.ToString()),
            new MultipartFormDataSection(_entryF4Count,    p.f4Count.ToString()),
            new MultipartFormDataSection(_entryUndoCount,  p.undoCount.ToString()),
        };

        using var req = UnityWebRequest.Post(_formUrl, fields);
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
            Debug.Log($"[Forms] 전송 성공: {p.visitorId} / {p.chapter}-{p.stage} / {p.result} / {p.playTime:F1}s");
        else
            Debug.LogWarning($"[Forms] 전송 실패: {req.error} ({p.result})");
    }

    // ─────────────────────────────────────────────────────────────────
    // 페이로드 구조체
    // ─────────────────────────────────────────────────────────────────

    private struct FormPayload
    {
        public string visitorId;
        public int    chapter;
        public int    stage;
        public string result;
        public float  playTime;
        public int    deathCount;
        public int    altCount;
        public int    tabCount;
        public int    f4Count;
        public int    undoCount;
    }
}