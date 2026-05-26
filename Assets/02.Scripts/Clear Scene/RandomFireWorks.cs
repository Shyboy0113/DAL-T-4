using UnityEngine;
using System.Collections;

public class RandomFireworks : MonoBehaviour
{
    [SerializeField] ParticleSystem[] fireworks; // 4개 드래그

    void Start()
    {
        foreach (var fw in fireworks)
            StartCoroutine(PlayLoop(fw));
    }

    IEnumerator PlayLoop(ParticleSystem ps)
    {
        while (true)
        {
            ps.Play();
            yield return new WaitForSeconds(ps.main.duration); // 이펙트 재생 대기
            
            float randomDelay = Random.Range(1.0f, 3.0f);
            yield return new WaitForSeconds(randomDelay);     // 랜덤 간격 대기
        }
    }
}