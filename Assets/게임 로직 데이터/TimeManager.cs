using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public static float maxTime = 150f;
    public float currentTime;

    public GameObject gameOverPanel;

    void Start()
    {
        ResetTime();
    }

    // Update is called once per frame
    void Update()
    {
        currentTime -= Time.deltaTime;
        if (currentTime <= 0)
        {
            if(gameOverPanel is not null)
            {
                gameOverPanel.SetActive(true);
            }
        }
    }

    public void ResetTime()
    {
        currentTime = maxTime;
    }

}
