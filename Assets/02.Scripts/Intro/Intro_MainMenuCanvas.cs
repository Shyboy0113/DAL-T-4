using UnityEngine;
using UnityEngine.EventSystems;
using Eflatun.SceneReference;

public class Intro_MainMenuCanvas : MonoBehaviour
{
    [SerializeField] private CutoutFade cutoutFade;
    [SerializeField] private SceneReference stageSelectScene; //stageSelect Scene의 이름이 아닌 Scene 자체를 할당
    
    public SO_UIEvent optionEvent;
    
    private GameObject _lastSelectedGameObject;

    private void OnEnable()
    {
        optionEvent.OnActiveToggle.AddListener(SetSelectedOfMainMenu);
    }

    private void OnDisable()
    {
        optionEvent.OnActiveToggle.RemoveListener(SetSelectedOfMainMenu);
    }

    private void OnDestroy()
    {
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    private void SetSelectedOfMainMenu(bool active)
    {
        if (!active)
        {
            if (_lastSelectedGameObject != null)
            {
                EventSystem.current.SetSelectedGameObject(_lastSelectedGameObject);
            }
        }
    }

    public void StartButton() // 게임 시작 버튼 클릭 시 Stage 선택창으로 넘어감
    {
        cutoutFade.FadeOut(() => 
        {
            StartCoroutine(SceneLoader.LoadScene(stageSelectScene));
        });
        
    }
    public void OptionButton()
    {
        _lastSelectedGameObject = EventSystem.current.currentSelectedGameObject;
        optionEvent.Raise(true);
    }
    
    public void ExitButton() // 게임 종료 버튼 클릭 시 게임 종료
    {
        Application.Quit();
    }

}
