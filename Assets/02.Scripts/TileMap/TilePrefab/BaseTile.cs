using UnityEngine;

public abstract class BaseTile : MonoBehaviour
{
    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        // 플레이어만 감지
        if (collision.TryGetComponent(out PlayerBehaviour player))
        {
            OnPlayerEnter(player);
        }
        else if (collision.TryGetComponent(out EnemyBehaviour enemy))
        {
            OnEnemyEnter(enemy);
        }
    }

    // 각 타일이 구현할 핵심 메서드
    protected abstract void OnPlayerEnter(PlayerBehaviour player);
    
    protected abstract void OnEnemyEnter(EnemyBehaviour enemy);
}