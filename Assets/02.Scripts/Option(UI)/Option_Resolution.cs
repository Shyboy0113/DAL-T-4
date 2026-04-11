using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Option_Resolution : MonoBehaviour
{
    [Header("Resolutions")]
    public TMP_Dropdown resolutionDropdown;
    public Resolution[] resolutions;
    
    private const string ResolutionWidthKey = "ResolutionWidth"; //해상도 너비
    private const string ResolutionHeightKey = "ResolutionHeight"; //해상도 높이
    
    [Header("Fullscreen")]
    public Toggle fullscreenToggle;
    private const string FullScreenPrefKey = "FullScreen"; // 전체화면 저장

    [Header("Rollback&Apply")]
    private Resolution _selectedResolution;
    private Resolution _originalResolution;
    private bool _isChangeApplied = false;
    
    [Header("CutOutFade")]
    private CutoutFade _cutoutFade; // 해상도 조정 후, Fade용 Panel 해상도도 조정해줘야 함
    
    private void Awake()
    {
        // CutoutFade 컴포넌트를 씬에서 찾아옴
        _cutoutFade = FindObjectOfType<CutoutFade>(); 
        
        InitializeResolutions();
    }
    
    private void OnEnable()
    {
        int savedWidth = PlayerPrefs.GetInt(ResolutionWidthKey, 1920);
        int savedHeight = PlayerPrefs.GetInt(ResolutionHeightKey, 1080);
    
        int index = FindResolutionIndex(savedWidth, savedHeight);
        if (index != -1)
            _originalResolution = resolutions[index];

        SyncFullscreenToggle();
        SyncDropdownToCurrentResolution();
    }
    

    void Start()
    {
        InitializeFullscreen();
        ApplySavedResolution();
    }

    private void Update()
    {
        // Alt+Tab 등으로 전체화면 상태가 바뀌면 토글 자동 반영
        if (fullscreenToggle != null && fullscreenToggle.isOn != Screen.fullScreen)
        {
            fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);
        }
    }
    
    // --- 초기화 ---
    private void InitializeFullscreen()
    {
        bool isFullScreen = (PlayerPrefs.GetInt(FullScreenPrefKey, 0) == 1); // 기본값: 전체화면 아님
        fullscreenToggle.isOn = isFullScreen;
        Screen.fullScreen = isFullScreen;
    }

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
        filteredResolutions.Sort((a, b) =>
        {
            if (a.width != b.width) return a.width.CompareTo(b.width);
            return a.height.CompareTo(b.height);
        });
        resolutions = filteredResolutions.ToArray();

        // 3. 드롭다운 채우기
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

    // 저장된 해상도 불러와서 적용 (첫 실행 시)
    private void ApplySavedResolution()
    {
        int savedWidth = PlayerPrefs.GetInt(ResolutionWidthKey, 1920);
        int savedHeight = PlayerPrefs.GetInt(ResolutionHeightKey, 1080);

        int index = FindResolutionIndex(savedWidth, savedHeight);

        // 저장된 해상도가 목록에 없으면 기본값 1920x1080 시도, 그것도 없으면 마지막 항목
        if (index == -1)
        {
            index = FindResolutionIndex(1920, 1080);
            if (index == -1)
                index = resolutions.Length - 1;
        }

        resolutionDropdown.value = index;
        resolutionDropdown.RefreshShownValue();

        _selectedResolution = resolutions[index];
        Screen.SetResolution(_selectedResolution.width, _selectedResolution.height, Screen.fullScreen);

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

        int savedWidth = PlayerPrefs.GetInt(ResolutionWidthKey, 1920);
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

    // OK 버튼 — 확정 + 저장
    public void ApplyAndClose()
    {
        Screen.SetResolution(_selectedResolution.width, _selectedResolution.height, Screen.fullScreen);

        PlayerPrefs.SetInt(ResolutionWidthKey, _selectedResolution.width);
        PlayerPrefs.SetInt(ResolutionHeightKey, _selectedResolution.height);
        PlayerPrefs.Save();

        if (_cutoutFade != null) _cutoutFade.ResizeResolution();
    }

    // Cancel / ESC — 변경 취소
    public void CancelChange()
    {
        _selectedResolution = _originalResolution;

        int index = FindResolutionIndex(_originalResolution.width, _originalResolution.height);
        if (index != -1)
        {
            resolutionDropdown.value = index;
            resolutionDropdown.RefreshShownValue();
        }
    }

    public void SetFullScreen(bool isFullScreen)
    {
        Screen.fullScreen = isFullScreen;
        PlayerPrefs.SetInt(FullScreenPrefKey, isFullScreen ? 1 : 0);
    }
}