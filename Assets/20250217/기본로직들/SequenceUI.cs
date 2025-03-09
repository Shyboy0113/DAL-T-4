using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SequenceUI : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField]
    private TMP_Text[] _tmp_Text = new TMP_Text[3];


    [SerializeField]
    private ResetStackManager _resetStackManager;
    void Start()
    {
        _resetStackManager = FindObjectOfType<ResetStackManager>();
    }
    private void Update()
    {
        if(_resetStackManager is not null)
        {
            int num = _resetStackManager.CheckInputQueue(0);
            _tmp_Text[0].text = InputText(num);

            num = _resetStackManager.CheckInputQueue(1);
            _tmp_Text[1].text = InputText(num);

            num = _resetStackManager.CheckInputQueue(2);
            _tmp_Text[2].text = InputText(num);
        }
    }
    private string InputText(int key)
    {
        switch (key)
        {
            case 1: return "ALT";
            case 2: return "F4";
            case 3: return "TAB";
            default: return "";
        }
    }

}
