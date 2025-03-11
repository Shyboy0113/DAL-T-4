using UnityEngine;
using System.IO;
using System.Collections.Generic;


//게임매니저에서 사용해야하기 때문에, StageData는 밖으로 빼냈습니다.
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
    
}

public class MapDataLoader : MonoBehaviour
{
    /////////////중첩 클래스///////////////////
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

    //////////////중첩 클래스//////////////////

    public GameData gameData;

    void Awake()
    {
        LoadGameData();
    }
    
    //JSON 파일에서 맵 데이터 전체를 불러옵니다.
    void LoadGameData()
    {
        string path = Path.Combine(Application.dataPath, "MapData&TileMap", "mapData.json");

        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            gameData = JsonUtility.FromJson<GameData>(json);
            Debug.Log($"Loaded {gameData.chapters.Count} chapters.");
        }
        else
        {
            Debug.LogError("JSON 파일을 찾을 수 없습니다.");
        }
    }

    //JSON 파일로 불러온 맵 데이터에서, 특정 챕터의 스테이지 정보를 불러오는 함수
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
                        Debug.Log($"Stage {st.stageName} is loaded!");                        
                        return st;
                    }
                }
            }
        }
        return null;
    }
}
