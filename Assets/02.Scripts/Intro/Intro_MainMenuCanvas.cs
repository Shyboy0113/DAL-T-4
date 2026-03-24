using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class Intro_MainMenuCanvas : MonoBehaviour
{
    [SerializeField] private CutoutFade cutoutFade;

    [SerializeField] private SO_SceneGroup stageSelect;
        
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
            SceneGroupLoader.LoadGroup(stageSelect);
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
