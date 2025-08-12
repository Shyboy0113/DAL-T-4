using System;
using UnityEngine;

// '플레이어가 타일을 벗어났다'는 이벤트
// OnPlayerExitedTile static 이벤트 보유

public class TileColliderTrigger : MonoBehaviour
{
    // 플레이어가 타일 맵을 벗어났을 때의 이벤트
    public static event Action OnPlayerExitedTile;
    
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            OnPlayerExitedTile?.Invoke();
        }
    }
    
}
