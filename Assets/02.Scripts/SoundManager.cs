using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set;}
    
    [SerializeField]
    private AudioSource audioSource;

    public List<AudioClip> bgmList;

    [SerializeField] private string currentStage;
    [SerializeField] private AudioClip currentAudioClip;
    
    private void Awake()
    {
        // 싱글톤 구현
        if (Instance == null)
        {
            Instance = this;
            
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        currentAudioClip = bgmList[0];
        
        audioSource.clip = bgmList[0]; //맨 첫번째 BGM을 기준으로 설정
        audioSource.Play();
    }

    public void RenewalBGM(AudioClip audioClip, string stageName)
    {
        if (audioClip == null) return;

        if (audioSource.clip != null && audioSource.clip == audioClip)
        {
            currentStage = stageName; //stageName 갱신
            return;
        }
        
        currentStage = stageName;
        audioSource.clip = currentAudioClip = audioClip;
        
        // 각 스테이지에 배치한 BGM을 SoundManager에 적용
        // DOTween을 이용한 볼륨 페이드 아웃 -> 교체 -> 페이드 인
        Sequence s = DOTween.Sequence();
        s.Append(audioSource.DOFade(0, 0.5f).OnComplete(() => {
            audioSource.clip = audioClip;
            audioSource.Play();
        }));
        s.Append(audioSource.DOFade(1.0f, 0.5f)); // 새로운 브금으로 갱신

    }
}
