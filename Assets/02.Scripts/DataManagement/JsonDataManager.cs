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

public class JsonDataManager : MonoBehaviour
{
    private string filePath;
    private Dictionary<string, StageProgressData> stageDataDict = new();

    private void Awake()
    {
        filePath = Path.Combine(Application.persistentDataPath, "stageData.json");
        
        // 현재 세이브 파일 저장 위치 표기
        Debug.Log("세이브 파일 경로: " + filePath);
        
        LoadAllStageData();
    }
    private void LoadAllStageData()
    {
        // 1. 먼저 플레이어의 컴퓨터에 저장된 파일(filePath)이 있는지 확인합니다.
        if (File.Exists(filePath))
        {
            string jsonData = File.ReadAllText(filePath);
            stageDataDict = JsonUtility.FromJson<SerializableDictionary>(jsonData).ToDictionary();
            Debug.Log("저장된 데이터를 불러왔습니다: " + filePath);
            return; // 저장 파일을 성공적으로 불러왔으므로 여기서 함수를 종료합니다.
        }
        
        // 2. 저장된 파일이 없다면 (처음 실행 등), Resources 폴더의 초기 데이터를 불러옵니다.
        TextAsset textAsset = Resources.Load<TextAsset>("stageData");

        if (textAsset != null)
        {
            string jsonData = textAsset.text;
            stageDataDict = JsonUtility.FromJson<SerializableDictionary>(jsonData).ToDictionary();
            Debug.Log("저장 파일이 없어 Resources 폴더의 기본 데이터를 불러왔습니다.");
        }
        else
        {
            Debug.Log("저장 파일과 기본 데이터가 모두 없습니다. 새롭게 시작합니다.");
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
