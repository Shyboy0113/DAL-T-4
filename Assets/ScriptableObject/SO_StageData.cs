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

// 3번째 도전과제에 복수 선택 가능한 조건 (Flags — 인스펙터에서 체크박스로 다중 선택)
[System.Flags]
public enum ThirdMissionCondition
{
    None              = 0,
    TimeLimit         = 1 << 0,   // 제한 시간 내 클리어
    MoveCountLimit    = 1 << 1,   // 특정 횟수 이하 키 사용
    KillAllEnemies    = 1 << 2,   // 모든 적 처치
    NoSpecificFeature = 1 << 3,   // 특정 기능 미사용
}

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
    // Ice모드가 발동된 상태로 텔레포트를 타면, Ice 모드가 이후 유지되는지에 대한 여부
    public bool continueIceModeAfterTeleport = false;
    
    public bool canUseF4 = true;
    public bool canUseLeftALT = true;
    public bool canUseTAB = true;
    
    public bool hasSecondMap = false;

    public int limitNumberALT;
    public int limitNumberF4;
    public int limitNumberTAB;
    
    [Header("미션 및 도전과제 (Missions)")]
    public float limitTime;

    public int missionActionCount;
    public int missionFeatureUsageLimit; // 0 = 완전 미사용, N = N회 이하 허용
    
    public MissionType firstMissionType  = MissionType.StageClear;
    public MissionType secondMissionType = MissionType.CollectStar;

    // 3번째 도전과제 — 복수 조건 AND, 디폴트는 4가지 모두 활성
    public ThirdMissionCondition thirdMissionConditions =
        ThirdMissionCondition.TimeLimit      |
        ThirdMissionCondition.MoveCountLimit |
        ThirdMissionCondition.KillAllEnemies |
        ThirdMissionCondition.NoSpecificFeature;

    public ForbiddenFeature forbiddenFeature = ForbiddenFeature.None;
    // 게임 시작 시, 카메라가 움직이는 모드 설정
    public CameraTrackingMode trackingMode = CameraTrackingMode.FrameEntireMap;
}
