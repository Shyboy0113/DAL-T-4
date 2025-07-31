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
}
