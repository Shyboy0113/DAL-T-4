using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro; // TextMeshPro를 제어하기 위해 필수!

public class Intro_CanvasEventSystemFocusKeeper : MonoBehaviour
{
    [Tooltip("이 메뉴가 켜졌을 때 가장 먼저 선택될 UI 오브젝트")]
    public GameObject firstSelectedObject;

    [SerializeField]
    private GameObject _lastSelectedObject;

    // --- 1. 초기 포커스 설정 (메뉴가 켜질 때) ---
    void OnEnable()
    {
        // 메뉴가 켜지는 순간, 지정된 첫 번째 오브젝트를 선택합니다.
        // 이것으로 명확한 UI 시작점이 생깁니다.
        EventSystem.current.SetSelectedGameObject(firstSelectedObject);
    }

    // --- 2. 포커스 유지 및 복구 (매 프레임 감시) ---
    void Update()
    {
        // 현재 선택된 오브젝트를 가져옵니다.
        GameObject currentSelected = EventSystem.current.currentSelectedGameObject;

        // 만약 선택된 것이 없다면 (마우스 클릭 등으로 포커스가 사라졌다면)
        if (currentSelected == null)
        {
            // 하지만 마지막으로 선택했던 기록이 남아있다면
            if (_lastSelectedObject != null)
            {
                // 그 기록을 바탕으로 포커스를 되돌립니다.
                EventSystem.current.SetSelectedGameObject(_lastSelectedObject);
            }
        }
        else
        {
            // 포커스가 잘 유지되고 있다면, 현재 선택된 것을 '마지막 선택'으로 계속 기록만 해둡니다.
            _lastSelectedObject = currentSelected;
        }
    }
}