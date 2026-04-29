using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class CanvasEventSystemFocusKeeper : MonoBehaviour
{
    public GameObject firstSelectedObject;
    [SerializeField] private GameObject _lastSelectedObject;
    [SerializeField] private GameObject resolutionDropdown;
    [SerializeField] private GameObject languageDropdown;

    private bool _dropdownOpened = false;

    private static readonly List<CanvasEventSystemFocusKeeper> _stack = new();

    private bool IsTop => _stack.Count > 0 && _stack[^1] == this;

    private GameObject _previousSelectedForScroll;
    
    void OnEnable()
    {
        _stack.Remove(this);
        _stack.Add(this);

        StartCoroutine(SetInitialFocusDeferred());
        
        if (firstSelectedObject != null && firstSelectedObject.activeInHierarchy && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(firstSelectedObject);
    }

    void OnDisable()
    {
        _stack.Remove(this);

        if (_stack.Count > 0)
        {
            var prev = _stack[^1];
            if (prev._lastSelectedObject != null)
                EventSystem.current.SetSelectedGameObject(prev._lastSelectedObject);
        }
    }

    void Update()
    {
        if (!IsTop) return;

        GameObject currentSelected = EventSystem.current.currentSelectedGameObject;

        if (currentSelected == null)
        {
            if (_dropdownOpened)
            {
                // 마지막 선택이 어떤 드롭다운이었는지 판별
                if (resolutionDropdown != null && _lastSelectedObject == resolutionDropdown)
                {
                    EventSystem.current.SetSelectedGameObject(resolutionDropdown);
                }
                else if (languageDropdown != null && _lastSelectedObject == languageDropdown)
                {
                    EventSystem.current.SetSelectedGameObject(languageDropdown);
                }
                _dropdownOpened = false;
            }
            else if (_lastSelectedObject != null)
            {
                EventSystem.current.SetSelectedGameObject(_lastSelectedObject);
            }
        }
        else
        {
            if (currentSelected.transform.IsChildOf(transform))
                _lastSelectedObject = currentSelected;
            
            if (currentSelected != _previousSelectedForScroll)
            {
                _previousSelectedForScroll = currentSelected;
                
                // [수정됨] 마우스 호버로 인한 포커스 변경은 무시하고, 키보드/패드 입력일 때만 스크롤
                if (IsKeyboardOrGamepadInput())
                {
                    ScrollToSelectedRatio(currentSelected);
                }
            }
        }
    }

    // [추가됨] 키보드나 패드로 조작 중인지 판별하는 함수
    private bool IsKeyboardOrGamepadInput()
    {
        // 유니티 기본 Input Manager 기준 (W, S, Up, Down, 스틱 등)
        return Input.GetAxisRaw("Vertical") != 0 || 
               Input.GetAxisRaw("Horizontal") != 0 || 
               Input.GetKeyDown(KeyCode.Tab);
    }

    // [수정됨] 제안해주신 '인덱스 비율(Ratio)' 기반의 스크롤 함수
    private void ScrollToSelectedRatio(GameObject selectedObj)
    {
        ScrollRect scrollRect = selectedObj.GetComponentInParent<ScrollRect>();
        if (scrollRect == null) return;

        RectTransform content = scrollRect.content;
        
        // 아이템이 1개 이하면 스크롤할 필요 없음
        if (content == null || content.childCount <= 1) return;

        // 1. 현재 선택된 아이템의 순서(인덱스)를 가져옴 (0부터 시작)
        int currentIndex = selectedObj.transform.GetSiblingIndex();
        
        // 2. 전체 아이템 개수
        int totalItems = content.childCount;

        // 3. 0.0 ~ 1.0 사이의 비율 계산 (ex: 6개 중 0번 = 0/5 = 0)
        float ratio = (float)currentIndex / (totalItems - 1);

        // 4. 스크롤 적용 
        // verticalNormalizedPosition은 1이 맨 위, 0이 맨 아래이므로 1에서 빼줌
        scrollRect.verticalNormalizedPosition = 1f - ratio;
    }
    
    public void RestoreLastSelected()
    {
        if (_lastSelectedObject != null)
            EventSystem.current.SetSelectedGameObject(_lastSelectedObject);
    }

    public void OnDropdownOpened(TMP_Dropdown dropdown)
    {
        _dropdownOpened = true;
        
        StartCoroutine(InitialScrollRoutine(dropdown));
    }
    
    private IEnumerator InitialScrollRoutine(TMP_Dropdown dropdown)
    {
        // TMP_Dropdown이 "Dropdown List"를 생성할 때까지 한 프레임 대기
        yield return null;

        // 생성된 리스트 내부의 ScrollRect를 찾음
        // 보통 드롭다운이 열리면 씬에 "Dropdown List"라는 이름의 객체가 생김
        ScrollRect scrollRect = dropdown.gameObject.GetComponentInChildren<ScrollRect>(true);
        
        // 만약 GetComponentInChildren으로 안 찾아지면 (드롭다운이 다른 캔버스에 생성될 경우)
        if (scrollRect == null)
        {
            GameObject listObj = GameObject.Find("Resolution Dropdown");
            if (listObj != null) scrollRect = listObj.GetComponent<ScrollRect>();
        }

        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();

            int currentIndex = dropdown.value; // 현재 선택된 인덱스 (예: 1920x1080이 5번이면 5)
            int totalItems = dropdown.options.Count;

            if (totalItems > 1)
            {
                float ratio = (float)currentIndex / (totalItems - 1);
                scrollRect.verticalNormalizedPosition = 1f - ratio;
            }
            
            // 처음 열었을 때 포커스된 항목을 '이전 선택'으로 등록해 중복 실행 방지
            _previousSelectedForScroll = EventSystem.current.currentSelectedGameObject;
        }
    }
    
    private IEnumerator SetInitialFocusDeferred()
    {
        yield return null; // 모든 Awake/Start 완료 후

        if (!IsTop) yield break;
        if (firstSelectedObject != null && firstSelectedObject.activeInHierarchy && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(firstSelectedObject);
    }
}