using UnityEngine;

[CreateAssetMenu(fileName = "SO_StageData", menuName = "ScriptableObject/StageData")]
public class SO_StageData : ScriptableObject
{
    public string stageDescription;
    
    public GameObject stagePrefab; // Level Prefab
    
    public int chapterNum; // 챕터 번호
    public int stageNum; // 하위 스테이지 번호
    
    public string stageName => chapterNum + "-" + stageNum;
    
    public AudioClip audioClip;
    
    public bool canUseF4 = true;
    public bool canUseLeftALT = true;
    public bool canUseTAB = false;

    public int enemyNum = 0;

}
