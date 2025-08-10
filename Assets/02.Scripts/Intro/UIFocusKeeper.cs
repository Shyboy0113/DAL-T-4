using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro; // TextMeshPro를 제어하기 위해 필수!

public class UIFocusKeeper : MonoBehaviour
{
    // --- 색상 관련 변수들 ---
    [Header("색상 설정")]
    public Color selectedColor = Color.yellow; // 선택됐을 때의 색상
    public Color normalColor = Color.white;   // 원래 색상

    // --- 포커스 관리 변수 ---
    private GameObject _lastSelectedObject;

    // 패널이 켜질 때, 처음 선택될 버튼의 색상을 바로 적용하기 위함
    void OnEnable()
    {
        // InitialFocusSetter 스크립트가 첫 포커스를 설정할 때까지 잠시 기다립니다.
        // null을 한번 거치지 않으면 lastSelectedObject가 이전 패널의 버튼을 기억하고 있을 수 있습니다.
        _lastSelectedObject = null;
    }

    void Update()
    {
        GameObject currentSelectedObject = EventSystem.current.currentSelectedGameObject;

        // --- 포커스가 아예 사라졌을 때 처리 ---
        if (currentSelectedObject is null)
        {
            if (_lastSelectedObject is not null)
            {
                EventSystem.current.SetSelectedGameObject(_lastSelectedObject);

            }
        }
        // --- 선택된 버튼이 '바뀌는 순간' 감지 ---
        else if (currentSelectedObject != _lastSelectedObject)
        {
            // 1. 이전에 선택됐던 버튼의 색상을 원래대로 되돌립니다.
            if (_lastSelectedObject is not null)
            {
                // TextMeshProUGUI 컴포넌트를 찾아 색상을 normalColor로 변경
                TextMeshProUGUI lastText = _lastSelectedObject.GetComponentInChildren<TextMeshProUGUI>();
                if (lastText is not null) lastText.color = normalColor;

            }

            // 2. 새로 선택된 버튼의 색상을 노란색으로 강조합니다.
            TextMeshProUGUI currentText = currentSelectedObject.GetComponentInChildren<TextMeshProUGUI>();
            if (currentText is not null) currentText.color = selectedColor;
            
            // 3. 마지막 선택 오브젝트를 현재 것으로 업데이트합니다.
            _lastSelectedObject = currentSelectedObject;
            
        }
    }
}