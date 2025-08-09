using UnityEngine;
using UnityEngine.EventSystems;

// 이 스크립트는 UI 패널이 활성화될 때,
// 지정된 UI 요소를 자동으로 선택(포커스)해주는 역할을 합니다.
public class UIEventSystem : MonoBehaviour
{
    // 인스펙터에서 이 패널이 켜졌을 때 가장 먼저 선택될 버튼을 연결합니다.
    public GameObject firstFocusObject;

    // 이 스크립트가 붙은 게임 오브젝트가 활성화(On)될 때마다 자동으로 호출됩니다.
    private void OnEnable()
    {
        // EventSystem에 "이제부터 이 오브젝트를 선택해줘" 라고 명령합니다.
        // null을 잠시 거쳐야 포커스가 확실하게 이동하는 경우가 있어 추가합니다.
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(firstFocusObject);
    }
}