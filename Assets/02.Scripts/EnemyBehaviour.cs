using System;
using System.Collections;
using UnityEngine;

public class EnemyBehaviour : MonoBehaviour
{
    private bool _isDead = false;
    public bool IsDead => _isDead;

    [SerializeField] private Vector3 startPosition;
    [SerializeField] private BehaviourManager behaviourManager;
    
    private Animator _animator;
    private Collider2D _collider2D;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _collider2D = GetComponent<Collider2D>();
    }

    public void Init()
    {
        transform.position = startPosition;
        SetDeadState(false);
    }

    public void SetDeadState(bool isDead)
    {
        _isDead = isDead;
        _collider2D.enabled = !isDead;
        
        if (isDead)
        {
            _animator.Play("Explosion");
        }
        else
        {
            _animator.Play("Idle");
        }

    }
    
    public void TakeTurn(Vector3 playerPosition)
    {
        if (_isDead) return;
        
        // 1. AI 로직: playerPosition을 향해 이동할 Vector3 계산
        Vector3 nextPosition = CalculateMove(playerPosition);
        // 2. 적 이동 명령 생성 및 매니저를 통한 실행
        ICommand moveCommand = new EnemyMoveCommand(this, nextPosition);
        
        if (behaviourManager != null)
        {
            behaviourManager.ExecuteCommand(moveCommand);
        }
    }
    
    private Vector3 CalculateMove(Vector3 targetPosition)
    {
        Vector3Int startCoordination = Vector3Int.FloorToInt(transform.position);
        
        Vector3Int[] directions = {Vector3Int.up , Vector3Int.down , Vector3Int.left , Vector3Int.right };

        Vector3Int bestMove = startCoordination;
        float minDistance = Vector3.Distance(transform.position, targetPosition);

        foreach (var dir in directions)
        {
            Vector3Int nextCoordination = startCoordination + dir;
            
            // 이동 가능한 타일인지 체크
            if (IsWalkable(nextCoordination))
            {
                Vector3 nextPosition =
                    new Vector3(nextCoordination.x + 0.5f, nextCoordination.y + 0.5f, 0);
                
                float dist = Vector3.Distance(nextPosition, targetPosition);

                if (dist < minDistance)
                {
                    minDistance = dist;
                    bestMove = nextCoordination;
                }
            }
        }
        return new Vector3(bestMove.x + 0.5f, bestMove.y + 0.5f , transform.position.z); 
    }

    private bool IsWalkable(Vector3Int cellPos)
    {
        // 해당 위치의 모든 콜라이더를 체크
        Collider2D hit = Physics2D.OverlapPoint(new Vector2(cellPos.x, cellPos.y));

        if (hit == null) return false; // 타일 자체가 없으면 (낭떠러지) 이동 불가

        TileBehaviour tile = hit.GetComponent<TileBehaviour>();
        if (tile != null)
        {
            // [수정] TileBehaviour에서 파괴된 타일은 렌더러가 꺼져있음을 확인하여 체크
            // 혹은 전용 bool 변수를 TileBehaviour에 추가하여 체크하는 것이 더 정확합니다.
            SpriteRenderer sr = hit.GetComponentInChildren<SpriteRenderer>();
            if (sr != null && !sr.enabled) return false; // 파괴된 타일은 통과 불가
        }
        return true; // 갈 수 있는 타일
    }
    
}
