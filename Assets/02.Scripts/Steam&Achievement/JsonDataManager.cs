using UnityEngine;
using System.IO;
using System.Collections.Generic;

[System.Serializable]
public class StageProgressData
{
    public int chapter = 1;
    public int stage = 1;

    public bool isFirstMissionCleared = false;
    public bool isSecondMissionCleared = false;
    public bool isThirdMissionCleared = false;

    public int minALT = int.MaxValue;
    public int minF4 = int.MaxValue;
    public int minTAB = int.MaxValue;

    public bool isCleared = false;
    public bool isAppeared = true;
    public float minClearTime = float.MaxValue;

    public float totalPlayTime = 0f;
    public int   attemptCount  = 0;
    public int   abandonCount  = 0;

    public StageProgressData(int chapter, int stage)
    {
        this.chapter = chapter;
        this.stage = stage;
    }
}

[System.Serializable]
public class GlobalStatsData
{
    public int lifetimeALT    = 0;
    public int lifetimeF4     = 0;
    public int lifetimeTAB    = 0;
    public int lifetimeAltTab = 0;
    public int totalDeaths    = 0;
    public int totalClears    = 0;
}

[System.Serializable]
public class SaveData
{
    public GlobalStatsData       globalStats;
    public List<string>          keys;
    public List<StageProgressData> values;

    public SaveData() { }

    public SaveData(Dictionary<string, StageProgressData> dict, GlobalStatsData stats)
    {
        globalStats = stats;
        keys   = new List<string>(dict.Keys);
        values = new List<StageProgressData>(dict.Values);
    }

    public Dictionary<string, StageProgressData> ToDictionary()
    {
        var dict = new Dictionary<string, StageProgressData>();
        if (keys == null) return dict;
        for (int i = 0; i < keys.Count; i++)
            dict[keys[i]] = values[i];
        return dict;
    }
}

public class JsonDataManager : MonoBehaviour
{
    private string filePath;
    private Dictionary<string, StageProgressData> stageDataDict = new();
    private GlobalStatsData globalStats = new();

    private void Awake()
    {
        filePath = Path.Combine(Application.persistentDataPath, "StageData.json");
        Debug.Log("세이브 파일 경로: " + filePath);
        LoadAllData();
    }

    private void LoadAllData()
    {
        if (File.Exists(filePath))
        {
            string jsonData = File.ReadAllText(filePath);
            var saveData = JsonUtility.FromJson<SaveData>(jsonData);
            stageDataDict = saveData.ToDictionary();
            globalStats   = saveData.globalStats ?? new GlobalStatsData();
            Debug.Log("저장된 데이터를 불러왔습니다: " + filePath);
        }
        else
        {
            Debug.Log("저장 파일이 없습니다. 새롭게 시작합니다.");
        }

        var firstStage = GetStageData(1, 1);
        firstStage.isAppeared = true;
    }

    private void SaveAllData()
    {
        string jsonData = JsonUtility.ToJson(new SaveData(stageDataDict, globalStats), true);
        File.WriteAllText(filePath, jsonData);
    }

    public StageProgressData GetStageData(int chapter, int stage)
    {
        string key = $"{chapter}-{stage}";
        if (!stageDataDict.ContainsKey(key))
        {
            stageDataDict[key] = new StageProgressData(chapter, stage) { isAppeared = false };
        }
        return stageDataDict[key];
    }

    public void SaveStageData(StageProgressData data)
    {
        string key = $"{data.chapter}-{data.stage}";
        stageDataDict[key] = data;
        SaveAllData();
        GameEvents.RaiseSaveDataChanged();
    }

    public GlobalStatsData GetGlobalStats() => globalStats;

    public void SaveGlobalStats()
    {
        SaveAllData();
    }

    // ─────────────────────────────────────────────────────────────────
    // 챕터/전체 클리어 판정 헬퍼
    // ─────────────────────────────────────────────────────────────────

    public bool IsChapterCleared(int chapter, int stagesPerChapter)
    {
        for (int st = 1; st <= stagesPerChapter; st++)
        {
            var data = GetStageData(chapter, st);
            if (!data.isCleared) return false;
        }
        return true;
    }

    public bool IsChapterPerfect(int chapter, int stagesPerChapter)
    {
        for (int st = 1; st <= stagesPerChapter; st++)
        {
            var data = GetStageData(chapter, st);
            if (!data.isFirstMissionCleared || 
                !data.isSecondMissionCleared || 
                !data.isThirdMissionCleared)
                return false;
        }
        return true;
    }

    public bool IsAllCleared(int totalChapters, int stagesPerChapter)
    {
        for (int ch = 1; ch <= totalChapters; ch++)
        {
            if (!IsChapterCleared(ch, stagesPerChapter)) return false;
        }
        return true;
    }

    public bool IsAllPerfect(int totalChapters, int stagesPerChapter)
    {
        for (int ch = 1; ch <= totalChapters; ch++)
        {
            if (!IsChapterPerfect(ch, stagesPerChapter)) return false;
        }
        return true;
    }

    public void UnlockAllStages(int totalChapters, int stagesPerChapter)
    {
        UnlockStageRange(1, totalChapters, stagesPerChapter);
    }

    public void UnlockStageRange(int minChapter, int maxChapter, int stagesPerChapter)
    {
        for (int ch = minChapter; ch <= maxChapter; ch++)
            for (int st = 1; st <= stagesPerChapter; st++)
                GetStageData(ch, st).isAppeared = true;
        SaveAllData();
        GameEvents.RaiseSaveDataChanged();
        
        Debug.Log($"스테이지 해금: {minChapter}-1 ~ {maxChapter}-{stagesPerChapter}");
    }

    public void UnlockSpecificRange(int startChapter, int startStage, int endChapter, int endStage, int stagesPerChapter)
    {
        ForEachInRange(startChapter, startStage, endChapter, endStage, stagesPerChapter,
            (ch, st) => GetStageData(ch, st).isAppeared = true);
        SaveAllData();
        GameEvents.RaiseSaveDataChanged();
        Debug.Log($"스테이지 해금: {startChapter}-{startStage} ~ {endChapter}-{endStage}");
    }

    public void LockSpecificRange(int startChapter, int startStage, int endChapter, int endStage, int stagesPerChapter)
    {
        // 자연 해금 여부와 무관하게 범위 내 모든 스테이지를 강제 잠금·초기화
        ForEachInRange(startChapter, startStage, endChapter, endStage, stagesPerChapter, (ch, st) =>
        {
            stageDataDict[$"{ch}-{st}"] = new StageProgressData(ch, st) { isAppeared = false };
        });
        SaveAllData();
        GameEvents.RaiseSaveDataChanged();
        Debug.Log($"스테이지 잠금: {startChapter}-{startStage} ~ {endChapter}-{endStage}");
    }

    private void ForEachInRange(int startCh, int startSt, int endCh, int endSt, int stagesPerChapter, System.Action<int, int> action)
    {
        int ch = startCh, st = startSt;
        while (ch < endCh || (ch == endCh && st <= endSt))
        {
            action(ch, st);
            st++;
            if (st > stagesPerChapter) { st = 1; ch++; }
        }
    }

    private StageProgressData GetPreviousStageData(int chapter, int stage, int stagesPerChapter)
    {
        int prevCh = stage > 1 ? chapter : chapter - 1;
        int prevSt = stage > 1 ? stage - 1 : stagesPerChapter;
        if (prevCh < 1) return null;
        return stageDataDict.TryGetValue($"{prevCh}-{prevSt}", out var data) ? data : null;
    }

    public void ResetAllData()
    {
        if (File.Exists(filePath))
            File.Delete(filePath);

        stageDataDict = new Dictionary<string, StageProgressData>();
        globalStats   = new GlobalStatsData();

        var firstStage = GetStageData(1, 1);
        firstStage.isAppeared = true;
        SaveAllData();
        GameEvents.RaiseSaveDataChanged();

        Debug.Log("세이브 데이터가 초기화되었습니다.");
    }
    
    // 지정 범위 클리어 처리
    public void ClearStageRange(int minChapter, int maxChapter, int stagesPerChapter,
        bool clearFirst, bool clearSecond, bool clearThird)
    {
        for (int ch = minChapter; ch <= maxChapter; ch++)
        for (int st = 1; st <= stagesPerChapter; st++)
            ApplyClear(GetStageData(ch, st), clearFirst, clearSecond, clearThird);

        SaveAllData();
        GameEvents.RaiseSaveDataChanged();
        Debug.Log($"스테이지 올클리어: {minChapter}-1 ~ {maxChapter}-{stagesPerChapter}");
    }

    public void ClearSpecificRange(int startChapter, int startStage, int endChapter, int endStage,
        int stagesPerChapter, bool clearFirst, bool clearSecond, bool clearThird)
    {
        ForEachInRange(startChapter, startStage, endChapter, endStage, stagesPerChapter,
            (ch, st) => ApplyClear(GetStageData(ch, st), clearFirst, clearSecond, clearThird));

        // 실제 게임과 동일하게: 범위 마지막 스테이지 클리어 → 바로 다음 스테이지 해금
        int nextSt = endStage < stagesPerChapter ? endStage + 1 : 1;
        int nextCh = endStage < stagesPerChapter ? endChapter : endChapter + 1;
        GetStageData(nextCh, nextSt).isAppeared = true;

        SaveAllData();
        GameEvents.RaiseSaveDataChanged();
        Debug.Log($"스테이지 클리어: {startChapter}-{startStage} ~ {endChapter}-{endStage}");
    }

    private void ApplyClear(StageProgressData data, bool clearFirst, bool clearSecond, bool clearThird)
    {
        data.isAppeared = true;
        data.isCleared  = true;
        if (clearFirst)  data.isFirstMissionCleared  = true;
        if (clearSecond) data.isSecondMissionCleared = true;
        if (clearThird)  data.isThirdMissionCleared  = true;
    }
}