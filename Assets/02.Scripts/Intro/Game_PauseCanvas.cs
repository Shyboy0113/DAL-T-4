using UnityEngine;
using UnityEngine.EventSystems;
using Eflatun.SceneReference;

public class Game_PauseCanvas : MonoBehaviour
{
    [SerializeField] private CutoutFade cutoutFade;
    [SerializeField] private SceneReference stageSelectScene; //stageSelect Scene의 이름이 아닌 Scene 자체를 할당
    [SerializeField] private SceneReference introScene; // Title button
    
    [SerializeField] private CanvasEventSystemFocusKeeper sequenceFocusKeeper;
    
    public SO_UIEvent optionEvent;
    
    [SerializeField] private GameObject firstSelectedButton;
    
    private GameObject _lastSelectedGameObject;

    private void OnEnable()
    {
        optionEvent.OnActiveToggle.AddListener(OnOptionToggle);
        
        // Pause Panel이 열릴 때마다 항상 Resume 버튼으로 포커스
        if (firstSelectedButton != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(firstSelectedButton);
    }

    private void OnDisable()
    {
        optionEvent.OnActiveToggle.RemoveListener(OnOptionToggle);
    }

    private void OnDestroy()
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    private void OnOptionToggle(bool active)
    {
        // Option Panel이 닫힐 때 → Option 버튼으로 포커스 복귀
        if (!active)
        {
            if (_lastSelectedGameObject != null)
                EventSystem.current.SetSelectedGameObject(_lastSelectedGameObject);
        }
    }

    public void ResumeButton()
    {
        GameManager.Instance.isPaused = false;
        gameObject.SetActive(false);
        
        // Sequence Canvas의 마지막 선택 버튼으로 포커스 복귀
        sequenceFocusKeeper.RestoreLastSelected();
    }

    public void OptionButton()
    {
        // Option Panel 열기 전 현재 포커스 저장
        _lastSelectedGameObject = EventSystem.current.currentSelectedGameObject;
        optionEvent.Raise(true);
    }

    public void StageSelectButton()
    {
        GameManager.Instance.isPaused = false;
        cutoutFade.FadeOut(() =>
        {
            StartCoroutine(SceneLoader.LoadScene(stageSelectScene));
        });
    }

    public void TitleButton()
    {
        GameManager.Instance.isPaused = false;
        cutoutFade.FadeOut(() =>
        {
            StartCoroutine(SceneLoader.LoadScene(introScene));
        });
    }

    public void QuitButton()
    {
        Application.Quit();
    }

}
