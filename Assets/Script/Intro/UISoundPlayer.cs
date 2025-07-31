using UnityEngine;

public class UISoundPlayer : MonoBehaviour
{
    private AudioSource audioSource;

    void Awake()
    {
        // 이 스크립트가 붙은 오브젝트에 AudioSource가 없으면 추가해줍니다.
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        // 소리가 3D 공간감을 갖지 않도록 2D로 설정합니다.
        audioSource.spatialBlend = 0f;
        // 씬이 시작될 때 자동 재생되지 않도록 합니다.
        audioSource.playOnAwake = false;
    }

    // 버튼에서 호출할 함수
    public void PlaySound(AudioClip clip)
    {
        if (clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}