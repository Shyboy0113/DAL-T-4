using UnityEngine;

public class Intro_ResetManagement : MonoBehaviour
{
    public CutoutFade cutoutFade;
    
    // Start is called before the first frame update
    void Start()
    {
        if (cutoutFade != null)
        {
            cutoutFade.FadeIn();
        }
    }
}
