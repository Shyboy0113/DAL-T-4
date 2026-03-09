using System.Collections;
using UnityEngine;

public class EnemyBehaviour : MonoBehaviour
{
    private bool _isDead = false;
    public bool IsDead => _isDead;

    public void TakeTurn(Vector3 playerPosition)
    {
        // Undo 연동
        // 1. 적의 행동을 커맨드로 생성하여 실행
        // StackManager에서 적의 커맨드도 관리하게 하거나, 즉시 실행 후 스택에 추가함

        // 적이 사망 애니메이션 상태라면, Dead -> Idle로 복구
        
        // Redo 연동
        // 현재는 Redo와 일반 커맨드의 작동 로직이 동일하지만, Redo는 일반 커맨드와 달리 즉시 Rotation이나 상태가 반영돼야 함
        // 예를 들어, 플레이어 이동 후 적 사망 -> Undo시 플레이어 이동 직전 transform과 적의 idle 상태 복구
        // 이 때 Redo를 할 경우, 플레이어의 transform과 적의 Dead 상태 다시 복구
        
        StartCoroutine(EndTurn());
    }

    IEnumerator EndTurn(float delay = 0f)
    {
        yield return new WaitForSeconds(delay);
        GameEvents.RaisePlayerTurnStarted(); // 플레이어에게 턴을 돌려줍니다.
    }
    
}
