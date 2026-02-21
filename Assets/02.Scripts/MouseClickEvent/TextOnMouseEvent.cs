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
        _originalScale = transform.localScale;  // ���� ������ ����
        _originalColor = _text.color;  // ���� ���� ����
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.localScale = _originalScale * 1.1f;  // ��ü ũ�� 1.1�� ����
        _text.color = Color.yellow;  // ������ ��������� ����
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.localScale = _originalScale;  // ���� ũ��� ����
        _text.color = _originalColor;  // ���� �������� ����
    }
}
