using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Option_Resolution : MonoBehaviour
{
    [Header("Resolutions")]
    public TMP_Dropdown resolutionDropdown;
    public Resolution[] resolutions;

    private const string ResolutionWidthKey  = "ResolutionWidth";
    private const string ResolutionHeightKey = "ResolutionHeight";

    [Header("Fullscreen")]
    public Toggle fullscreenToggle;
    private const string FullScreenPrefKey = "FullScreen";
    private bool _selectedFullScreen;
    private bool _originalFullScreen;

    [Header("Rollback&Apply")]
    private Resolution _selectedResolution;
    private Resolution _originalResolution;

    [Header("CutOutFade")]
    private CutoutFade _cutoutFade;

    private void Awake()
    {
        _cutoutFade = FindFirstObjectByType<CutoutFade>();
        InitializeResolutions();
    }

    private void OnEnable()
    {
        int savedWidth  = PlayerPrefs.GetInt(ResolutionWidthKey, 1920);
        int savedHeight = PlayerPrefs.GetInt(ResolutionHeightKey, 1080);

        int index = FindResolutionIndex(savedWidth, savedHeight);
        if (index == -1) index = FindResolutionIndex(1920, 1080);
        if (index == -1) index = resolutions.Length - 1;
        _originalResolution = resolutions[index];

        _originalFullScreen = Screen.fullScreen;
        _selectedFullScreen = Screen.fullScreen;

        SyncFullscreenToggle();
        SyncDropdownToCurrentResolution();
    }

    private void Start()
    {
        InitializeFullscreen();
        ApplySavedResolution();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            _selectedFullScreen = Screen.fullScreen;
            fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);
            SyncDropdownToCurrentResolution();
        }
    }

    // --- 초기화 ---
    private void InitializeFullscreen()
    {
        bool isFullScreen = (PlayerPrefs.GetInt(FullScreenPrefKey, 0) == 1);
        fullscreenToggle.isOn = isFullScreen;
        Screen.fullScreen = isFullScreen;
    }

    private const float TargetAspect    = 16f / 9f;
    private const float AspectTolerance = 0.01f;

    private void InitializeResolutions()
    {
        Dictionary<string, Resolution> uniqueResolutions = new Dictionary<string, Resolution>();
        foreach (Resolution res in Screen.resolutions)
        {
            float aspect = (float)res.width / res.height;
            if (Mathf.Abs(aspect - TargetAspect) > AspectTolerance) continue;

            string key = res.width + "x" + res.height;
            if (!uniqueResolutions.ContainsKey(key) || res.refreshRateRatio.value > uniqueResolutions[key].refreshRateRatio.value)
            {
                uniqueResolutions[key] = res;
            }
        }

        if (uniqueResolutions.Count == 0)
        {
            Resolution fallback = new Resolution { width = 1920, height = 1080 };
            uniqueResolutions["1920x1080"] = fallback;
        }

        List<Resolution> filteredResolutions = new List<Resolution>(uniqueResolutions.Values);
        filteredResolutions.Sort((a, b) =>
        {
            if (a.width != b.width) return a.width.CompareTo(b.width);
            return a.height.CompareTo(b.height);
        });
        resolutions = filteredResolutions.ToArray();

        resolutionDropdown.ClearOptions();
        List<string> options = new List<string>();

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height + " @ " +
                            Mathf.RoundToInt((float)resolutions[i].refreshRateRatio.value) + "hz";
            options.Add(option);
        }

        resolutionDropdown.AddOptions(options);
    }

    private void ApplySavedResolution()
    {
        int savedWidth  = PlayerPrefs.GetInt(ResolutionWidthKey, 1920);
        int savedHeight = PlayerPrefs.GetInt(ResolutionHeightKey, 1080);

        int index = FindResolutionIndex(savedWidth, savedHeight);

        if (index == -1)
        {
            index = FindResolutionIndex(1920, 1080);
            if (index == -1)
                index = resolutions.Length - 1;
        }

        resolutionDropdown.value = index;
        resolutionDropdown.RefreshShownValue();

        _selectedResolution = resolutions[index];
        
        bool isFullScreen = (PlayerPrefs.GetInt(FullScreenPrefKey, 0) == 1);
        
            if (isFullScreen)
            {
                Resolution native = Screen.currentResolution;
                Screen.SetResolution(native.width, native.height, true);
            }
            else
            {
                Screen.SetResolution(_selectedResolution.width, _selectedResolution.height, false);
            }
        
            if (_cutoutFade != null) _cutoutFade.ResizeResolution();
    }

    // --- 패널 열릴 때 동기화 ---
    private void SyncFullscreenToggle()
    {
        if (fullscreenToggle != null)
            fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);
    }

    private void SyncDropdownToCurrentResolution()
    {
        if (resolutionDropdown == null || resolutions == null || resolutions.Length == 0)
            return;

        int savedWidth  = PlayerPrefs.GetInt(ResolutionWidthKey, 1920);
        int savedHeight = PlayerPrefs.GetInt(ResolutionHeightKey, 1080);

        int index = FindResolutionIndex(savedWidth, savedHeight);
        if (index != -1)
        {
            resolutionDropdown.value = index;
            resolutionDropdown.RefreshShownValue();
            _selectedResolution = resolutions[index];
        }
    }

    // --- 유틸리티 ---
    private int FindResolutionIndex(int width, int height)
    {
        for (int i = 0; i < resolutions.Length; i++)
        {
            if (resolutions[i].width == width && resolutions[i].height == height)
                return i;
        }
        return -1;
    }

    // --- 외부 호출 ---
    public void SetResolution(int resolutionIndex)
    {
        _selectedResolution = resolutions[resolutionIndex];
    }

    public void SetFullScreen(bool isFullScreen)
    {
        _selectedFullScreen = isFullScreen;
    }

    // OK 버튼
    public void ApplyAndClose()
    {
        Screen.fullScreen = _selectedFullScreen;
        PlayerPrefs.SetInt(FullScreenPrefKey, _selectedFullScreen ? 1 : 0);

        if (_selectedFullScreen)
        {
            Resolution native = Screen.currentResolution;
            Screen.SetResolution(native.width, native.height, true);
        }
        else
        {
            Screen.SetResolution(_selectedResolution.width, _selectedResolution.height, false);
        }

        PlayerPrefs.SetInt(ResolutionWidthKey, _selectedResolution.width);
        PlayerPrefs.SetInt(ResolutionHeightKey, _selectedResolution.height);
        PlayerPrefs.Save();

        if (_cutoutFade != null) _cutoutFade.ResizeResolution();

        // 패널이 닫히기 전에 동기 적용 (코루틴은 panel.SetActive(false) 시 소멸)
        var letterbox = Camera.main?.GetComponent<LetterboxCamera>();
        if (letterbox != null)
            letterbox.Apply(_selectedResolution.width, _selectedResolution.height, _selectedFullScreen);
    }

    // Cancel / ESC
    public void CancelChange()
    {
        _selectedResolution = _originalResolution;
        int index = FindResolutionIndex(_originalResolution.width, _originalResolution.height);
        if (index != -1)
        {
            resolutionDropdown.value = index;
            resolutionDropdown.RefreshShownValue();
        }

        _selectedFullScreen = _originalFullScreen;
        fullscreenToggle.SetIsOnWithoutNotify(_originalFullScreen);
    }
}