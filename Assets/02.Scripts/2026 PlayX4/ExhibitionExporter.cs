using System;
using System.IO;
using System.Text;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 전시 로그를 CSV 로 내보내는 유틸리티
/// </summary>
public static class ExhibitionExporter
{
    public static string ExportCSV(ExhibitionSaveData data, string outputDir = null)
    {
        outputDir ??= Application.persistentDataPath;

        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string path      = Path.Combine(outputDir, $"Exhibition_{timestamp}.csv");

        var sb = new StringBuilder();

        // ── 요약 ───────────────────────────────────────────────────
        sb.AppendLine("=== 전시 요약 ===");
        sb.AppendLine($"기록 시작,{data.summary.recordStartedAt}");
        sb.AppendLine($"마지막 갱신,{data.summary.lastUpdatedAt}");
        sb.AppendLine($"총 방문자 수,{data.summary.totalVisitors}");
        sb.AppendLine($"총 클리어,{data.summary.totalClears}");
        sb.AppendLine($"총 사망,{data.summary.totalDeaths}");
        sb.AppendLine($"총 플레이 시간(분),{data.summary.totalPlaySeconds / 60f:F1}");
        sb.AppendLine();

        // ── 스테이지별 상세 ────────────────────────────────────────
        sb.AppendLine("=== 스테이지별 통계 ===");
        sb.AppendLine(
            "챕터,스테이지," +
            "진입수,클리어수,이탈수,재시도수,사망수," +
            "클리어율(%),이탈율(%)," +
            "평균클리어시간(초),최단클리어(초),최장클리어(초)," +
            "평균플레이타임(초),총플레이타임(초)," +
            "세션당평균사망," +
            "ALT합계,TAB합계,F4합계,Undo합계"
        );

        int totalVisitors = data.summary.totalVisitors;

        data.stages.Sort((a, b) =>
            a.chapter != b.chapter ? a.chapter.CompareTo(b.chapter) : a.stage.CompareTo(b.stage));

        foreach (var r in data.stages)
        {
            float clearRate   = r.entryCount > 0 ? (float)r.clearCount   / r.entryCount * 100f : 0f;
            float abandonRate = r.entryCount > 0 ? (float)r.abandonCount / r.entryCount * 100f : 0f;
            float avgClear    = r.clearCount > 0 ? r.totalClearTime / r.clearCount : 0f;
            float avgPlay     = r.entryCount > 0 ? r.totalPlayTime  / r.entryCount : 0f;
            float avgDeaths   = r.entryCount > 0 ? (float)r.deathCount / r.entryCount : 0f;

            float minTime = r.minClearTime == float.MaxValue ? 0f : r.minClearTime;
            float maxTime = r.maxClearTime == float.MinValue ? 0f : r.maxClearTime;

            sb.AppendLine(
                $"{r.chapter},{r.stage}," +
                $"{r.entryCount},{r.clearCount},{r.abandonCount},{r.retryCount},{r.deathCount}," +
                $"{clearRate:F1},{abandonRate:F1}," +
                $"{avgClear:F1},{minTime:F1},{maxTime:F1}," +
                $"{avgPlay:F1},{r.totalPlayTime:F1}," +
                $"{avgDeaths:F2}," +
                $"{r.totalAltCount},{r.totalTabCount},{r.totalF4Count},{r.totalUndoCount}"
            );
        }

        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        Debug.Log($"[ExhibitionExporter] CSV 저장 완료: {path}");
        return path;
    }

    public static string ExportFromScene()
    {
        var logger = UnityEngine.Object.FindFirstObjectByType<ExhibitionLogger>();
        if (logger == null)
        {
            Debug.LogWarning("[ExhibitionExporter] ExhibitionLogger 를 찾을 수 없습니다.");
            return null;
        }
        logger.ForceSave();
        return ExportCSV(logger.GetData());
    }

    public static string ExportFromFile(string logJsonPath = null)
    {
        logJsonPath ??= Path.Combine(Application.persistentDataPath, "ExhibitionLog.json");

        if (!File.Exists(logJsonPath))
        {
            Debug.LogWarning($"[ExhibitionExporter] 로그 파일 없음: {logJsonPath}");
            return null;
        }

        var data = JsonUtility.FromJson<ExhibitionSaveData>(File.ReadAllText(logJsonPath));
        return ExportCSV(data);
    }

#if UNITY_EDITOR
    [MenuItem("Tools/Exhibition/Export CSV (from log file)")]
    private static void EditorExportFromFile()
    {
        string path = EditorUtility.OpenFilePanel("ExhibitionLog.json 선택", Application.persistentDataPath, "json");
        if (string.IsNullOrEmpty(path)) return;

        string outputDir = EditorUtility.OpenFolderPanel("CSV 저장 폴더", Application.persistentDataPath, "");
        if (string.IsNullOrEmpty(outputDir)) return;

        var data = JsonUtility.FromJson<ExhibitionSaveData>(File.ReadAllText(path));
        string result = ExportCSV(data, outputDir);
        EditorUtility.DisplayDialog("완료", $"CSV 저장됨:\n{result}", "확인");
    }

    [MenuItem("Tools/Exhibition/Reset Log File")]
    private static void EditorResetLog()
    {
        string path = Path.Combine(Application.persistentDataPath, "ExhibitionLog.json");
        if (!EditorUtility.DisplayDialog("전시 로그 초기화", $"정말 초기화할까요?\n{path}", "초기화", "취소")) return;

        if (File.Exists(path)) File.Delete(path);
        Debug.Log("[ExhibitionExporter] 로그 파일 초기화 완료");
    }

    [MenuItem("Tools/Exhibition/Open Log Folder")]
    private static void EditorOpenLogFolder()
    {
        EditorUtility.RevealInFinder(Application.persistentDataPath);
    }
#endif
}