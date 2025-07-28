using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;


public class StageSelect : MonoBehaviour
{
    [SerializeField]
    private TMP_Text stageText;

    private void Awake()
    {
        stageText = transform.GetChild(0).GetComponent<TMP_Text>();
    }

    public void GotoStage()
    {
        if (stageText)
        {
            SceneManager.LoadScene(stageText.text);
        }
    }
}
