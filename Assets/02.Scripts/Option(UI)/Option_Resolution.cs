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

    [Tooltip("true: 드롭다운/토글 변경 즉시 화면에 반영 (취소 시 롤백)\nfalse: 확인 버튼을 눌러야 화면에 반영")]
    [SerializeField] private bool isAfterApply = false;

    private void Awake()
    {
        _cutoutFade = FindFirstObjectByType<CutoutFade>();
        InitializeResolutions();
        ApplySavedResolution();
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

        // SetValueWithoutNotify: OnValueChanged 이벤트 발동 없이 UI만 갱신
        resolutionDropdown.SetValueWithoutNotify(index);
        resolutionDropdown.RefreshShownValue();

        _selectedResolution = resolutions[index];

        bool isFullScreen = (PlayerPrefs.GetInt(FullScreenPrefKey, 0) == 1);
        ApplyResolution(_selectedResolution, isFullScreen);
    }

    // --- 패널 열릴 때 동기화 ---
    private void SyncFullscreenToggle()
    {
        if (fullscreenToggle != null)
            fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);
    }

    // 항상 SetValueWithoutNotify 사용 — 동기화 목적의 호출이 SetResolution을 발동시키면 안 됨
    private void SyncDropdownToCurrentResolution()
    {
        if (resolutionDropdown == null || resolutions == null || resolutions.Length == 0)
            return;

        int savedWidth  = PlayerPrefs.GetInt(ResolutionWidthKey, 1920);
        int savedHeight = PlayerPrefs.GetInt(ResolutionHeightKey, 1080);

        int index = FindResolutionIndex(savedWidth, savedHeight);
        if (index != -1)
        {
            resolutionDropdown.SetValueWithoutNotify(index);
            resolutionDropdown.RefreshShownValue();
            _selectedResolution = resolutions[index];
        }
    }

    // --- 공통 해상도 적용 ---
    private void ApplyResolution(Resolution resolution, bool isFullScreen)
    {
        Screen.fullScreen = isFullScreen;

        if (isFullScreen)
        {
            Resolution native = Screen.currentResolution;
            Screen.SetResolution(native.width, native.height, true);
        }
        else
        {
            Screen.SetResolution(resolution.width, resolution.height, false);
        }

        if (_cutoutFade != null) _cutoutFade.ResizeResolution();

        var letterbox = Camera.main?.GetComponent<LetterboxCamera>();
        if (letterbox != null)
            letterbox.Apply(resolution.width, resolution.height, isFullScreen);
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

        // isAfterApply=true: 선택만 저장, Apply()에서 반영
        // isAfterApply=false: 드롭다운 변경 즉시 화면 반영
        if (!isAfterApply)
            ApplyResolution(_selectedResolution, _selectedFullScreen);
    }

    public void SetFullScreen(bool isFullScreen)
    {
        _selectedFullScreen = isFullScreen;

        if (!isAfterApply)
            ApplyResolution(_selectedResolution, _selectedFullScreen);
    }

    // 확인 버튼
    public void Apply()
    {
        PlayerPrefs.SetInt(FullScreenPrefKey, _selectedFullScreen ? 1 : 0);
        PlayerPrefs.SetInt(ResolutionWidthKey, _selectedResolution.width);
        PlayerPrefs.SetInt(ResolutionHeightKey, _selectedResolution.height);
        PlayerPrefs.Save();

        // isAfterApply=false: 이미 적용됐지만 안전망으로 한 번 더
        // isAfterApply=true: 이 시점에 처음으로 화면 적용
        ApplyResolution(_selectedResolution, _selectedFullScreen);
    }

    // 취소 / ESC
    public void CancelChange()
    {
        _selectedResolution = _originalResolution;
        _selectedFullScreen = _originalFullScreen;

        int index = FindResolutionIndex(_originalResolution.width, _originalResolution.height);
        if (index != -1)
        {
            resolutionDropdown.SetValueWithoutNotify(index);
            resolutionDropdown.RefreshShownValue();
        }

        fullscreenToggle.SetIsOnWithoutNotify(_originalFullScreen);

        // isAfterApply=true: 화면은 변하지 않았으므로 롤백 불필요
        // isAfterApply=false: 미리보기로 변경된 화면을 원래대로 롤백
        if (!isAfterApply)
            ApplyResolution(_originalResolution, _originalFullScreen);
    }
}