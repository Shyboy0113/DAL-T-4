using System;
using System.Collections;
using UnityEngine;

public class Option_UIHandler : MonoBehaviour 
{
    public SO_UIEvent optionEvent;
    public GameObject panel; // 옵션 UI 패널

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
        if (GameManager.Instance == null || GameManager.Instance.isCleared) 
            return;
        
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (GameManager.Instance.isOption)
            {
                optionEvent.Raise(false);
            }
        }
        
    }

    private void HandleOptionToggle(bool active)
    {
        if (panel == null || GameManager.Instance == null) return;

        // 3. 현재 상태와 요청받은 상태가 다를 때만 실행 (중복 방지)
        if (panel.activeSelf != active)
        {
            panel.SetActive(active);
            GameManager.Instance.isOption = active;
        }
    }
}
