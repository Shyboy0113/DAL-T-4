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
    private StackManager _stackManager;
    void Start()
    {
        _stackManager = FindObjectOfType<StackManager>();
    }
    private void Update()
    {
        if(_stackManager is not null)
        {
            int key = _stackManager.CheckInputQueue(0);
            _tmp_Text[0].text = InputText(0, key);

            key = _stackManager.CheckInputQueue(1);
            _tmp_Text[1].text = InputText(1, key);

            key = _stackManager.CheckInputQueue(2);
            _tmp_Text[2].text = InputText(2, key);
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

}
