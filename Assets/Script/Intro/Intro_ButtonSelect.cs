using UnityEngine;
using UnityEngine.SceneManagement;

public class Intro_ButtonSelect : MonoBehaviour
{
    [SerializeField] private GameObject optionPanel;
    
    [SerializeField] private CutoutFade cutoutFade;
    
    public void StartGame() // 게임 시작 버튼 클릭 시 Stage 선택창으로 넘어감
    {
        cutoutFade.FadeOut(() => 
        {
            SceneManager.LoadScene("StageSelect");
        });
        
    }

    public void ActivateOption()
    {
        optionPanel.SetActive(true);
    }
    
    public void ExitGame() // 게임 종료 버튼 클릭 시 게임 종료
    {
        Application.Quit();
    }

}
