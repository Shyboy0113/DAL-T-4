using System.Collections;
using UnityEngine;
using DG.Tweening;

public class Intro_Music : MonoBehaviour
{
    GameObject BackGroundMusic;
    public AudioSource backMusic;

    bool _isPlaying = false;

    public double fadeInSeconds = 5f;
    public float volumeSize = 0.2f;
    void Awake()
    {
        BackGroundMusic = GameObject.Find("BackGroundMusic");
        backMusic = BackGroundMusic.GetComponent<AudioSource>(); //배경음악 저장해둠
        if (backMusic.isPlaying)
        {
            _isPlaying = true;
            return; //배경음악이 재생되고 있다면 패스
        }
        else
        {
            _isPlaying = false;
            backMusic.Play();
            DontDestroyOnLoad(BackGroundMusic); //배경음악 계속 재생하게(이후 버튼매니저에서 조작)
        }
    }

    IEnumerator FadeInMusic()
    {
        yield return new WaitForSeconds(1.0f);
    }
    
    IEnumerator FadeOutMusic()
    {
        yield return new WaitForSeconds(1.0f);
    }
    
}
