using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;

public class StageClearManager : MonoBehaviour
{
    [Header("Immediate Actions (Group A)")]
    // 인스펙터에서 드래그 앤 드롭으로 그림자 끄기, 스팀 업적 등을 연결
    // 코드를 수정하지 않고도 의존성을 해결할 수 있습니다.
    [SerializeField] private UnityEvent onImmediateActions;

    [Header("Sequential Effects (Group B)")]
    [SerializeField] private List<MonoBehaviour> effectObjects;
    private List<IStageClearEffect> _sequentialEffects = new List<IStageClearEffect>();

    private void Awake()
    {
        _sequentialEffects = effectObjects.OfType<IStageClearEffect>().ToList();
    }

    private void OnEnable()
    {
        GameEvents.StageCleared += StartClearSequence;
    }

    private void OnDisable()
    {
        GameEvents.StageCleared -= StartClearSequence;
    }

    private void StartClearSequence()
    {
        StartCoroutine(ClearSequenceRoutine());
    }

    private IEnumerator ClearSequenceRoutine()
    {
        // 1. 즉시 실행 로직 처리 (그림자 숨기기, 스팀 업적 등)
        onImmediateActions?.Invoke();

        // 2. 순차 연출 시작 (사운드 -> 텍스트 -> 이펙트 순)
        foreach (var effect in _sequentialEffects)
        {
            yield return StartCoroutine(effect.Execute());
        }
    }
}