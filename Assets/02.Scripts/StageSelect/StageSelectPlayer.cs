using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 스테이지 셀렉트 씬에서 현재 선택된 StageNode 위로 이동하는 플레이어 캐릭터 UI.
/// StageNode.OnSelect()에서 MoveTo()를 호출하면 DOTween으로 해당 위치로 이동합니다.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class StageSelectPlayer : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveTime  = 0.25f;
    [SerializeField] private Ease  moveEase  = Ease.OutSine;

    [Header("SFX")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip   moveSound;
    [SerializeField] private AudioClip   enterSound;

    private RectTransform _rectTransform;
    
    private RectTransform RectTr => _rectTransform ??= GetComponent<RectTransform>();
    private bool          _isLocked = false;

    public bool IsLocked => _isLocked;

    public void Lock()   => _isLocked = true;
    public void Unlock() => _isLocked = false;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    /// <summary>
    /// StageNode가 선택될 때 호출합니다. 해당 노드의 위치로 플레이어 이미지를 이동시킵니다.
    /// </summary>
    public void MoveTo(RectTransform target, bool playSound = true)
    {
        if (_isLocked || target == null) return;

        if (playSound) PlaySound(moveSound);

        RectTr.DOKill();
        RectTr.DOAnchorPos(GetAnchoredPositionOf(target), moveTime).SetEase(moveEase);
    }

    /// <summary>
    /// 씬 진입 시 애니메이션 없이 즉시 위치를 맞춥니다.
    /// </summary>
    public void SnapTo(RectTransform target)
    {
        if (target == null) return;
        RectTr.DOKill();
        RectTr.anchoredPosition = GetAnchoredPositionOf(target);
    }

    // target의 월드 위치를 이 RectTransform의 부모 기준 로컬 좌표로 변환합니다.
    // 부모가 다른 경우에도 올바르게 동작합니다.
    private Vector2 GetAnchoredPositionOf(RectTransform target)
    {
        var parentRect = RectTr.parent as RectTransform;
        if (parentRect == null) return target.anchoredPosition;

        // Canvas RenderMode에 따른 카메라 (Overlay = null)
        Canvas canvas = _rectTransform.GetComponentInParent<Canvas>();
        Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, target.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPoint, cam, out Vector2 localPos);
        return localPos;
    }

    public void PlayEnterSound() => PlaySound(enterSound);

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }
}
