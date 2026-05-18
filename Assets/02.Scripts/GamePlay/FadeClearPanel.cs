using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

[RequireComponent(typeof(CanvasGroup))] // CanvasGroup이 반드시 필요함
public class FadeClearPanel : MonoBehaviour, IStageClearEffect
{
    [SerializeField] private float fadeTime = 1.0f;
    
    [SerializeField] private GameObject clearPanelTarget;
    [SerializeField] private GameObject originTarget;
    
    private void Awake()
    {
        gameObject.transform.localScale = Vector3.zero;
    }

    private void OnEnable()
    {
        GameEvents.StageRestarted += OnStageStarted;
    }

    private void OnDisable()
    {
        GameEvents.StageRestarted -= OnStageStarted;
    }

    // 인터페이스 구현: 연출 실행
    public IEnumerator Execute()
    {
        originTarget = EventSystem.current.currentSelectedGameObject;
        
        yield return new WaitForSeconds(fadeTime);
        
        // 현재 이벤트시스템의 타겟을 clearPanelTarget으로 지정해주기
        EventSystem.current.SetSelectedGameObject(clearPanelTarget);
        
        // 알파를 1로 페이드 시키고 애니메이션이 끝날 때까지 대기
        // .SetUpdate(true)는 게임이 정지(TimeScale = 0)된 상태에서도 작동하게 함
        yield return gameObject.transform.DOScale(1f, fadeTime).SetEase(Ease.InOutElastic);
    }

    // 인터페이스 구현: 초기화
    public void ResetEffect()
    {
        transform.localScale = Vector3.zero;
        
        // 현재 이벤트시스템의 타겟을 기존의 타겟으로 지정해주기
        EventSystem.current.SetSelectedGameObject(originTarget);
    }

    public void OnStageStarted()
    {
        originTarget = null;
    }
}