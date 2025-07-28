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
    public bool isAppeared = true; //이전 스테이지 클리어 시, 다음 스테이지가 해금
    public float minClearTime = float.MaxValue;

    public StageProgressData(int chapter, int stage)
    {
        this.chapter = chapter;
        this.stage = stage;
    }
}

public class JsonDataManager : MonoBehaviour
{
    private string filePath;
    private Dictionary<string, StageProgressData> stageDataDict = new();

    private void Awake()
    {
        filePath = Path.Combine(Application.dataPath, "MapData&TileMap", "stageData.json");
        LoadAllStageData();
    }

    private void LoadAllStageData()
    {
        if (File.Exists(filePath))
        {
            string jsonData = File.ReadAllText(filePath);
            stageDataDict = JsonUtility.FromJson<SerializableDictionary>(jsonData).ToDictionary();
        }
        else
        {
            Debug.Log("No save file found, starting fresh.");
        }
    }

    private void SaveAllStageData()
    {
        string jsonData = JsonUtility.ToJson(new SerializableDictionary(stageDataDict), true);
        File.WriteAllText(filePath, jsonData);
    }

    public StageProgressData GetStageData(int chapter, int stage)
    {
        string key = $"{chapter}-{stage}";
        if (!stageDataDict.ContainsKey(key))
        {
            stageDataDict[key] = new StageProgressData(chapter, stage);
        }
        return stageDataDict[key];
    }

    public void SaveStageData(StageProgressData data)
    {
        string key = $"{data.chapter}-{data.stage}";
        stageDataDict[key] = data;
        SaveAllStageData();
    }
}

[System.Serializable]
public class SerializableDictionary
{
    public List<string> keys;
    public List<StageProgressData> values;

    public SerializableDictionary(Dictionary<string, StageProgressData> dict)
    {
        keys = new List<string>(dict.Keys);
        values = new List<StageProgressData>(dict.Values);
    }

    public Dictionary<string, StageProgressData> ToDictionary()
    {
        Dictionary<string, StageProgressData> dict = new();
        for (int i = 0; i < keys.Count; i++)
        {
            dict[keys[i]] = values[i];
        }
        return dict;
    }
}
