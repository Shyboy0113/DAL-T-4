using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    [SerializeField] private List<EnemyBehaviour> _enemies = new List<EnemyBehaviour>();
    [SerializeField] private float delayTime; // 적의 공격/이동 애니메이션 끝날 때까지 대기
    
    public bool IsAnyEnemyActing { get; private set; } // BehaviourManager가 확인

    public void InitEnemies()
    {
        foreach (var enemy in _enemies)
        {
            enemy.Init();
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
