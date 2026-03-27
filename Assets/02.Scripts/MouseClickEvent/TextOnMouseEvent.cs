using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class TextOnMouseEvent : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    ISelectHandler, IDeselectHandler
{
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private Color hoverColor = Color.yellow;

    private RectTransform _rectTransform;
    private Button _button;
    private TMP_Text _text;
    private Color _originalColor;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _button = GetComponentInParent<Button>();
        _text = GetComponent<TMP_Text>();
        _originalColor = _text.color;
    }

    private bool IsInteractable => _button == null || _button.interactable;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsInteractable) return;
        _rectTransform.localScale = Vector3.one * hoverScale;
        _text.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (EventSystem.current.currentSelectedGameObject != gameObject)
        {
            _rectTransform.localScale = Vector3.one;
            _text.color = _originalColor;
        }
    }

    public void OnSelect(BaseEventData eventData)
    {
        _rectTransform.localScale = Vector3.one * hoverScale;
        _text.color = hoverColor;
    }

    public void OnDeselect(BaseEventData eventData)
    {
        _rectTransform.localScale = Vector3.one;
        _text.color = _originalColor;
    }

    private void OnDisable()
    {
        if (_rectTransform != null)
            _rectTransform.localScale = Vector3.one;
        if (_text != null)
            _text.color = _originalColor;
    }
}