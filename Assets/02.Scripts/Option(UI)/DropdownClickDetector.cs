using UnityEngine;
using UnityEngine.EventSystems;
using TMPro; // 추가

public class DropdownClickDetector : MonoBehaviour, IPointerClickHandler, ISubmitHandler
{
    private CanvasEventSystemFocusKeeper _focusKeeper;
    private TMP_Dropdown _dropdown;

    void Awake()
    {
        _focusKeeper = GetComponentInParent<CanvasEventSystemFocusKeeper>();
        _dropdown = GetComponent<TMP_Dropdown>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        TriggerDropdownOpened();
    }

    public void OnSubmit(BaseEventData eventData)
    {
        TriggerDropdownOpened();
    }

    private void TriggerDropdownOpened()
    {
        if (_focusKeeper != null && _dropdown != null)
        {
            // 이제 단순히 true만 만드는 게 아니라 드롭다운 정보도 같이 보냄
            _focusKeeper.OnDropdownOpened(_dropdown);
        }
    }
}