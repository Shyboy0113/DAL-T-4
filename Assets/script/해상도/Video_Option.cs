using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // DropDown을 사용하기 위한 기능
using UnityEngine.SceneManagement; // 장면 전환에 필요한 기능

public class Video_Option : MonoBehaviour
{
    private Resolution[] resolutions;

    public Dropdown resolutionDropdown;
    public Toggle fullscreenToggle;

    private bool isOk = false;
    private Resolution selectedResolution;

    void Start()
    {
        bool isFullScreen = (PlayerPrefs.GetInt("FullScreen", 1) == 1);
        Screen.fullScreen = isFullScreen;

        fullscreenToggle.isOn = isFullScreen;

        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        int currentResolutionIndex = 0;

        List<string> options = new List<string>();

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height + " @ " + resolutions[i].refreshRate + "hz";
            options.Add(option);

            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue(); // 새로 고침

        selectedResolution = Screen.currentResolution;
    }

    public void SetResolution(int resolutionIndex)
    {
        isOk = false;
        selectedResolution = resolutions[resolutionIndex];
    }

    public void SetOk()
    {
        isOk = true;
        Screen.SetResolution(selectedResolution.width, selectedResolution.height, Screen.fullScreen);
    }

    public void SetFullScreen(bool isFullScreen)
    {
        Screen.fullScreen = isFullScreen;
        PlayerPrefs.SetInt("FullScreen", isFullScreen ? 1 : 0);
    }

    public void Return()
    {
        if (isOk && selectedResolution.width != Screen.currentResolution.width &&
            selectedResolution.height != Screen.currentResolution.height)
        {
            Screen.SetResolution(selectedResolution.width, selectedResolution.height, Screen.fullScreen);
        }
        SceneManager.LoadScene(1);
    }
}