using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//SoundManagement : 실제 게임 스테이지로 진입했을 경우, BGM을 변경

public class SoundManagement : MonoBehaviour
{
    [SerializeField] private string stageName;
    
    [SerializeField] private AudioClip audioClip; //각 스테이지별로 할당되는 오디오클립(mp3)
    
    void Start()
    {
        //만약 SoundManager가 있을 경우, BGM 갱신
        if(SoundManager.Instance is not null) SoundManager.Instance.RenewalBGM(audioClip, stageName);
    }
}
