using UnityEngine;
using UnityEngine.EventSystems;

public class CanvasEventSystemFocusKeeper : MonoBehaviour
{
    [Tooltip("이 메뉴가 켜졌을 때 가장 먼저 선택될 UI 오브젝트")]
    public GameObject firstSelectedObject;

    [SerializeField]
    private GameObject _lastSelectedObject;

    // --- 1. 초기 포커스 설정 (메뉴가 켜질 때) ---
    void OnEnable()
    {
        // Option Panel이 활성화될 때, 처음 지정할 오브젝트와 기존에 EventSystem으로 조종한 적(키보드 입력)이 있는지 확인
        if (firstSelectedObject != null && EventSystem.current != null)
        {
            // MainMenu Panel에서 Option Panel로 이동 변경
            EventSystem.current.SetSelectedGameObject(firstSelectedObject);
        }
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
    
}