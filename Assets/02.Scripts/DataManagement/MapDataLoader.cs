using UnityEngine;
using System.IO;
using System.Collections.Generic;


//���ӸŴ������� ����ؾ��ϱ� ������, StageData�� ������ ���½��ϴ�.
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
