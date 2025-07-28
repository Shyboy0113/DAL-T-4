using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ImageOnMouseEvent : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Image _image;
    private RectTransform _imageRect;
    private Vector2 _originalSize;
    private Color _originalColor;

    private void Start()
    {
        _image = GetComponent<Image>();
        _imageRect = _image.GetComponent<RectTransform>();
        _originalSize = _imageRect.sizeDelta;  // 원래 크기 저장
        _originalColor = _image.color;  // 원래 색상 저장
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _imageRect.sizeDelta = _originalSize * 1.1f;  // 이미지 크기만 1.1배 증가
        _image.color = Color.yellow;  // 색상을 노란색으로 변경
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _imageRect.sizeDelta = _originalSize;  // 원래 크기로 복귀
        _image.color = _originalColor;  // 원래 색상으로 복귀
    }
}
