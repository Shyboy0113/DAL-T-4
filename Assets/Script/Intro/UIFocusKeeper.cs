using UnityEngine;
using UnityEngine.EventSystems;

// 이 스크립트는 마우스 클릭 등으로 EventSystem의 포커스가 사라졌을 때,
// 마지막으로 선택했던 UI 요소로 포커스를 되돌려주는 역할을 합니다.
public class UIFocusKeeper : MonoBehaviour
{
    // 마지막으로 선택했던 게임 오브젝트를 기억할 변수
    private GameObject _gameObject;

    void Update()
    {
        // 현재 EventSystem이 아무것도 선택하고 있지 않다면 (포커스를 잃었다면)
        if (EventSystem.current.currentSelectedGameObject is null)
        {
            // 하지만 우리가 마지막으로 선택했던 오브젝트를 기억하고 있다면
            if (_gameObject is not null)
            {
                // 그 오브젝트로 포커스를 되돌린다!
                EventSystem.current.SetSelectedGameObject(_gameObject);
            }
        }
        else
        {
            // EventSystem이 무언가를 선택하고 있다면, 그 오브젝트를 계속 기억해 둔다.
            _gameObject = EventSystem.current.currentSelectedGameObject;
        }
    }
}