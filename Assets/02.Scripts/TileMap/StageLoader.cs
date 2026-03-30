using UnityEngine;
using System.Collections.Generic;

public class StageLoader : MonoBehaviour
{
    [SerializeField] private List<SO_StageData> stageList;
    [SerializeField] private Transform mapParent; // Tilemap Grid

    [Header("Sub Cameras (RenderTexture)")]
    [SerializeField] private Camera map1Camera;
    [SerializeField] private Camera map2Camera;

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
            CenterCamerasOnTiles();

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

    private void CenterCamerasOnTiles()
    {
        if (map1Camera == null && map2Camera == null) return;

        int map1Layer = LayerMask.NameToLayer("Map 1");
        int map2Layer = LayerMask.NameToLayer("Map 2");

        var tiles = FindObjectsByType<TileBehaviour>(FindObjectsSortMode.None);

        Vector3 map1Sum = Vector3.zero;
        int     map1Count = 0;
        Vector3 map2Sum = Vector3.zero;
        int     map2Count = 0;

        foreach (var tile in tiles)
        {
            int layer = tile.gameObject.layer;
            if (layer == map1Layer)
            {
                map1Sum += tile.transform.position;
                map1Count++;
            }
            else if (layer == map2Layer)
            {
                map2Sum += tile.transform.position;
                map2Count++;
            }
        }

        if (map1Count == 0) return;

        Vector3 map1Center = map1Sum / map1Count;
        Vector3 map2Center = map2Count > 0 ? map2Sum / map2Count : map1Center;

        if (map1Camera != null)
        {
            var pos = map1Camera.transform.position;
            map1Camera.transform.position = new Vector3(map1Center.x, map1Center.y, pos.z);
        }

        if (map2Camera != null)
        {
            var pos = map2Camera.transform.position;
            map2Camera.transform.position = new Vector3(map2Center.x, map2Center.y, pos.z);
        }
    }

    private void ResetPlayerStatus()
    {
        var player = FindObjectOfType<PlayerBehaviour>();
        if (player == null) return;

        Vector3? spawnPos = null;
        var tiles = FindObjectsByType<TileBehaviour>(FindObjectsSortMode.None);
        foreach (var tile in tiles)
        {
            if (tile.currentTileType == TileType.Start)
            {
                spawnPos = tile.transform.position;
                break;
            }
        }

        player.InitPlayer(spawnPos);
    }

}
