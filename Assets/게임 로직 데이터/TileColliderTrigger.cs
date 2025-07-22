using System;
using UnityEngine;

public class TileColliderTrigger : MonoBehaviour
{
    // ? '플레이어가 타일을 벗어났다'는 이벤트 선언
    public static event Action OnPlayerExitedTile;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Debug.Log("타일에 들어왔습니다.");            
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Debug.Log("타일에서 벗어났습니다.");
            
            //GameManager.Instance.TileOut();
            OnPlayerExitedTile?.Invoke();
            
        }
    }
}
