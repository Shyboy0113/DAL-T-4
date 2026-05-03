using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ImageOnMouseEvent : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    ISelectHandler, IDeselectHandler
{
    [SerializeField] private float hoverScale = 1.1f;

    [SerializeField] private Image image;
    
    private Vector3 _originalScale;
    
    private Color _originalColor;
    [SerializeField] private Color hoverColor = Color.yellow;
    
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
        
        if (image is not null) _originalColor = image.color; 
        
    }

    private bool IsInteractable => _button == null || _button.interactable;

    // 시각적 업데이트를 담당하는 단일 메서드
    private void UpdateVisuals()
    {
        if (!IsInteractable) return;

        if (_isHovered || _isSelected)
        {
            _rectTransform.localScale = _originalScale * hoverScale;
            
            if (image is not null) image.color = hoverColor;
        }
        else
        {
            _rectTransform.localScale = _originalScale;
            
            if (image is not null) image.color = _originalColor;
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