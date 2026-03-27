using Eflatun.SceneReference;
using UnityEngine;

public class Intro_GameStateManagement : MonoBehaviour
{
    public CutoutFade cutoutFade;

    [SerializeField] private SceneReference introScene;
    
    // Start is called before the first frame update
    void Start()
    {
        if (cutoutFade != null)
        {
            cutoutFade.FadeIn();
        }

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.RenewalBGMForSCene(introScene);
        }
        
    }
}
