using System;
using System.Collections.Generic;
using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
// 스테이지별 누적 통계 (방문자가 바뀌어도 계속 쌓임)
// ─────────────────────────────────────────────────────────────────────────────
[Serializable]
public class ExhibitionStageRecord
{
    public int chapter;
    public int stage;

    // ── 횟수 집계 ──────────────────────────────────────────────────
    public int entryCount;       // 스테이지 진입 횟수 (재시도 포함)
    public int clearCount;       // 클리어 횟수
    public int abandonCount;     // 이탈 횟수
    public int retryCount;       // 재시도 횟수
    public int deathCount;       // 사망 횟수

    // ── 클리어 시간 ────────────────────────────────────────────────
    public float totalClearTime;
    public float minClearTime = float.MaxValue;
    public float maxClearTime = float.MinValue;

    // ── 플레이 시간 (결과 무관, 모든 세션 합산) ───────────────────
    public float totalPlayTime;

    // ── 키 입력 누적 ──────────────────────────────────────────────
    public int totalAltCount;
    public int totalTabCount;
    public int totalF4Count;
    public int totalUndoCount;

    // ── 파생 지표 ─────────────────────────────────────────────────
    public float ClearRate    => entryCount > 0 ? (float)clearCount   / entryCount : 0f;
    public float AbandonRate  => entryCount > 0 ? (float)abandonCount / entryCount : 0f;
    public float AvgClearTime => clearCount > 0 ? totalClearTime      / clearCount : 0f;
    public float AvgPlayTime  => entryCount > 0 ? totalPlayTime       / entryCount : 0f;
    public float AvgDeaths    => entryCount > 0 ? (float)deathCount   / entryCount : 0f;

    public void NormalizeMinMax()
    {
        if (minClearTime == float.MaxValue) minClearTime = 0f;
        if (maxClearTime == float.MinValue) maxClearTime = 0f;
    }
}

[Serializable]
public class ExhibitionSummary
{
    public int   totalVisitors;
    public int   totalClears;
    public int   totalDeaths;
    public float totalPlaySeconds;
    public string recordStartedAt;
    public string lastUpdatedAt;
}

[Serializable]
public class ExhibitionSaveData
{
    public ExhibitionSummary           summary = new();
    public List<ExhibitionStageRecord> stages  = new();
}