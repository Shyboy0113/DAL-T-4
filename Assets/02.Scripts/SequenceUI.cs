using UnityEngine;
using TMPro;

public class SequenceUI : MonoBehaviour
{
    
    [SerializeField]
    private TMP_Text[] _tmp_Text = new TMP_Text[3];

    [SerializeField]
    private PlayerBehaviour _playerBehaviour;
    
    private void UpdateUI()
    {
        if (_playerBehaviour is not null)
        {
            for (int i = 0; i < 3; i++)
            {
                int key = _playerBehaviour.CheckInputQueue(i);
                _tmp_Text[i].text = InputText(i, key);
            }
        }
    }
    private string InputText(int index, int key)
    {
        switch (key)
        {
            case (int)KeyType.Alt:
                _tmp_Text[index].color = Color.red;
                return "ALT";
            case (int)KeyType.F4:
                _tmp_Text[index].color = Color.black;
                return "F4";
            case (int)KeyType.Tab:
                _tmp_Text[index].color = Color.blue;
                return "TAB";
            default:
                _tmp_Text[index].color = Color.black;
                return "";
        }
    }

    private void OnEnable()
    {
        _playerBehaviour = FindObjectOfType<PlayerBehaviour>();
        
        if (_playerBehaviour != null)
        {
            // ◀️ StackManager의 이벤트에 UpdateUI 메서드를 구독
            _playerBehaviour.OnInputQueueChanged += UpdateUI;
            UpdateUI(); // ◀️ 시작할 때 한 번 초기화
        }
    }

    private void OnDestroy()
    {
        if (_playerBehaviour != null)
        {
            _playerBehaviour.OnInputQueueChanged -= UpdateUI;
        }
    }

}
