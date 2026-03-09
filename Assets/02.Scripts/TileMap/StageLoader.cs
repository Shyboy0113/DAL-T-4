using UnityEngine;
using System.Collections.Generic;

public class StageLoader : MonoBehaviour
{
    [SerializeField] private List<SO_StageData> stageList;
    [SerializeField] private Transform mapParent; // Tilemap Grid
    
    private GameObject _currentStageObject;

    public bool LoadStage(int chapterNum, int stageNum)
    {
        SO_StageData stageData = stageList.Find(
            x => x.chapterNum == chapterNum && x.stageNum == stageNum);

        if (stageData != null)
        {
            // 1. 기존 스테이지 제거
            if (_currentStageObject != null)
            {
                Destroy(_currentStageObject);
            }
            
            _currentStageObject = Instantiate(stageData.stagePrefab, mapParent);
            
            var mapManager = FindObjectOfType<MapManager>();
            if (mapManager != null)
            {
                mapManager.InitializeNewStage(_currentStageObject);
            }
            
            ResetPlayerStatus();
            
            GameManager.Instance.canUseF4 = stageData.canUseF4;
            GameManager.Instance.canUseLeftALT = stageData.canUseLeftALT;
            GameManager.Instance.canUseTAB = stageData.canUseTAB;
            
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.RenewalBGM(stageData.audioClip, stageData.stageName);
            }
            
            return true; //로드 성공
        }
        return false;
    }

    private void ResetPlayerStatus()
    {
        var player = FindObjectOfType<PlayerBehaviour>();
        if (player != null)
        {
            player.InitPlayer();
        }
    }

}
