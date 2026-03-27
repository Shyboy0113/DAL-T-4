using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Eflatun.SceneReference;

[System.Serializable]
public class SceneBGM
{
    public SceneReference scene;
    public AudioClip bgm;
}

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set;}
    
    [SerializeField]
    private AudioSource audioSource;
    public List<SceneBGM> sceneBGMList;

    // 인스펙터 창에서 작업자가 현재 무슨 BGM이 틀려 있는지 직접 확인하기 위한 용도
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

    public void RenewalBGMForSCene(SceneReference sceneReference)
    {
        var entry = sceneBGMList.Find(x => x.scene.Name == sceneReference.Name);
        if (entry == null) return;
        
        RenewalBGM(entry.bgm, sceneReference.Name);
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
