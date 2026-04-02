using UnityEngine;
using System.IO;
using System.Collections.Generic;


// 스테이지 2번/3번 미션의 종류를 정의합니다.
public enum MissionType
{
    None             = 0, // 미션 없음
    MoveCountLimit   = 1, // 특정 횟수 이하로 키를 사용하여 클리어
    KillAllEnemies   = 2, // 모든 적 퇴치
    CollectAllStars  = 3, // 맵의 모든 STAR 수집
    NoSpecificFeature= 4, // 특정 기능(ALT/F4/TAB)을 사용하지 않고 클리어
    TimeLimit        = 5, // 제한 시간 내 클리어
}

// NoSpecificFeature 미션에서 금지할 기능
public enum ForbiddenFeature { None, ALT, F4, TAB }

// GameManager에서 참조해야 하기 때문에, StageData를 직렬화합니다.
[System.Serializable]
public class StageData
{
    public string stageName;
    public int chapterNum;
    public int stageNum;

    public bool canUseAlt;
    public bool canUseF4;
    public bool canUseTab;

    public string firstMission;
    public string secondMission;
    public string thirdMission;

    public int limitNumberALT;
    public int limitNumberF4;
    public int limitNumberTAB;

    public float limitTime;

    // 2번/3번 미션 타입 (기본값 None = 미사용)
    public MissionType    secondMissionType  = MissionType.None;
    public MissionType    thirdMissionType   = MissionType.None;
    // NoSpecificFeature 타입일 때 금지할 기능
    public ForbiddenFeature forbiddenFeature = ForbiddenFeature.None;
}

public class MapDataLoader : MonoBehaviour
{
    [System.Serializable]
    public class ChapterData
    {
        public int chapterNum;
        public List<StageData> stages;
    }
    [System.Serializable]
    public class GameData
    {
        public List<ChapterData> chapters;
    }

    public GameData gameData;

    void Awake()
    {
        LoadGameData();
    }
    
    void Previous_LoadGameData()
    {
        string path = Path.Combine(Application.dataPath, "Datas(Json...etc)", "mapData.json");

        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            gameData = JsonUtility.FromJson<GameData>(json);
        }
        else
        {
            Debug.LogError("JSON Error!");
        }
    }
    
    void LoadGameData()
    {
        TextAsset textAsset = Resources.Load<TextAsset>("mapData");

        if (textAsset != null)
        {
            string json = textAsset.text;
            gameData = JsonUtility.FromJson<GameData>(json);
        }
        else
        {
            Debug.LogError("Resources mapData Error!");
        }
    }

    
    public StageData GetStageData(int chapter, int stage)
    {
        foreach (var ch in gameData.chapters)
        {
            if (ch.chapterNum == chapter)
            {
                foreach (var st in ch.stages)
                {
                    if (st.stageNum == stage)
                    {                   
                        return st;
                    }
                }
            }
        }
        return null;
    }
}
