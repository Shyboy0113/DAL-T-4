using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapColorSetter : MonoBehaviour
{
    private Tilemap tilemap;
    public Game_Button DALT;

    void Awake()
    {
        tilemap = GetComponent<Tilemap>();
        
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab) && DALT.isPause == false)
        {
            Debug.Log("색상 변경");
            
            if (tilemap.color != Color.white)
            {
                tilemap.color = Color.white;
            }
            else
            {
                tilemap.color = Color.black;
            }
        }
    }
}
