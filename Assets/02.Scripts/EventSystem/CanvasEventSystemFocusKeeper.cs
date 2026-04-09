using UnityEngine;
using UnityEngine.EventSystems;

public class CanvasEventSystemFocusKeeper : MonoBehaviour
{
    [Tooltip("이 메뉴가 켜졌을 때 가장 먼저 선택될 UI 오브젝트")]
    public GameObject firstSelectedObject;

    [SerializeField] private GameObject _lastSelectedObject;

    [SerializeField] private GameObject ResolutionDropdown;
    private bool _dropdownOpened = false;
    
    void OnEnable()
    {
        // 나 외의 다른 FocusKeeper를 전부 끈다
        var all = FindObjectsByType<CanvasEventSystemFocusKeeper>(FindObjectsSortMode.None);
        foreach (var other in all)
        {
            if (other != this)
                other.enabled = false;
        }

        // 비활성 오브젝트를 선택하면 오작동할 수 있으므로 activeInHierarchy 검사 추가
        if (firstSelectedObject != null && firstSelectedObject.activeInHierarchy && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(firstSelectedObject);
    }

    // --- 2. 자기가 꺼질 때 다른 FocusKeeper를 다시 켠다 ---
    void OnDisable()
    {
        var all = FindObjectsByType<CanvasEventSystemFocusKeeper>(FindObjectsSortMode.None);
        foreach (var other in all)
        {
            if (other != this)
                other.enabled = true;
        }
    }

    // --- 포커스 유지 및 복구 (매 프레임 감시) ---
    void Update()
    {
        GameObject currentSelected = EventSystem.current.currentSelectedGameObject;

        if (currentSelected == null)
        {
            // 포커스가 사라졌을 때 마지막 선택 오브젝트로 복구
            if (_dropdownOpened && ResolutionDropdown != null)
            {
                EventSystem.current.SetSelectedGameObject(ResolutionDropdown);
                _dropdownOpened = false;
            }
            else if (_lastSelectedObject != null)
            {
                EventSystem.current.SetSelectedGameObject(_lastSelectedObject);
            }
        }
        else
        {
            // 자신의 자식 오브젝트일 때만 기록
            if (currentSelected.transform.IsChildOf(transform))
                _lastSelectedObject = currentSelected;
        }
    }
    
    public void RestoreLastSelected()
    {
        if (_lastSelectedObject != null)
            EventSystem.current.SetSelectedGameObject(_lastSelectedObject);
    }
    
    public void OnDropdownOpened()
    {
        _dropdownOpened = true;
    }
    
}