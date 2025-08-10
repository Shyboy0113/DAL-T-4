using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StageSelectUI : MonoBehaviour
{
    [SerializeField]
    private CutoutFade cutoutFade;
    
    public void ReturnToMenu()
    {
        // FadeOut이 끝나면 메인 메뉴로 되돌아갑니다.
        cutoutFade.FadeOut(() => 
        {
            SceneManager.LoadScene(0);
        });
    } 
    
}
