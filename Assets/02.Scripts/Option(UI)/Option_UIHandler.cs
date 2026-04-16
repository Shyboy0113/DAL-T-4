using UnityEngine;

public class Option_UIHandler : MonoBehaviour 
{
    public SO_UIEvent optionEvent;
    public GameObject panel; // 옵션 UI 패널

    public Option_Resolution resolution;
    public Option_Language language;
    public Option_SoundUI sound;
    
    private void Awake()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    private void OnEnable() 
    {
        optionEvent.OnActiveToggle.AddListener(HandleOptionToggle);
    }

    private void Start()
    {
        if (GameManager.Instance != null && panel != null)
        {
            panel.SetActive(GameManager.Instance.isOption);
        }
    }

    private void OnDisable()
    {
        optionEvent.OnActiveToggle.RemoveListener(HandleOptionToggle);;
    }

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.isCleared ||
            GameManager.Instance.isChatting) 
            return;
        
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (GameManager.Instance.isOption)
            {
                if (resolution != null) resolution.CancelChange();
                if (language != null)   language.CancelChange();
                if (sound != null)      sound.CancelChange();

                optionEvent.Raise(false);
            }
        }
        
    }
    
    // OK 버튼의 OnClick에 연결
    public void OnOKButtonClicked()
    {
        if (resolution != null) resolution.ApplyAndClose();
        if (language != null)   language.ApplyAndClose();
        if (sound != null)      sound.ApplyAndClose();

        optionEvent.Raise(false);
    }

    // Cancel 버튼의 OnClick에 연결
    public void OnCancelButtonClicked()
    {
        if (resolution != null) resolution.CancelChange();
        if (language != null)   language.CancelChange();
        if (sound != null)      sound.CancelChange();

        optionEvent.Raise(false);
    }

    private void HandleOptionToggle(bool active)
    {
        if (panel == null || GameManager.Instance == null) return;

        if (panel.activeSelf != active)
        {
            panel.SetActive(active);
            GameManager.Instance.isOption = active;

            if (active)
            {
                // 패널이 열릴 때 각 설정의 원본 값을 캡처 (Cancel 시 복원용)
                if (language != null) language.CaptureOriginalLocale();
                if (sound != null)    sound.CaptureOriginalVolume();
            }
        }
    }
}
