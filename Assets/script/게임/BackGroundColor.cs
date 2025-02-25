using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackGroundColor : MonoBehaviour
{

    private SpriteRenderer spriterenderer;
    void Start()
    {
        spriterenderer = GetComponent<SpriteRenderer>();
        spriterenderer.color = Color.white;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if(spriterenderer.color != Color.black)
            {
                spriterenderer.color = Color.black;
            }
            else
            {
                spriterenderer.color = Color.white;
            }

        }
    }
}
