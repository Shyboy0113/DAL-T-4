using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;


public class StageSelect : MonoBehaviour
{
    [SerializeField]
    private TMP_Text stageText;

    [SerializeField]
    private TransparentHoleFade fadeScript;
    
    private void Awake()
    {
        stageText = transform.GetChild(0).GetComponent<TMP_Text>();
    }

    public void GotoStage()
    {
        // FadeOut이 끝나면 SceneManager.LoadScene을 실행하라는 Action을 전달
        fadeScript.FadeOut(() => 
        {
            SceneManager.LoadScene(stageText.text);
        });
        
    }
}
