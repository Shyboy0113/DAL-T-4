using System.Collections;
using UnityEngine;

public class SoundPlayer : MonoBehaviour
{
    private AudioSource _audioSource;

    [SerializeField]
    private GameObject optionPanel; // 비활성화용
    
    void Awake()
    {
        // 이 스크립트가 붙은 오브젝트에 AudioSource가 없으면 추가해줍니다.
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }
        // 소리가 3D 공간감을 갖지 않도록 2D로 설정합니다.
        _audioSource.spatialBlend = 0f;
        // 씬이 시작될 때 자동 재생되지 않도록 합니다.
        _audioSource.playOnAwake = false;
    }

    // 버튼에서 호출할 함수
    public void PlaySound(AudioClip clip)
    {
        if (clip != null)
        {
            _audioSource.PlayOneShot(clip);
        }
    }

    public void PlaySoundAndDeactivate(AudioClip clip)
    {
        if (clip != null)
        {
            _audioSource.PlayOneShot(clip);
        }
        
        Invoke("DeactivatePanel", clip.length);
        
    }

    public void DeactivatePanel()
    {
        if(optionPanel!=null) optionPanel.SetActive(false);
    }
    
}