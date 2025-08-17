using System.Collections.Generic;
using System.Linq; // Sort를 위해 추가될 수 있으나, List.Sort는 기본 포함
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResolutionUI : MonoBehaviour
{
    // PlayerPrefs 키를 상수로 관리하여 오타 방지
    private const string FullScreenPrefKey = "FullScreen";

    public Resolution[] resolutions;

    public TMP_Dropdown resolutionDropdown;
    public Toggle fullscreenToggle;

    private Resolution _selectedResolution;
    private Resolution _originalResolution;
    private bool _isChangeApplied = false;
    
    private CutoutFade _cutoutFade;
    
    private void OnEnable()
    {
        _originalResolution = Screen.currentResolution;
    }

    private void Awake()
    {
        // GetComponentInChildren 또는 다른 안전한 방식으로 찾는 것을 고려해볼 수 있습니다.
        _cutoutFade = FindObjectOfType<CutoutFade>(); 
    }

    void Start()
    {
        InitializeFullscreen();
        InitializeResolutions();
    }
    
    // 전체 화면 관련 초기화를 담당하는 메서드
    private void InitializeFullscreen()
    {
        bool isFullScreen = (PlayerPrefs.GetInt(FullScreenPrefKey, 1) == 1);
        fullscreenToggle.isOn = isFullScreen;
        Screen.fullScreen = isFullScreen;
    }

    // 해상도 관련 초기화를 담당하는 메서드
    private void InitializeResolutions()
    {
        // 1. 중복 제거 및 최고 주사율 필터링
        Dictionary<string, Resolution> uniqueResolutions = new Dictionary<string, Resolution>();
        foreach (Resolution res in Screen.resolutions)
        {
            string key = res.width + "x" + res.height;
            if (!uniqueResolutions.ContainsKey(key) || res.refreshRateRatio.value > uniqueResolutions[key].refreshRateRatio.value)
            {
                uniqueResolutions[key] = res;
            }
        }
        
        // 2. 리스트로 변환 후 정렬
        List<Resolution> filteredResolutions = new List<Resolution>(uniqueResolutions.Values);
        filteredResolutions.Sort((a, b) => {
            if (a.width != b.width) return a.width.CompareTo(b.width);
            return a.height.CompareTo(b.height);
        });
        resolutions = filteredResolutions.ToArray();

        // 3. 드롭다운 채우기
        resolutionDropdown.ClearOptions();
        int currentResolutionIndex = 0;
        List<string> options = new List<string>();

        for (int i = 0; i < resolutions.Length; i++)
        {
            // [수정됨] .refreshRateRatio.value를 사용하고 정수로 반올림
            string option = resolutions[i].width + " x " + resolutions[i].height + " @ " +
                            Mathf.RoundToInt((float)resolutions[i].refreshRateRatio.value) + "hz";
            options.Add(option);

            if (resolutions[i].Equals(Screen.currentResolution))
            {
                currentResolutionIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();

        _selectedResolution = Screen.currentResolution;
    }

    public void SetResolution(int resolutionIndex)
    {
        _isChangeApplied = false;
        _selectedResolution = resolutions[resolutionIndex];
    }

    public void ApplyChange()
    {
        _isChangeApplied = true;
        Screen.SetResolution(_selectedResolution.width, _selectedResolution.height, Screen.fullScreen);
        
        if (_cutoutFade != null) _cutoutFade.ResizeResolution();
    }

    public void SetFullScreen(bool isFullScreen)
    {
        Screen.fullScreen = isFullScreen;
        PlayerPrefs.SetInt(FullScreenPrefKey, isFullScreen ? 1 : 0);
    }

    public void Return()
    {
        if (_isChangeApplied && !_originalResolution.Equals(Screen.currentResolution))
        {
            Screen.SetResolution(_originalResolution.width, _originalResolution.height, Screen.fullScreen);
        }
    }
}