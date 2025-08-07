using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class TextFade : MonoBehaviour
{
    [SerializeField] private TMP_Text text;

    private void OnEnable()
    {
        StackManager.OnStageCleared += FadeIn;
    }

    private void OnDestroy()
    {
        StackManager.OnStageCleared -= FadeIn;
    }

    private void Awake()
    {
        text = GetComponent<TextMeshProUGUI>(); //컴포넌트 가져오기
        
    }

    public void FadeIn()
    {
        text.DOFade(1f, 1f);
        text.rectTransform.DOScale(1f, 1f).SetEase(Ease.OutElastic);
    }
    
}
