using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ImageOnMouseEvent : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Color hoverColor = Color.white;
    [SerializeField] private float hoverScale = 1.1f;
    
    private Image _image;
    private Vector2 _originalSize;
    private Color _originalColor;

    private void Start()
    {
        _image = GetComponent<Image>();
        _originalSize = _image.rectTransform.sizeDelta;  // 원래 크기 저장
        _originalColor = _image.color;  // 원래 색상 저장
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _image.rectTransform.sizeDelta = _originalSize * hoverScale;  // 이미지 크기만 1.1배 증가
        _image.color = hoverColor;  // 색상을 지정한 색으로 변경
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _image.rectTransform.sizeDelta = _originalSize;  // 원래 크기로 복귀
        _image.color = _originalColor;  // 원래 색상으로 복귀
    }

    private void OnDisable()
    {
        _image.color = _originalColor;
    }
}
