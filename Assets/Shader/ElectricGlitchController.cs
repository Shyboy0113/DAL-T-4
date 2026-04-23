using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ElectricGlitchController : MonoBehaviour
{
    [SerializeField] private Image glitchOverlay;

    [Header("감전 연출 설정")]
    [SerializeField] private float duration = 0.3f;
    
    private void OnEnable()
    {
        GameEvents.GlitchTriggered += ToggleGlitch;
    }
    
    private void Start()
    {
        glitchOverlay.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        
        GameEvents.GlitchTriggered -= ToggleGlitch;
    }

    public void ToggleGlitch()
    {
        StartCoroutine(IToggleGlitch());
    }

    private IEnumerator IToggleGlitch()
    {
        glitchOverlay.gameObject.SetActive(true);
        
        yield return new WaitForSeconds(duration);
        
        glitchOverlay.gameObject.SetActive(false);
    }
}