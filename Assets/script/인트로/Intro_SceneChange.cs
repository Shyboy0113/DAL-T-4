using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Intro_SceneChange : MonoBehaviour
{
    public GameObject audioSource;
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
        SceneManager.LoadScene(2);
        
    }
    public void Scene_Option()
    {
        SceneManager.LoadScene(3);
    }

    public void Scene_Exit()
    {
        Application.Quit();
    }

}
