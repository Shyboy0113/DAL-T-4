using System;
using UnityEngine;

public class GameClearEffect : MonoBehaviour
{
    [SerializeField] private ParticleSystem confettiParticle;
    [SerializeField] private int burstCount = 150;

    private void Start()
    {
        PlayCelebration();
    }

    public void PlayCelebration()
    {
        // 위치를 화면 중앙 상단에 맞추고 싶다면
        // (UI Canvas 기준이면 월드 좌표로 변환 필요)
        confettiParticle.transform.position = GetScreenTopCenter();
        
        var emission = confettiParticle.emission;
        emission.SetBurst(0, new ParticleSystem.Burst(0f, burstCount));
        
        confettiParticle.Play();
    }

    private Vector3 GetScreenTopCenter()
    {
        // 카메라 기준 화면 상단 중앙
        return Camera.main.ScreenToWorldPoint(
            new Vector3(Screen.width / 2f, Screen.height, 10f)
        );
    }
}