using UnityEngine;
using UnityEngine.EventSystems;

public class DropdownClickDetector : MonoBehaviour, IPointerClickHandler
{
    private CanvasEventSystemFocusKeeper _focusKeeper;

    void Awake()
    {
        _focusKeeper = GetComponentInParent<CanvasEventSystemFocusKeeper>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_focusKeeper != null)
            _focusKeeper.OnDropdownOpened();
    }
}