using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using TMPro;

public class Option_Language : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown languageDropdown;

    private bool _isChanging = false;
    private bool _initialized = false;
    
    private Locale _originalLocale; // 기존 로케일(언어)

    private void OnEnable()
    {
        if (!_initialized)
            StartCoroutine(InitializeDropdown()); // 초기화 안 됐으면 여기서 시작
        else
            CaptureOriginalLocale(); // 이미 초기화됐으면 원본만 캡처
    }

    /// <summary>옵션 패널이 열릴 때 현재 언어를 저장합니다. 패널이 항상 활성 상태인 씬에서 명시적으로 호출하세요.</summary>
    public void CaptureOriginalLocale()
    {
        _originalLocale = LocalizationSettings.SelectedLocale;
    }

    private IEnumerator InitializeDropdown()
    {
        yield return LocalizationSettings.InitializationOperation;

        languageDropdown.ClearOptions();
        var locales = LocalizationSettings.AvailableLocales.Locales;
        var options = new List<string>();
        foreach (var locale in locales)
            options.Add(locale.LocaleName);
        languageDropdown.AddOptions(options);

        int currentIndex = locales.IndexOf(LocalizationSettings.SelectedLocale);
        if (currentIndex != -1)
            languageDropdown.SetValueWithoutNotify(currentIndex);

        _initialized = true;
        CaptureOriginalLocale();
    }

    public void OnLanguageChanged(int index)
    {
        if (_isChanging) return;
        StartCoroutine(ChangeLocale(index));
    }

    private IEnumerator ChangeLocale(int index)
    {
        _isChanging = true;
        yield return LocalizationSettings.InitializationOperation;
        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[index];
        _isChanging = false;
    }
    
    public void Apply()
    {
        _originalLocale = LocalizationSettings.SelectedLocale;
    }

    public void CancelChange()
    {
        if (_originalLocale != null && LocalizationSettings.SelectedLocale != _originalLocale)
        {
            LocalizationSettings.SelectedLocale = _originalLocale;

            int index = LocalizationSettings.AvailableLocales.Locales.IndexOf(_originalLocale);
            if (index != -1)
            {
                languageDropdown.SetValueWithoutNotify(index);
            }
        }
    }
}
