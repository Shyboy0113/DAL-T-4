using UnityEngine;
using UnityEngine.EventSystems;

public class OptionPanelManager : MonoBehaviour
{
    // 돌아갈 메인 메뉴 패널
    public GameObject mainMenuPanel;
    // 돌아갔을 때 다시 선택될 메인 메뉴의 'Option' 버튼
    public GameObject returnFocusTo;
    
    public void ClosePanel()
    {
        // 1. 메인 메뉴를 다시 켭니다.
        mainMenuPanel.SetActive(true);

        // 2. 키보드 포커스를 지정된 'Option' 버튼으로 되돌립니다.
        EventSystem.current.SetSelectedGameObject(returnFocusTo);

        // 3. 자기 자신(옵션 패널)을 끕니다.
        gameObject.SetActive(false);
    }
}