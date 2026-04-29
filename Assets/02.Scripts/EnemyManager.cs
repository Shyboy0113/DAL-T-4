using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
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

    void OnEnable()
    {
        GameEvents.MapInitialized += SpawnEnemies;
    }

    void OnDisable()
    {
        GameEvents.MapInitialized -= SpawnEnemies;
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

    public void SpawnEnemies()
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

        int activeCount = Mathf.Min(spawnList.Count, _enemies.Count);

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
    
    public void StopAllEnemiesTurn()
    {
        StopAllCoroutines();
        IsAnyEnemyActing = false;
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
            if (!enemy.gameObject.activeSelf || enemy.IsDead) continue;

            enemy.TakeTurn(playerPosition);
            yield return new WaitForSeconds(delayTime);

            // 타일 로직 턴: 이동 중 등록된 타일 효과 실행 (TrapToggle 사망 등)
            GameEvents.RaiseTileLogicTurnStarted();
            yield return null;

            // 물리 턴: Player/Enemy 낙사 판정
            GameEvents.RaisePhysicsTurnStarted();
            yield return null;

            // 플레이어가 이번 적의 행동으로 사망했다면 나머지 적은 행동하지 않음
            if (GameManager.Instance.isGameOver) break;
        }

        IsAnyEnemyActing = false;
    }
    
}
