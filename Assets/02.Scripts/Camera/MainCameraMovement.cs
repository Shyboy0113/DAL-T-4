using UnityEngine;
using DG.Tweening;

public class MainCameraMovement : MonoBehaviour
{

    // 카메라가 추적할 대상(플레이어)
    public Transform target;
    
    // 플레이어와 카메라 사이의 초기 거리(오프셋)를 저장할 변수
    private Vector3 _offset;

    public float moveDuration; // 이동에 걸리는 시간

    [SerializeField] private bool isMove = true;

    void Start()
    {
        // DOTween 초기화 (안전하게 한 번만 호출되도록 설정)
        DOTween.Init();
        
        if (target != null)
        {
            // 초기 오프셋 계산
            _offset = transform.position - target.position;
            
        }

    }

    private void OnEnable()
    {
        GameEvents.PlayerMoved += MoveToTarget;
    }

    private void OnDestroy()
    {
        GameEvents.PlayerMoved -= MoveToTarget;
    }

    void MoveToTarget()
    {
        if (target == null || !isMove) return;

        Vector3 targetDestination = target.position + _offset;
        
        // 2. 기존에 실행 중인 트윈이 있다면 중지 (충돌 방지)
        transform.DOKill();

        // 1. 목표 위치로 부드럽게 이동 (Ease.InOutCubic 사용)
        transform.DOMove(targetDestination, moveDuration)
            .SetEase(Ease.OutElastic)
            .OnComplete(() => {
                Debug.Log("카메라 이동 완료!");
            });
        
    }
    
}
