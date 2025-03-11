using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileColliderTrigger : MonoBehaviour
{
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
            GameManager.Instance.TileOut();
        }
    }
}
