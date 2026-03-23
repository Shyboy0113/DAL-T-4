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
    private float _bgmVolume = 1f;
    private float _sfxVolume = 1f;
    
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
        // UI 슬라이더의 값을 불러온 값으로 설정
        masterScrollbar.value = _masterVolume;
        bgmScrollbar.value = _bgmVolume;
        sfxScrollbar.value = _sfxVolume;
        
        // 오디오 믹서의 값도 불러온 값으로 설정
        // (슬라이더 값이 바뀌어야만 Set 함수가 호출되므로, 시작할 때도 한번 호출해줘야 합니다)
        SetMasterVolume(_masterVolume);
        SetBGMVolume(_bgmVolume);
        SetSFXVolume(_sfxVolume);
    }

    private void Update()
    {
        masterData.text = masterScrollbar.value.ToString("F2") + "f";
        bgmData.text = bgmScrollbar.value.ToString("F2") + "f";
        sfxData.text = sfxScrollbar.value.ToString("F2") + "f";
    }

    public void SaveTotalVolume()
    {
        PlayerPrefs.SetFloat("MasterVolume", _masterVolume);
        PlayerPrefs.SetFloat("BGMVolume", _bgmVolume);
        PlayerPrefs.SetFloat("SFXVolume", _sfxVolume);
        PlayerPrefs.Save();
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
        SaveTotalVolume();
    }
}
