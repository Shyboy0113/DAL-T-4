using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class MapDataLoader : MonoBehaviour
{
    ////////////////////////////////
    ///
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
        public bool isFirstMissionCleared;
        public bool isSecondMissionCleared;
        public bool isThirdMissionCleared;
        public bool isAppeared;
        public bool isCleared;
        public float clearTime;
    }

    [System.Serializable]
    public class ChapterData
    {
        public int chapterNum;
        public List<StageData> stages;
    }

    [System.Serializable]

    ////////////////////////////////

    public class GameData
    {
        public List<ChapterData> chapters;
    }

    public GameData gameData;
    public StageData currentStage;

    void Awake()
    {
        LoadGameData();
    }
    void LoadGameData()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "mapData.json");
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

    public void GetStageData(int chapter, int stage)
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
                        currentStage = st;
                        return;
                    }
                }
            }
        }
        currentStage = null;
    }

    public bool CanUseF4()
    {
        return currentStage.canUseF4;
    }

    public bool CanUseALT()
    {
        return currentStage.canUseAlt;
    }

    public bool CanUseTab()
    {
        return currentStage.canUseTab;
    }

}
