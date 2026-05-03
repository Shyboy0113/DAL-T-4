using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ImageOnMouseEvent : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    ISelectHandler, IDeselectHandler
{
    [SerializeField] private float hoverScale = 1.1f;

    private Vector3 _originalScale;
    
    private RectTransform _rectTransform;
    private Button _button;

    // 상태를 추적할 변수 추가
    private bool _isHovered = false;
    private bool _isSelected = false;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _button = GetComponent<Button>();
        
        _originalScale = _rectTransform.localScale;
        
    }

    private bool IsInteractable => _button == null || _button.interactable;

    // 시각적 업데이트를 담당하는 단일 메서드
    private void UpdateVisuals()
    {
        if (!IsInteractable) return;

        if (_isHovered || _isSelected)
        {
            _rectTransform.localScale = _originalScale * hoverScale;
        }
        else
        {
            _rectTransform.localScale = _originalScale;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovered = true;
        UpdateVisuals();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovered = false;
        UpdateVisuals();
    }

    public void OnSelect(BaseEventData eventData)
    {
        _isSelected = true;
        UpdateVisuals();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        _isSelected = false;
        UpdateVisuals();
    }

    private void OnDisable()
    {
        _isHovered = false;
        _isSelected = false;
        if (_rectTransform != null)
            _rectTransform.localScale = _originalScale;
    }
}