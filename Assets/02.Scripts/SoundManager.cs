using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set;}
    
    [SerializeField]
    private AudioSource audioSource;

    public List<AudioClip> bgmList;
    public List<AudioClip> vfxList;

    [SerializeField] private string currentStage = "";
    
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
        audioSource.clip = bgmList[0]; //맨 첫번째 BGM을 기준으로 설정
        audioSource.Play();
    }

    public bool CheckCurrentStage(string stageName)
    {
        //현재 스테이지와 새로 입력받는 스테이지가 같은지 비교
        return currentStage.Equals(stageName);
    }

    public void RenewalBGM(AudioClip audioClip, string stageName)
    {
        if (!CheckCurrentStage(stageName))
        {
            currentStage = stageName; //stageName 갱신
            
            audioSource.clip = audioClip; // 각 스테이지에 배치한 BGM을 SoundManager에 적용
            audioSource.Play(); // 새로운 브금으로 갱신
        }
    }
}
