using UnityEngine;
using Steamworks;

public class SteamAchievementManager : MonoBehaviour
{
    private void Start()
    {
        // 스팀 API 초기화 여부 확인
        if (!SteamManager.Initialized)
        {
            Debug.LogWarning("Steam API가 초기화되지 않았습니다. 스팀 클라이언트가 켜져 있는지 확인하세요.");
            return;
        }
    }

    /// <summary>
    /// 도전과제 달성 처리
    /// </summary>
    /// <param name="achievementId">스팀 파트너스에 등록된 도전과제 API Name</param>
    public void UnlockAchievement(string achievementId)
    {
        if (!SteamManager.Initialized) return;

        // 1. 내부적으로 도전과제 달성 상태로 변경
        SteamUserStats.SetAchievement(achievementId);
        
        // 2. 변경된 상태를 스팀 서버로 전송 (이 함수를 호출해야 스팀 오버레이 알림이 뜹니다)
        bool success = SteamUserStats.StoreStats();
        
        if (success)
            Debug.Log($"도전과제 [{achievementId}] 달성 및 서버 전송 요청 성공!");
        else
            Debug.LogError($"도전과제 [{achievementId}] 서버 저장 실패.");
    }

    /// <summary>
    /// 도전과제 달성 여부 확인
    /// </summary>
    public bool CheckAchievementStatus(string achievementId)
    {
        if (!SteamManager.Initialized) return false;

        bool isUnlocked;
        // 스팀 서버로부터 해당 도전과제의 상태를 받아옵니다.
        SteamUserStats.GetAchievement(achievementId, out isUnlocked);
        
        Debug.Log($"도전과제 [{achievementId}] 달성 여부: {isUnlocked}");
        return isUnlocked;
    }

    /// <summary>
    /// 도전과제 초기화 (개발 중 테스트 목적)
    /// </summary>
    public void ClearAchievement(string achievementId)
    {
        if (!SteamManager.Initialized) return;

        SteamUserStats.ClearAchievement(achievementId);
        SteamUserStats.StoreStats();
        Debug.Log($"도전과제 [{achievementId}] 상태가 초기화되었습니다.");
    }
}