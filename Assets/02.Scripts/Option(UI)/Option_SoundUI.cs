using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;

public class Option_SoundUI : MonoBehaviour
{
    // 음량 전체를 총괄하는 클래스
    [SerializeField] private AudioMixer audioMixer;

    // 각 슬라이더를 Inspector에서 할당
    public Scrollbar masterScrollbar;
    public Scrollbar bgmScrollbar;
    public Scrollbar sfxScrollbar;

    public TMP_Text masterData;
    public TMP_Text bgmData;
    public TMP_Text sfxData;
    
    // UI 슬라이더와 연동될 볼륨 값 (0.0f ~ 1.0f)
    private float _masterVolume = 1f;
    private float _bgmVolume    = 1f;
    private float _sfxVolume    = 1f;

    // 패널 열릴 때 캡처해두는 원본 값 (Cancel 시 복원용)
    private float _originalMasterVolume;
    private float _originalBgmVolume;
    private float _originalSfxVolume;
    
    private void OnEnable()
    {
        SyncUIWithMixer();
    }
    
    private void SyncUIWithMixer()
    {
        // 공식: Linear = 10 ^ (dB / 20)
        if (audioMixer.GetFloat("Master", out float masterDB))
        {
            _masterVolume = Mathf.Pow(10, masterDB / 20);
            masterScrollbar.value = _masterVolume;
        }

        if (audioMixer.GetFloat("BGM", out float bgmDB))
        {
            _bgmVolume = Mathf.Pow(10, bgmDB / 20);
            bgmScrollbar.value = _bgmVolume;
        }

        if (audioMixer.GetFloat("SFX", out float sfxDB))
        {
            _sfxVolume = Mathf.Pow(10, sfxDB / 20);
            sfxScrollbar.value = _sfxVolume;
        }
    }

    private void Start()
    {
        // PlayerPrefs에서 저장된 볼륨 값을 불러옴 (없으면 기본값 1f)
        _masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        _bgmVolume    = PlayerPrefs.GetFloat("BGMVolume",    1f);
        _sfxVolume    = PlayerPrefs.GetFloat("SFXVolume",    1f);

        // 오디오 믹서에 적용
        SetMasterVolume(_masterVolume);
        SetBGMVolume(_bgmVolume);
        SetSFXVolume(_sfxVolume);

        // UI 슬라이더에 반영
        masterScrollbar.value = _masterVolume;
        bgmScrollbar.value    = _bgmVolume;
        sfxScrollbar.value    = _sfxVolume;
    }

    private void Update()
    {
        masterData.text = masterScrollbar.value.ToString("F2") + "f";
        bgmData.text = bgmScrollbar.value.ToString("F2") + "f";
        sfxData.text = sfxScrollbar.value.ToString("F2") + "f";
    }

    // 패널이 열릴 때 호출 — 현재 값을 원본으로 캡처
    public void CaptureOriginalVolume()
    {
        _originalMasterVolume = _masterVolume;
        _originalBgmVolume    = _bgmVolume;
        _originalSfxVolume    = _sfxVolume;
    }

    // OK 버튼 — 현재 값을 PlayerPrefs에 저장
    public void Apply()
    {
        PlayerPrefs.SetFloat("MasterVolume", _masterVolume);
        PlayerPrefs.SetFloat("BGMVolume",    _bgmVolume);
        PlayerPrefs.SetFloat("SFXVolume",    _sfxVolume);
        PlayerPrefs.Save();
    }

    // Cancel / ESC — 캡처해둔 원본 값으로 복원
    public void CancelChange()
    {
        SetMasterVolume(_originalMasterVolume);
        SetBGMVolume(_originalBgmVolume);
        SetSFXVolume(_originalSfxVolume);

        masterScrollbar.value = _originalMasterVolume;
        bgmScrollbar.value    = _originalBgmVolume;
        sfxScrollbar.value    = _originalSfxVolume;
    }
    
    public void SetMasterVolume(float volume)
    {
        _masterVolume = volume;
        audioMixer.SetFloat("Master", Mathf.Log10(volume > 0 ? volume : 0.0001f) * 20);
    }

    public void SetBGMVolume(float volume)
    {
        _bgmVolume = volume;
        audioMixer.SetFloat("BGM", Mathf.Log10(volume > 0 ? volume : 0.0001f) * 20);
    }

    public void SetSFXVolume(float volume)
    {
        _sfxVolume = volume;
        audioMixer.SetFloat("SFX", Mathf.Log10(volume > 0 ? volume : 0.0001f) * 20);
    }
    
    private void OnApplicationQuit()
    {
        Apply();
    }
}
