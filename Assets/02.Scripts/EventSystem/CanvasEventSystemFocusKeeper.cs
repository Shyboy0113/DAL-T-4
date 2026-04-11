using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class CanvasEventSystemFocusKeeper : MonoBehaviour
{
    public GameObject firstSelectedObject;
    [SerializeField] private GameObject _lastSelectedObject;
    [SerializeField] private GameObject resolutionDropdown;
    [SerializeField] private GameObject languageDropdown;

    private bool _dropdownOpened = false;

    private static readonly List<CanvasEventSystemFocusKeeper> _stack = new();

    private bool IsTop => _stack.Count > 0 && _stack[^1] == this;

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
                if (resolutionDropdown!=null && _lastSelectedObject == resolutionDropdown)
                {
                    EventSystem.current.SetSelectedGameObject(resolutionDropdown);
                }
                else if (languageDropdown!=null && _lastSelectedObject == languageDropdown)
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
    private IEnumerator SetInitialFocusDeferred()
    {
        yield return null; // 모든 Awake/Start 완료 후

        if (!IsTop) yield break;
        if (firstSelectedObject != null && firstSelectedObject.activeInHierarchy && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(firstSelectedObject);
    }
}