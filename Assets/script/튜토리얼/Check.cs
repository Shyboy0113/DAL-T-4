using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Check : MonoBehaviour
{
    public GameObject Tutorial_UI_1;
    public GameObject Tutorial_UI_2;
    public GameObject Tutorial_UI_3;
    public GameObject Panel;

    public GameObject Tutorial_Skip;
    void Awake()
    {
        if (Tutorial.Skip == true) { SceneManager.LoadScene(4); return; }
    }

    public void Ok_1()
    {
        Tutorial_UI_1.SetActive(false);
        Tutorial_UI_2.SetActive(true);
    }
        public void Ok_2()
    {
        Tutorial_UI_2.SetActive(false);
        Tutorial_UI_3.SetActive(true);
        
    }

    public void Ok_3()
    {
        Tutorial_UI_3.SetActive(false);
        Panel.SetActive(false);
        Tutorial_Skip.SetActive(true);
    }

    public void _Skip()
    {
        Panel.SetActive(false);
        Tutorial_Skip.SetActive(true);
    }
    public void next()
    {
        SceneManager.LoadScene(4);
    }

}
