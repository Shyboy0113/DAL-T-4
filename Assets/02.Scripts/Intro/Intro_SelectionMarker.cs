using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems; // EventSystem 인터페이스 사용을 위해 필수!

// ISelectHandler: 선택되었을 때 호출될 함수를 정의
// IDeselectHandler: 선택 해제되었을 때 호출될 함수를 정의
public class Intro_SelectionMarker : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    // 인스펙터에서 켜고 끌 화살표(마커) 오브젝트를 연결
    public GameObject selectionMarkers;

    private void OnEnable()
    {
        // 오브젝트가 활성화될 때 마커를 꺼진 상태로 초기화
        if (selectionMarkers != null)
        {
            selectionMarkers.SetActive(false);
        }

        StartCoroutine(I_CheckInitialSelection());
    }

    // 이 오브젝트가 선택되었을 때 EventSystem이 자동으로 호출하는 함수
    public void OnSelect(BaseEventData eventData)
    {
        if (selectionMarkers != null)
        {
            selectionMarkers.SetActive(true); // 마커를 켠다
        }
    }

    // 이 오브젝트가 선택 해제되었을 때 EventSystem이 자동으로 호출하는 함수
    public void OnDeselect(BaseEventData eventData)
    {
        if (selectionMarkers != null)
        {
            selectionMarkers.SetActive(false); // 마커를 끈다
        }
    }

    private IEnumerator I_CheckInitialSelection()
    {
        yield return new WaitForEndOfFrame();
        
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject)
        {
            // 내가 선택된 상태라면 마커를 강제로 켬
            if (selectionMarkers != null) selectionMarkers.SetActive(true);
        }
    }
}