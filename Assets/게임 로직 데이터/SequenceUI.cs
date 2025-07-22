using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SequenceUI : MonoBehaviour
{
    
    [SerializeField]
    private TMP_Text[] _tmp_Text = new TMP_Text[3];

    [SerializeField]
    private StackManager _stackManager;
    
    void Awake()
    {
        _stackManager = FindObjectOfType<StackManager>();
        
        if (_stackManager != null)
        {
            // ◀️ StackManager의 이벤트에 UpdateUI 메서드를 구독
            _stackManager.OnInputQueueChanged += UpdateUI;
            UpdateUI(); // ◀️ 시작할 때 한 번 초기화
        }
        
    }
    
    private void UpdateUI()
    {
        if (_stackManager is not null)
        {
            for (int i = 0; i < 3; i++)
            {
                int key = _stackManager.CheckInputQueue(i);
                _tmp_Text[i].text = InputText(i, key);
            }
        }
    }
    private string InputText(int index, int key)
    {
        switch (key)
        {
            case 1:
                _tmp_Text[index].color = Color.red;
                return "ALT";
            case 2:
                _tmp_Text[index].color = Color.black;
                return "F4";
            case 3:
                _tmp_Text[index].color = Color.blue;
                return "TAB";
            default:
                _tmp_Text[index].color = Color.black;
                return "";
        }
    }
    
    private void OnDestroy()
    {
        if (_stackManager != null)
        {
            _stackManager.OnInputQueueChanged -= UpdateUI;
        }
    }

}
