using UnityEngine;

public class Option_UIHandler : MonoBehaviour 
{
    public SO_UIEvent optionEvent;
    public GameObject panel; // 옵션 UI 패널

    public Option_Resolution resolution;
    public Option_Language language;
    
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
                if (resolution != null)
                {
                    resolution.CancelChange();
                }

                if (language != null)
                {
                    language.CancelChange();
                }

                optionEvent.Raise(false);
            }
        }
        
    }
    
    // OK 버튼의 OnClick에 연결
    public void OnOKButtonClicked()
    {
        if (resolution != null)
        {
            resolution.ApplyAndClose();
        }

        if (language != null)
        {
            language.ApplyAndClose();
        }

        optionEvent.Raise(false);
    }

    // Cancel 버튼의 OnClick에 연결
    public void OnCancelButtonClicked()
    {
        if (resolution != null)
        {
            resolution.CancelChange();
        }

        if (language != null)
        {
            language.CancelChange();
        }

        optionEvent.Raise(false);
    }

    private void HandleOptionToggle(bool active)
    {
        if (panel == null || GameManager.Instance == null) return;

        if (panel.activeSelf != active)
        {
            panel.SetActive(active);
            GameManager.Instance.isOption = active;

            // 패널이 열릴 때 현재 언어를 저장 (Option_Language가 패널 바깥에 있는 씬 대응)
            if (active && language != null)
                language.CaptureOriginalLocale();
        }
    }
}
