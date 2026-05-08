using System;
using System.Collections;
using UnityEngine;
using UnityEngine.VFX;

public class TeleportVFXController : MonoBehaviour
{
    [SerializeField] private float teleportTime = 3f;
    [SerializeField] private VisualEffect teleportVFX;

    private void OnEnable()
    {
        GameEvents.TeleportTriggered += PlayTeleportCoroutine;
    }

    private void Start()
    {
        teleportVFX.Stop();
    }

    private void OnDisable()
    {
        GameEvents.TeleportTriggered -= PlayTeleportCoroutine;
    }

    public void PlayTeleportCoroutine()
    {
        //기존의 코루틴을 제거
        StopCoroutine(ITeleport());
        
        StartCoroutine(ITeleport());
    }
    
    // 테스트용 - VFX 재생
    public void PlayTeleportEffect()
    {
        teleportVFX.Play();
    }

    // 테스트용 - VFX 정지
    public void StopTeleportEffect()
    {
        teleportVFX.Stop();
    }

    private IEnumerator ITeleport()
    {
        PlayTeleportEffect();
        
        yield return new WaitForSeconds(teleportTime);
        
        StopTeleportEffect();
    }
    
}