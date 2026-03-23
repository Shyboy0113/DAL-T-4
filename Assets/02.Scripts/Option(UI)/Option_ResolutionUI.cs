using System.Collections.Generic;
using System.Linq; // Sort�� ���� �߰��� �� ������, List.Sort�� �⺻ ����
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Option_ResolutionUI : MonoBehaviour
{
    // PlayerPrefs Ű�� ����� �����Ͽ� ��Ÿ ����
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
        // GetComponentInChildren �Ǵ� �ٸ� ������ ������� ã�� ���� ����غ� �� �ֽ��ϴ�.
        _cutoutFade = FindObjectOfType<CutoutFade>(); 
    }

    void Start()
    {
        InitializeFullscreen();
        InitializeResolutions();
    }
    
    // ��ü ȭ�� ���� �ʱ�ȭ�� ����ϴ� �޼���
    private void InitializeFullscreen()
    {
        bool isFullScreen = (PlayerPrefs.GetInt(FullScreenPrefKey, 1) == 1);
        fullscreenToggle.isOn = isFullScreen;
        Screen.fullScreen = isFullScreen;
    }

    // �ػ� ���� �ʱ�ȭ�� ����ϴ� �޼���
    private void InitializeResolutions()
    {
        // 1. �ߺ� ���� �� �ְ� �ֻ��� ���͸�
        Dictionary<string, Resolution> uniqueResolutions = new Dictionary<string, Resolution>();
        foreach (Resolution res in Screen.resolutions)
        {
            string key = res.width + "x" + res.height;
            if (!uniqueResolutions.ContainsKey(key) || res.refreshRateRatio.value > uniqueResolutions[key].refreshRateRatio.value)
            {
                uniqueResolutions[key] = res;
            }
        }
        
        // 2. ����Ʈ�� ��ȯ �� ����
        List<Resolution> filteredResolutions = new List<Resolution>(uniqueResolutions.Values);
        filteredResolutions.Sort((a, b) => {
            if (a.width != b.width) return a.width.CompareTo(b.width);
            return a.height.CompareTo(b.height);
        });
        resolutions = filteredResolutions.ToArray();

        // 3. ��Ӵٿ� ä���
        resolutionDropdown.ClearOptions();
        int currentResolutionIndex = 0;
        List<string> options = new List<string>();

        for (int i = 0; i < resolutions.Length; i++)
        {
            // [������] .refreshRateRatio.value�� ����ϰ� ������ �ݿø�
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