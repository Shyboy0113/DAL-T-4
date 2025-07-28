using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class TextOnMouseEvent : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private TMP_Text _text;
    private Vector3 _originalScale;
    private Color _originalColor;

    private void Start()
    {
        _text = GetComponent<TMP_Text>();
        _originalScale = transform.localScale;  // 원래 스케일 저장
        _originalColor = _text.color;  // 원래 색상 저장
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.localScale = _originalScale * 1.1f;  // 전체 크기 1.1배 증가
        _text.color = Color.yellow;  // 색상을 노란색으로 변경
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.localScale = _originalScale;  // 원래 크기로 복귀
        _text.color = _originalColor;  // 원래 색상으로 복귀
    }
}
