using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Intro_Music : MonoBehaviour
{
    GameObject BackGroundMusic;
    public AudioSource backmusic;

    bool Is_Playing = false;

    //esc���� �Ͻ�����
    public bool Is_Pause;

    //public bool fadeIn;
    // fade in �ð� ���� 1s
    public double fadeInSeconds = 5;
    double fadeDeltaTime = 0;
    bool isFadeIn = true;
    public float volume_Size = 0.2f;
    void Awake()
    {
        Is_Pause = false;
        BackGroundMusic = GameObject.Find("BackGroundMusic");
        backmusic = BackGroundMusic.GetComponent<AudioSource>(); //������� �����ص�
        if (backmusic.isPlaying)
        {
            Is_Playing = true;
            return; //��������� ����ǰ� �ִٸ� �н�
        }
        else
        {
            Is_Playing = false;
            backmusic.Play();
            DontDestroyOnLoad(BackGroundMusic); //������� ��� ����ϰ�(���� ��ư�Ŵ������� ����)
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
