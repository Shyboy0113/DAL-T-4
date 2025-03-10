using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class OnMouseEvent : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Image image;
    private RectTransform imageRect;
    private Vector2 originalSize;
    private Color originalColor;

    private void Start()
    {
        image = GetComponent<Image>();
        imageRect = image.GetComponent<RectTransform>();
        originalSize = imageRect.sizeDelta;  // 원래 크기 저장
        originalColor = image.color;  // 원래 색상 저장
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        imageRect.sizeDelta = originalSize * 1.1f;  // 이미지 크기만 1.1배 증가
        image.color = Color.yellow;  // 색상을 노란색으로 변경
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        imageRect.sizeDelta = originalSize;  // 원래 크기로 복귀
        image.color = originalColor;  // 원래 색상으로 복귀
    }
}
