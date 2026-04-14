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

    public void ResetAllData()
    {
        if (File.Exists(filePath))
            File.Delete(filePath);

        stageDataDict = new Dictionary<string, StageProgressData>();
        globalStats   = new GlobalStatsData();

        var firstStage = GetStageData(1, 1);
        firstStage.isAppeared = true;
        SaveAllData();

        Debug.Log("세이브 데이터가 초기화되었습니다.");
    }
}