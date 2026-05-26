using System;
using UnityEngine;

public class DevelopmentManager : Singleton<DevelopmentManager>
{
    [SerializeField] private DevelopmentPanel panel;
    
    private void Update()
    {
//#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // 개발자 모드 코드 실행
        if (Input.GetKeyDown(KeyCode.F12))
        {
            panel.gameObject.SetActive(!panel.gameObject.activeSelf);
        }
//#endif
        
    }
}
