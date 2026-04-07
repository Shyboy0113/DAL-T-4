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
    public bool isAppeared = true; // 다음 스테이지가 클리어 됐을 때, 해당 스테이지가 표시됨
    public float minClearTime = float.MaxValue;

    // 플레이 분석 데이터
    public float totalPlayTime = 0f; // 스테이지에 머문 누적 시간 (초)
    public int   attemptCount  = 0;  // 클리어 or 리스타트 횟수
    public int   abandonCount  = 0;  // StageSelect 이동 or 게임 종료 횟수

    public StageProgressData(int chapter, int stage)
    {
        this.chapter = chapter;
        this.stage = stage;
    }
}

[System.Serializable]
public class GlobalStatsData
{
    public int lifetimeALT = 0;
    public int lifetimeF4  = 0;
    public int lifetimeTAB = 0;
    public int totalDeaths = 0;
    public int totalClears = 0;
}

public class JsonDataManager : MonoBehaviour
{
    private string filePath;
    private Dictionary<string, StageProgressData> stageDataDict = new();
    private GlobalStatsData globalStats = new();

    private void Awake()
    {
        filePath = Path.Combine(Application.persistentDataPath, "stageData.json");

        // 현재 세이브 파일 저장 위치 표기
        Debug.Log("세이브 파일 경로: " + filePath);

        LoadAllData();
    }

    private void LoadAllData()
    {
        // 1. 먼저 플레이어의 컴퓨터에 저장된 파일(filePath)이 있는지 확인합니다.
        if (File.Exists(filePath))
        {
            string jsonData = File.ReadAllText(filePath);
            var saveData = JsonUtility.FromJson<SaveData>(jsonData);
            stageDataDict = saveData.ToDictionary();
            globalStats   = saveData.globalStats ?? new GlobalStatsData();
            Debug.Log("저장된 데이터를 불러왔습니다: " + filePath);
            return;
        }

        // 2. 저장된 파일이 없다면 (처음 실행 등), Resources 폴더의 초기 데이터를 불러옵니다.
        TextAsset textAsset = Resources.Load<TextAsset>("StageData");

        if (textAsset != null)
        {
            var saveData = JsonUtility.FromJson<SaveData>(textAsset.text);
            stageDataDict = saveData.ToDictionary();
            globalStats   = saveData.globalStats ?? new GlobalStatsData();
            Debug.Log("저장 파일이 없어 Resources 폴더의 기본 데이터를 불러왔습니다.");
        }
        else
        {
            Debug.Log("저장 파일과 기본 데이터가 모두 없습니다. 새롭게 시작합니다.");
        }
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
            // isAppeared 기본값(true)이 아닌 false로 생성 — 세이브에 없는 스테이지는 잠김 상태
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

    /// <summary>저장 파일을 삭제하고 Resources의 기본 데이터로 복원합니다.</summary>
    public void ResetAllData()
    {
        if (File.Exists(filePath))
            File.Delete(filePath);

        stageDataDict = new Dictionary<string, StageProgressData>();
        globalStats   = new GlobalStatsData();

        TextAsset textAsset = Resources.Load<TextAsset>("StageData");
        if (textAsset != null)
        {
            var saveData  = JsonUtility.FromJson<SaveData>(textAsset.text);
            stageDataDict = saveData.ToDictionary();
            globalStats   = saveData.globalStats ?? new GlobalStatsData();
        }

        // 1-1은 리셋 후에도 반드시 진입 가능해야 함
        var firstStage = GetStageData(1, 1);
        firstStage.isAppeared = true;
        SaveAllData();

        Debug.Log("세이브 데이터가 초기화되었습니다.");
    }
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
