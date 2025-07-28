using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Intro_SceneChange : MonoBehaviour
{
    public GameObject audioSource;

    public GameObject optionPanel;

    private void Awake()
    {
        Time.timeScale = 1;
        audioSource = GameObject.Find("BackGroundMusic");
    }
    public void Scene_Start()
    {
        if (audioSource)
        {
            Destroy(audioSource);
        }
        SceneManager.LoadScene(1);
        
    }
    public void Scene_Option()
    {
        optionPanel.SetActive(true);
    }

    public void Scene_Exit()
    {
        Application.Quit();
    }

}
