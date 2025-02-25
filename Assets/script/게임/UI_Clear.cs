using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


public class UI_Clear : MonoBehaviour
{

    public GameObject Clear_UI;
    public Text Clear_Score;
    public Game_Button DALT;
    public GameObject Music;
    private void Awake()
    {
        Music = GameObject.Find("BackGroundMusic");
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        Time.timeScale = 0;
        Clear_UI.SetActive(true);
        DALT.isPause = true;
    }

    public void Title()
    {
        Destroy(Music);
        SceneManager.LoadScene(1);
        
    }


}
