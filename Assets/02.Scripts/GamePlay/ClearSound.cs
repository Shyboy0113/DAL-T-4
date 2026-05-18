using System.Collections;
using UnityEngine;

public class ClearSound : MonoBehaviour, IStageClearEffect
{
    [SerializeField] private AudioClip clearSound;
    [SerializeField] private AudioClip fireCrackerSound;
    [SerializeField] private SoundEffectPlayer soundEffectPlayer;

    public IEnumerator Execute()
    {
        soundEffectPlayer.PlaySoundEffect(clearSound);
        soundEffectPlayer.PlaySoundEffect(fireCrackerSound);
        
        // 사운드는 병렬로 재생되어야 하므로 대기 없이 바로 종료
        yield break;
    }

    public void ResetEffect() {}
    
}