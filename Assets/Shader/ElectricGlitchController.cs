using UnityEngine;
using UnityEngine.UI;
using DG.Tweening; // DOTween 네임스페이스 추가

public class ElectricGlitchController : MonoBehaviour
{
    [SerializeField] private Image glitchOverlay;

    [Header("감전 페이드 연출 설정")]
    [Tooltip("화면에 나타나는 시간 (보통 글리치는 확 나타나는게 좋으므로 짧게 설정)")]
    [SerializeField] private float fadeInDuration = 0.05f; 
    [Tooltip("서서히 사라지는 시간")]
    [SerializeField] private float fadeOutDuration = 0.25f;
    [Tooltip("최대 투명도 (0~1)")]
    [SerializeField] private float maxAlpha = 1f;

    private void OnEnable()
    {
        GameEvents.GlitchTriggered += ToggleGlitch;
    }
    
    private void Start()
    {
        // 시작 시 알파값을 0으로 맞추고 비활성화해둡니다.
        Color c = glitchOverlay.color;
        c.a = 0f;
        glitchOverlay.color = c;
        
        glitchOverlay.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        GameEvents.GlitchTriggered -= ToggleGlitch;
        
        // 컴포넌트가 꺼질 때 실행 중인 트윈이 있다면 강제 종료 (메모리 누수 및 버그 방지)
        glitchOverlay.DOKill();
    }

    public void ToggleGlitch()
    {
        // 1. 이미 페이드 효과가 진행 중인데 다시 호출되었다면, 기존 트윈을 즉시 취소합니다. (연타 버그 방지)
        glitchOverlay.DOKill();
        
        // 2. 이미지를 화면에 켭니다.
        glitchOverlay.gameObject.SetActive(true);

        // 3. DOTween의 Sequence를 사용하여 페이드 인 -> 페이드 아웃을 순차적으로 연결합니다.
        Sequence glitchSeq = DOTween.Sequence();

        // [페이드 인] 현재 알파값에서 maxAlpha까지 빠르게 나타납니다.
        glitchSeq.Append(glitchOverlay.DOFade(maxAlpha, fadeInDuration));

        // [페이드 아웃] maxAlpha에 도달하면 즉시 0으로 서서히 사라집니다.
        glitchSeq.Append(glitchOverlay.DOFade(0f, fadeOutDuration));

        // [종료 처리] 페이드 아웃이 완전히 끝나면 오브젝트를 꺼서 최적화합니다.
        glitchSeq.OnComplete(() => 
        {
            glitchOverlay.gameObject.SetActive(false);
        });
    }
}