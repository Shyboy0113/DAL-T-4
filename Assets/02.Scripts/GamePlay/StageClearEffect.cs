using System;
using System.Collections;
using UnityEngine;

public class StageClearEffect : MonoBehaviour, IStageClearEffect
{
    [SerializeField] private ParticleSystem confettiParticle;
    [SerializeField] private int burstCount = 150;

    // 인터페이스 구현
    public IEnumerator Execute()
    {
        var emission = confettiParticle.emission;
        emission.SetBurst(0, new ParticleSystem.Burst(0f, burstCount));
        
        confettiParticle.Play();
        yield break;
    }

    public void ResetEffect() => confettiParticle.Stop();
}