using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    [SerializeField] private List<EnemyBehaviour> _enemies = new List<EnemyBehaviour>();
    [SerializeField] private float delayTime; // 적의 공격/이동 애니메이션 끝날 때까지 대기

    [Header("Enemy Containers")]
    [SerializeField] private Transform unassignedContainer;
    [SerializeField] private Transform map1Container;
    [SerializeField] private Transform map2Container;

    public bool IsAnyEnemyActing { get; private set; } // BehaviourManager가 확인

    private void Awake()
    {
        InsertEnemy(); // Inspector 창에서 적이 할당돼있지 않을 때, 자동으로 할당
    }

    private void InsertEnemy()
    {
        if (_enemies.Count == 0)
        {
            _enemies.AddRange(GetComponentsInChildren<EnemyBehaviour>(true));
        }

        foreach (var enemy in _enemies)
            enemy.gameObject.SetActive(false);
    }
    
    public void InitEnemies()
    {
        foreach (var enemy in _enemies)
        {
            if (!enemy.gameObject.activeSelf) continue;
            enemy.Init();
        }
    }

    public void SpawnEnemies(int enemyNum)
    {
        int map1Layer = LayerMask.NameToLayer("Map 1");
        int map2Layer = LayerMask.NameToLayer("Map 2");

        var allTiles = FindObjectsByType<TileBehaviour>(FindObjectsSortMode.None);

        var map1Spawns = new List<Vector3>();
        var map2Spawns = new List<Vector3>();

        foreach (var tile in allTiles)
        {
            if (tile.currentTileType == TileType.FirstEnemySpawn)
                map1Spawns.Add(tile.transform.position);
            else if (tile.currentTileType == TileType.SecondEnemySpawn)
                map2Spawns.Add(tile.transform.position);
        }

        var spawnList = new List<(Vector3 pos, int layer)>();
        foreach (var pos in map1Spawns) spawnList.Add((pos, map1Layer));
        foreach (var pos in map2Spawns) spawnList.Add((pos, map2Layer));

        int activeCount = Mathf.Min(enemyNum, spawnList.Count);

        for (int i = 0; i < _enemies.Count; i++)
        {
            if (i < activeCount)
            {
                var (pos, layer) = spawnList[i];
                _enemies[i].gameObject.layer = layer;
                _enemies[i].SetStartPosition(pos);

                if (layer == map1Layer)
                    _enemies[i].transform.SetParent(map1Container, false);
                else if (layer == map2Layer)
                    _enemies[i].transform.SetParent(map2Container, false);

                _enemies[i].gameObject.SetActive(true);
                _enemies[i].Init();
            }
            else
            {
                _enemies[i].gameObject.SetActive(false);
                _enemies[i].transform.SetParent(unassignedContainer, false);
            }
        }
    }
    
    // 플레이어 이동 -> 타일맵 효과 적용 -> 적 턴 넘어감(이 때 호출됨)
    public void StartAllEnemiesTurn(Vector3 playerPosition)
    {
        StartCoroutine(IStartAllEnemiesTurn(playerPosition));
    }

    private IEnumerator IStartAllEnemiesTurn(Vector3 playerPosition)
    {
        IsAnyEnemyActing = true;
        
        foreach (var enemy in _enemies)
        {
            if (enemy.IsDead) continue; // 죽은 적은 제외
            
            // 각 적에게 플레이어의 위치를 주고 행동하게 함
            // 추후에 알고리즘을 넣어 동선을 짜야 함
            enemy.TakeTurn(playerPosition);
            
            // 적의 이동/공격 애니메이션이 끝날 때까지 대기 시간을 부여함
            yield return new WaitForSeconds(delayTime);
        }
        
        IsAnyEnemyActing = false;

    }
    
}
