using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class StageManager : MonoBehaviour
{
    private JsonDataManager jsonDataManager;
    private StageProgressData currentStageData;

    private void Start()
    {
        jsonDataManager = FindObjectOfType<JsonDataManager>();
    }

    public void ReportData(StageData currentData)
    {
        // 현재 스테이지 데이터 불러오기
        currentStageData = jsonDataManager.GetStageData(currentData.chapterNum, currentData.stageNum);

        // 도전과제 체크 및 업데이트
        CheckTheChallenges(currentData);

        // 업데이트된 데이터 저장
        jsonDataManager.SaveStageData(currentStageData);
    }

    private void CheckTheChallenges(StageData currentData)
    {
        int currentALT = GameManager.Instance?.pushedNumberALT ?? 999;
        int currentF4 = GameManager.Instance?.pushedNumberF4 ?? 999;
        int currentTAB = GameManager.Instance?.pushedNumberTAB ?? 999;
        float currentTime = GameManager.Instance?.currentTime ?? 99999999f;

        // ✅ 스테이지 클리어 체크
        currentStageData.isCleared = true;

        // ✅ 도전과제 1: 게임에서 클리어
        currentStageData.isFirstMissionCleared = true;

        // ✅ 도전과제 2: 제한 횟수 내 클리어
        if (currentData.limitNumberALT >= currentALT &&
            currentData.limitNumberF4 >= currentF4 &&
            currentData.limitNumberTAB >= currentTAB)
        {
            currentStageData.isSecondMissionCleared = true;

            // 최소 키 입력 갱신
            currentStageData.minALT = Mathf.Min(currentStageData.minALT, currentALT);
            currentStageData.minF4 = Mathf.Min(currentStageData.minF4, currentF4);
            currentStageData.minTAB = Mathf.Min(currentStageData.minTAB, currentTAB);
        }

        // ✅ 도전과제 3: 제한 시간 이내 클리어
        if (currentTime <= currentData.limitTime)
        {
            currentStageData.isThirdMissionCleared = true;
            currentStageData.minClearTime = Mathf.Min(currentStageData.minClearTime, currentTime);
        }
    }
}
