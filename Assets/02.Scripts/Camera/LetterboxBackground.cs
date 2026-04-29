using UnityEngine;

public class LetterboxBackground : MonoBehaviour
{
    private void Awake()
    {
        var bgCam = gameObject.AddComponent<Camera>();
        bgCam.depth           = -100;
        bgCam.cullingMask     = 0;
        bgCam.clearFlags      = CameraClearFlags.SolidColor;
        bgCam.backgroundColor = Color.black;
        bgCam.rect            = new Rect(0, 0, 1, 1);
    }
}