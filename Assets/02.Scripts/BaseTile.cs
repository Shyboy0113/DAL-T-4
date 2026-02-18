using UnityEngine;

public abstract class BaseTile : MonoBehaviour
{
    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        // 플레이어만 감지
        if (collision.TryGetComponent(out StackManager player))
        {
            OnPlayerEnter(player);
        }
    }

    // 각 타일이 구현할 핵심 메서드
    protected abstract void OnPlayerEnter(StackManager player);
}