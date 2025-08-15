using System;
using UnityEngine;
using DG.Tweening;

public class MainCameraMovement : MonoBehaviour
{
    public Transform target; // 카메라가 이동할 대상의 위치
    [SerializeField]
    private Vector3 additionalVector = new (5, -1, -10);
    public float moveDuration; // 이동에 걸리는 시간

    [SerializeField] private bool isMove = true;
    
    void Start()
    {
        // DOTween 초기화 (안전하게 한 번만 호출되도록 설정)
        DOTween.Init();
        
        MoveAndLookAtTarget(); //초기 카메라 위치 세팅
    }

    private void OnEnable()
    {
        StackManager.OnPlayerMoved += MoveAndLookAtTarget;
    }

    private void OnDestroy()
    {
        StackManager.OnPlayerMoved -= MoveAndLookAtTarget;
    }

    void MoveAndLookAtTarget()
    {
        if (target is null || isMove is false) return;

        // 1. 목표 위치로 부드럽게 이동 (Ease.InOutCubic 사용)
        transform.DOMove(target.position + additionalVector, moveDuration)
            .SetEase(Ease.OutElastic)
            .OnComplete(() => {
                Debug.Log("카메라 이동 완료!");
            });
        
    }
    
    
}
