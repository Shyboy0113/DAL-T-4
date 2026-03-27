using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ImageOnMouseEvent : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    ISelectHandler, IDeselectHandler
{
    [SerializeField] private float hoverScale = 1.1f;

    private RectTransform _rectTransform;
    private Button _button;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _button = GetComponent<Button>();
    }

    private bool IsInteractable => _button == null || _button.interactable;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsInteractable) return;
        _rectTransform.localScale = Vector3.one * hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _rectTransform.localScale = Vector3.one;
    }

    public void OnSelect(BaseEventData eventData)
    {
        _rectTransform.localScale = Vector3.one * hoverScale;
    }

    public void OnDeselect(BaseEventData eventData)
    {
        _rectTransform.localScale = Vector3.one;
    }

    private void OnDisable()
    {
        if (_rectTransform != null)
            _rectTransform.localScale = Vector3.one;
    }
}