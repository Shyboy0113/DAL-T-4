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
        _originalSize = _image.rectTransform.sizeDelta;  // ���� ũ�� ����
        _originalColor = _image.color;  // ���� ���� ����
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _image.rectTransform.sizeDelta = _originalSize * hoverScale;  // �̹��� ũ�⸸ 1.1�� ����
        _image.color = hoverColor;  // ������ ������ ������ ����
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _image.rectTransform.sizeDelta = _originalSize;  // ���� ũ��� ����
        _image.color = _originalColor;  // ���� �������� ����
    }

    private void OnDisable()
    {
        if (_image != null) _image.color = _originalColor;
    }
}
