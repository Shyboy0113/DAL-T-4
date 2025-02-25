using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_PlayerController : MonoBehaviour
{
    UI_TimingManager theTimingManager;

    public Game_Button DALT;
    void Start()
    {
        theTimingManager = FindObjectOfType<UI_TimingManager>();
    }

    // Update is called once per frame
    void Update()
    {
        if (DALT.isPause == false &&(
            (Input.GetKeyUp(KeyCode.F4))||(Input.GetKeyDown(KeyCode.Tab))))
        {
            theTimingManager.CheckTiming();
        }
    }
}
