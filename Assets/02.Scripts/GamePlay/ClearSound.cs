using UnityEngine;

public class ClearSound : MonoBehaviour
{
    [SerializeField] private AudioClip clearSound;
    [SerializeField] private AudioClip fireCrackerSound;

    [SerializeField] private SoundEffectPlayer soundEffectPlayer;
    
    private void OnEnable()
    {
        GameEvents.StageCleared += PlayClearSound;
    }

    private void OnDisable()
    {
        GameEvents.StageCleared -= PlayClearSound;
    }

    private void PlayClearSound()
    {
        soundEffectPlayer.PlaySoundEffect(clearSound);
        soundEffectPlayer.PlaySoundEffect(fireCrackerSound);
    }
}
