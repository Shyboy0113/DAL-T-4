using UnityEngine;

public class PlayerPrefsManager : MonoBehaviour
{
    private MapDataLoader _mapDataLoader;

    public int chapter;
    public int stage;

    public bool isFirstMissionCleared;
    public bool isSecondMissionCleared;
    public bool isThirdMissionCleared;

    public int minimumPushedNumberALT;
    public int minimumPushedNumberF4;
    public int minimumPushedNumberTAB;

    public bool isCleared;

    public float minimumTime;

    public void ReportData(MapDataLoader mapDataLoader, StageData currentData)
    {
        _mapDataLoader = mapDataLoader;        

        chapter = currentData.chapterNum;
        stage = currentData.stageNum;

        // ✅ 기존 데이터 불러오기
        LoadPlayerPrefs();

        // ✅ 도전과제 체크
        CheckTheChallenges(currentData);

        // ✅ 업데이트된 데이터 저장
        SavePlayerPrefs();
    }

    private void LoadPlayerPrefs()
    {
        isFirstMissionCleared = PlayerPrefs.GetInt($"Chapter_{chapter}_Stage_{stage}_FirstMissionCleared", 0) == 1;
        isSecondMissionCleared = PlayerPrefs.GetInt($"Chapter_{chapter}_Stage_{stage}_SecondMissionCleared", 0) == 1;
        isThirdMissionCleared = PlayerPrefs.GetInt($"Chapter_{chapter}_Stage_{stage}_ThirdMissionCleared", 0) == 1;

        minimumPushedNumberALT = PlayerPrefs.GetInt($"Chapter_{chapter}_Stage_{stage}_MinALT", int.MaxValue);
        minimumPushedNumberF4 = PlayerPrefs.GetInt($"Chapter_{chapter}_Stage_{stage}_MinF4", int.MaxValue);
        minimumPushedNumberTAB = PlayerPrefs.GetInt($"Chapter_{chapter}_Stage_{stage}_MinTAB", int.MaxValue);

        minimumTime = PlayerPrefs.GetFloat($"Chapter_{chapter}_Stage_{stage}_MinTime", 99999999f);

        isCleared = PlayerPrefs.GetInt($"Chapter_{chapter}_Stage_{stage}_Cleared", 0) == 1;
    }

    private void SavePlayerPrefs()
    {
        PlayerPrefs.SetInt($"Chapter_{chapter}_Stage_{stage}_FirstMissionCleared", isFirstMissionCleared ? 1 : 0);
        PlayerPrefs.SetInt($"Chapter_{chapter}_Stage_{stage}_SecondMissionCleared", isSecondMissionCleared ? 1 : 0);
        PlayerPrefs.SetInt($"Chapter_{chapter}_Stage_{stage}_ThirdMissionCleared", isThirdMissionCleared ? 1 : 0);

        PlayerPrefs.SetInt($"Chapter_{chapter}_Stage_{stage}_MinALT", minimumPushedNumberALT);
        PlayerPrefs.SetInt($"Chapter_{chapter}_Stage_{stage}_MinF4", minimumPushedNumberF4);
        PlayerPrefs.SetInt($"Chapter_{chapter}_Stage_{stage}_MinTAB", minimumPushedNumberTAB);

        PlayerPrefs.SetFloat($"Chapter_{chapter}_Stage_{stage}_MinTime", minimumTime);

        PlayerPrefs.SetInt($"Chapter_{chapter}_Stage_{stage}_Cleared", isCleared ? 1 : 0);
        PlayerPrefs.Save();  // ✅ 저장!
    }

    private void CheckTheChallenges(StageData currentData)
    {
        int currentALT = GameManager.Instance.pushedNumberALT;
        int currentF4 = GameManager.Instance.pushedNumberF4;
        int currentTAB = GameManager.Instance.pushedNumberTAB;
        float currentTime = GameManager.Instance.currentTime;

        if (GameManager.Instance != null)
        {
            currentALT = GameManager.Instance.pushedNumberALT;
            currentF4 = GameManager.Instance.pushedNumberF4;
            currentTAB = GameManager.Instance.pushedNumberTAB;
            currentTime = GameManager.Instance.currentTime;
        }
        else
        {
            Debug.LogWarning("GameManager.Instance is null. Using default values.");
            currentALT = 999;  // 적절한 기본값 설정
            currentF4 = 999;
            currentTAB = 999;
            currentTime = 99999999f;
        }


        // ✅ 스테이지 클리어 체크
        isCleared = true;

        // ✅ 도전과제 1: 게임에서 클리어
        isFirstMissionCleared = true;

        // ✅ 도전과제 2: 제한횟수 클리어
        if (currentData.limitNumberALT >= currentALT &&
            currentData.limitNumberF4 >= currentF4 &&
            currentData.limitNumberTAB >= currentTAB)
        {
            isSecondMissionCleared = true;

            // ✅ 키 입력 최소값 업데이트 (기존 값보다 작을 경우만 갱신)
            if (currentALT < minimumPushedNumberALT) minimumPushedNumberALT = currentALT;
            if (currentF4 < minimumPushedNumberF4) minimumPushedNumberF4 = currentF4;
            if (currentTAB < minimumPushedNumberTAB) minimumPushedNumberTAB = currentTAB;
        }


        // ✅ 도전과제 3: 제한 시간 이내 클리어
        if (currentTime <= currentData.limitTime)
        {
            isThirdMissionCleared = true;
            if(currentTime <= minimumTime)
            {
                minimumTime = currentTime;
            }
        }
    }
}
