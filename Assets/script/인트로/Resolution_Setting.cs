using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class Resolution_Setting : MonoBehaviour
{
    private void Start()
    {
        SetResolution();
    }

    public void SetResolution()
    {

        int setWidth = 1920;
        int setHeight = 1080;

        Screen.SetResolution(setWidth, setHeight, true);

        SceneManager.LoadScene(1);

    }
}
