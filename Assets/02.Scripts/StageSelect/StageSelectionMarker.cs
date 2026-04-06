using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // EventSystem 인터페이스 사용을 위해 필수!
using TMPro;

// ISelectHandler: 선택되었을 때 호출될 함수를 정의
// IDeselectHandler: 선택 해제되었을 때 호출될 함수를 정의
public class StageSelectionMarker : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    [SerializeField]
    private Color originalColor = Color.white;
    [SerializeField]
    private Color selectedColor = Color.yellow;
    
    // 인스펙터에서 켜고 끌 화살표(마커) 오브젝트를 연결
    public Image selectionCircle;
    public TMP_Text selectionText;

    private void OnEnable()
    {
        if(selectionCircle is not null) selectionCircle.color = originalColor;
        if(selectionText is not null) selectionText.color = originalColor;
    }

    // 이 오브젝트가 선택되었을 때 EventSystem이 자동으로 호출하는 함수
    public void OnSelect(BaseEventData eventData)
    {
        if(selectionCircle is not null) selectionCircle.color = selectedColor;
        if(selectionText is not null) selectionText.color = selectedColor;
    }

    // 이 오브젝트가 선택 해제되었을 때 EventSystem이 자동으로 호출하는 함수
    public void OnDeselect(BaseEventData eventData)
    {
        if(selectionCircle is not null) selectionCircle.color = originalColor;
        if(selectionText is not null) selectionText.color = originalColor;
    }
}