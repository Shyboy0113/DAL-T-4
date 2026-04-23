using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ElectricGlitchController : MonoBehaviour
{
    [SerializeField] private Material glitchMaterialAsset; // 원본 쉐이더 에셋
    [SerializeField] private RawImage glitchOverlay;

    [Header("감전 연출 설정")]
    [SerializeField] private float duration = 0.3f;
    [SerializeField] private float peakIntensity = 0.2f;

    
    private void OnEnable()
    {
        GameEvents.GlitchTriggered += ToggleGlitch;
    }
    
    private void Start()
    {
        glitchOverlay.enabled = false;
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
        glitchOverlay.enabled = true;
        
        yield return new WaitForSeconds(duration);
        
        glitchOverlay.enabled = false;
    }
}