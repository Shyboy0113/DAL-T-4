using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Camera_Rotate : MonoBehaviour
{
    int rotate;

    public Game_Button DALT;

    private void Start()
    {
        rotate = 180;
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab) &&DALT.isPause == false)
        {
            transform.rotation = Quaternion.Euler(0, 0, rotate%360);
            rotate += 180;

        }
    }
}
