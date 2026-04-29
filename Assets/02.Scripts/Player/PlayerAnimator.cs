using UnityEngine;
using DG.Tweening;

/// <summary>
/// 레이어의 애니메이션, 스프라이트, 화살표 회전을 담당합니다.
/// PlayerBehaviour에서 시각 처리 책임만 추출한 클래스입니다.
/// </summary>
public class PlayerAnimator : MonoBehaviour
{
    [SerializeField] private Animator        animator;
    [SerializeField] private SpriteRenderer  spriteRenderer;
    [SerializeField] private SpriteRenderer  iconSpriteRenderer;
    [SerializeField] private GameObject      arrow;
    [SerializeField] private SoundEffectPlayer soundEffectPlayer;
    [SerializeField] private AudioClip       explosionSound;

    [Header("DOTween")]
    [SerializeField] private float   tweenDuration;
    [SerializeField] private Vector3 tweenPunch;
    [SerializeField] private int     tweenVibrato;

    // BehaviourManager가 Undo/ 중인지 알아야 RotateArrow OnComplete에서
    // ActionFinished를 발화할지 결정할 수 있어서 참조를 둡니다.
    [SerializeField] private PlayerUndoStateBridge undoState;

    public bool IsRotating { get; private set; }

    private void Awake()
    {
        if (undoState == null)
            undoState = GetComponent<PlayerUndoStateBridge>();
    }

    public void PlayIdle()
    {
        animator.Play("Idle");
        iconSpriteRenderer.enabled = true;
        arrow.SetActive(true);
    }

    public void PlayExplosion()
    {
        if (animator.GetCurrentAnimatorStateInfo(0).IsName("Explosion")) return;

        iconSpriteRenderer.enabled = false;
        arrow.SetActive(false);
        animator.Play("Explosion");
        soundEffectPlayer.PlaySoundEffect(explosionSound);
    }

    public void PlayClear()
    {
        iconSpriteRenderer.enabled = false;
        animator.Play("Clear");
        arrow.SetActive(false);
    }

    /// <param name="immediate">true면 DOTween 없이 즉시 회전</param>
    /// <param name="rotationCount">RaisePlayerRotated에 전달할 카운터 (PlayerBehaviour에서 넘겨줌)</param>
    /// <param name="playerLayer">플레이어가 속한 맵 레이어 — 같은 레이어의 토글 타일만 반응</param>
    public void RotateArrow(PlayerDirection direction, bool immediate = false, int rotationCount = 0, int playerLayer = 0)
    {
        float targetAngle = direction switch
        {
            PlayerDirection.Right => 0f,
            PlayerDirection.Down  => 270f,
            PlayerDirection.Left  => 180f,
            PlayerDirection.Up    => 90f,
            _                     => 0f
        };

        if (immediate)
        {
            arrow.transform.rotation = Quaternion.Euler(0, 0, targetAngle);
            return;
        }

        IsRotating = true;
        bool wasUndo = undoState.IsUndo;
        int capturedCount = rotationCount;
        int capturedLayer = playerLayer;

        if (!wasUndo)
        {
            GameEvents.RaisePlayerRotated(capturedCount, capturedLayer);
            GameEvents.RaisePlayerActionFinished(capturedLayer);
        }
        
        arrow.transform
            .DORotate(new Vector3(0, 0, targetAngle), tweenDuration)
            .SetEase(Ease.OutElastic)
            .OnComplete(() =>
            {
                IsRotating = false;
            });
        
        transform.DOPunchRotation(tweenPunch, tweenDuration, tweenVibrato, 0.5f);
    }
}
