using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Intro_MainMenuCanvas : MonoBehaviour
{
    [SerializeField] private GameObject optionPanel;
    [SerializeField] private CutoutFade cutoutFade;
    
    public void StartButton() // 게임 시작 버튼 클릭 시 Stage 선택창으로 넘어감
    {
        cutoutFade.FadeOut(() => 
        {
            SceneManager.LoadScene("StageSelect");
        });
        
    }

    public void OptionButton()
    {
        optionPanel.SetActive(true);
        
        StartCoroutine(DeactivateMainMenu());
    }
    
    public void ExitButton() // 게임 종료 버튼 클릭 시 게임 종료
    {
        Application.Quit();
    }

    IEnumerator DeactivateMainMenu()
    {
        yield return new WaitForSeconds(0.3f);
        
        gameObject.SetActive(false);
        
    }

}
