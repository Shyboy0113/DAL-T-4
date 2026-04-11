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
    
    private Locale _originalLocale; // 기존 로케일(언어)

    private void OnEnable()
    {
        _originalLocale = LocalizationSettings.SelectedLocale;
    }

    void Start()
    {
        languageDropdown.ClearOptions();

        var locales = LocalizationSettings.AvailableLocales.Locales;
        var options = new List<string>();

        foreach (var locale in locales)
        {
            options.Add(locale.LocaleName);
        }

        languageDropdown.AddOptions(options);

        // 현재 선택된 로케일에 맞춰 드롭다운 동기화
        int currentIndex = locales.IndexOf(LocalizationSettings.SelectedLocale);
        if (currentIndex != -1)
        {
            languageDropdown.SetValueWithoutNotify(currentIndex);
        }
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
    
    public void ApplyAndClose()
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
