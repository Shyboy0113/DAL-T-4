using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class UISoundEffectPlayer : MonoBehaviour
{
    private AudioSource _audioSource;
 
    [SerializeField]
    private AudioClip soundEffectClip;
    
    void Awake()
    {
        // 이 스크립트가 붙은 오브젝트에 AudioSource가 없으면 추가해줍니다.
        _audioSource = GetComponent<AudioSource>();
        
        // 소리가 3D 공간감을 갖지 않도록 2D로 설정합니다.
        _audioSource.spatialBlend = 0f;
        // 씬이 시작될 때 자동 재생되지 않도록 합니다.
        _audioSource.playOnAwake = false;
    }

    public void PlayButtonClickSoundEffect()
    {
        // 효과음이 설정되어 있을 경우에만 재생
        if (soundEffectClip is not null)
        {
            // PlayOneShot은 여러번 중첩해서 소리를 재생할 수 있어 버튼 클릭에 적합합니다.
            _audioSource.PlayOneShot(soundEffectClip);
        }
    }
    
    
    
}