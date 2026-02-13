using System;
using UnityEngine;

// OnPlayerExitedTile static event를 정의하는 클래스

public class TileColliderTrigger : MonoBehaviour
{
    // 플레이어가 타일을 벗어났을 때 작동하는 이벤트
    public static event Action OnPlayerExitedTile;
    
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            OnPlayerExitedTile?.Invoke();
        }
    }
    
}
