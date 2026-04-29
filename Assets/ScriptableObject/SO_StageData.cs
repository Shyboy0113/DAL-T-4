using UnityEngine;

// 스테이지 2번/3번 미션의 종류를 정의합니다.
public enum MissionType
{
    None             = 0, // 미션 없음
    StageClear       = 1, // 스테이지 클리어
    TimeLimit        = 2, // 제한 시간 내 클리어
    MoveCountLimit   = 3, // 특정 횟수 이하로 키를 사용하여 클리어
    KillAllEnemies   = 4, // 모든 적 퇴치
    CollectStar  = 5, // 맵의 STAR 수집
    NoSpecificFeature= 6, // 특정 기능(ALT/F4/TAB)을 사용하지 않고 클리어
}

// NoSpecificFeature 미션에서 금지할 기능
public enum ForbiddenFeature { None, ALT, F4, TAB }

[CreateAssetMenu(fileName = "SO_StageData", menuName = "ScriptableObject/StageData")]
public class SO_StageData : ScriptableObject
{
    [Header("기본 정보")]
    // stageDescription은 로컬라이제이션으로 이전됨
    // 키: "StageData Strings" 테이블의 "Stage_{chapterNum}-{stageNum}_Desc"
    public GameObject stagePrefab;
    public AudioClip bgmClip;
    
    [Header("스테이지 번호")]
    public int chapterNum;
    public int stageNum;
    public string stageName => chapterNum + "-" + stageNum;
    
    [Header("기능 제한 (Feature Limits)")]
    public bool canUseF4 = true;
    public bool canUseLeftALT = true;
    public bool canUseTAB = false;
    
    public bool hasSecondMap = false;

    public bool continueIceModeAfterTeleport = false;
    
    public int limitNumberALT;
    public int limitNumberF4;
    public int limitNumberTAB;
    
    [Header("미션 및 업적 세팅")]
    public float limitTime;
    public string steamAchievementKey;
    
    public MissionType firstMissionType = MissionType.None;
    public MissionType secondMissionType = MissionType.None;
    public MissionType thirdMissionType = MissionType.None;
    
    public ForbiddenFeature forbiddenFeature = ForbiddenFeature.None;
    
    [Header("스피드런 업적")]
    [Tooltip("이 시간(초) 이내에 클리어하면 스피드런 업적 달성. 0이면 기본값(30초) 사용.")]
    public float speedRunTime = 0f;
}
