using UnityEngine;
using UnityEngine.SceneManagement;

public class Button_Select : MonoBehaviour
{
    public string stageName;
    public void SceneChange()
    {
        SceneManager.LoadScene(stageName);
    }
}
