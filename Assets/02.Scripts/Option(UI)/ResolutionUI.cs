using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // DropDown을 사용하기 위한 기능

using TMPro; // TextMeshPro

public class ResolutionUI : MonoBehaviour
{
    [SerializeField]
    private Resolution[] resolutions;

    public TMP_Dropdown resolutionDropdown;
    public Toggle fullscreenToggle;

    private bool isOk = false;
    
    private Resolution _selectedResolution;
    private Resolution _originalResolution;

    private void OnEnable()
    {
        _originalResolution = Screen.currentResolution;
    }

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

            // Resolution.Equals()를 사용하여 가로, 세로, 주사율까지 정확하게 비교
            if (resolutions[i].Equals(Screen.currentResolution))
            {
                currentResolutionIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue(); // 새로 고침

        _selectedResolution = Screen.currentResolution;
    }

    public void SetResolution(int resolutionIndex)
    {
        isOk = false;
        _selectedResolution = resolutions[resolutionIndex];
    }

    public void SetOk()
    {
        isOk = true;
        Screen.SetResolution(_selectedResolution.width, _selectedResolution.height, Screen.fullScreen);
        
        CutoutFade fade = FindObjectOfType<CutoutFade>().GetComponent<CutoutFade>();
        if (fade is not null) fade.ResizeResolution();

    }

    public void SetFullScreen(bool isFullScreen)
    {
        Screen.fullScreen = isFullScreen;
        PlayerPrefs.SetInt("FullScreen", isFullScreen ? 1 : 0);
    }

    public void Return()
    {
        if (isOk && _originalResolution.width != Screen.currentResolution.width &&
            _originalResolution.height != Screen.currentResolution.height)
        {
            Screen.SetResolution(_originalResolution.width, _originalResolution.height, Screen.fullScreen);
        }
    }
}