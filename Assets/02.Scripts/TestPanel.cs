using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TestPanel : MonoBehaviour
{
    public TMP_Text ALT;
    public TMP_Text F4;
    public TMP_Text TAB;
    public TMP_Text clearTime; 

    private void Update()
    {
        ALT.text = GameManager.Instance.pushedNumberALT.ToString();
        F4.text = GameManager.Instance.pushedNumberF4.ToString();
        TAB.text = GameManager.Instance.pushedNumberTAB.ToString();
        clearTime.text = GameManager.Instance.currentTime.ToString();
    }

}
