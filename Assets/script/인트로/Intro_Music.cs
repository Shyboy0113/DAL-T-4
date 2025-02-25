using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Intro_Music : MonoBehaviour
{
    GameObject BackGroundMusic;
    public AudioSource backmusic;

    bool Is_Playing = false;

    //esc에서 일시정지
    public bool Is_Pause;

    //public bool fadeIn;
    // fade in 시간 설정 1s
    public double fadeInSeconds = 5;
    double fadeDeltaTime = 0;
    bool isFadeIn = true;
    public float volume_Size = 0.2f;
    void Awake()
    {
        Is_Pause = false;
        BackGroundMusic = GameObject.Find("BackGroundMusic");
        backmusic = BackGroundMusic.GetComponent<AudioSource>(); //배경음악 저장해둠
        if (backmusic.isPlaying)
        {
            Is_Playing = true;
            return; //배경음악이 재생되고 있다면 패스
        }
        else
        {
            Is_Playing = false;
            backmusic.Play();
            DontDestroyOnLoad(BackGroundMusic); //배경음악 계속 재생하게(이후 버튼매니저에서 조작)
        }
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (Is_Pause == false)
            {
                backmusic.Pause();
                Is_Pause = true;
            }
            else
            {
                backmusic.Play();
                Is_Pause = false;
            }
        }



        if (Is_Playing == false)
        {
            if (isFadeIn)
            {
                fadeDeltaTime += Time.deltaTime;
                if (fadeDeltaTime >= fadeInSeconds)
                {
                    fadeDeltaTime = fadeInSeconds;
                    isFadeIn = false;
                }
                backmusic.volume = (float)(fadeDeltaTime / fadeInSeconds) * volume_Size;

            }
        }
    }

}
