using UnityEngine;
using TMPro;
using DG.Tweening;

public class SequenceUI : MonoBehaviour
{
    [SerializeField] private TMP_Text[] _tmp_Text = new TMP_Text[3];
    [SerializeField] private PlayerBehaviour _playerBehaviour;

    private void UpdateUI()
    {
        if (_playerBehaviour is null) return;

        for (int i = 0; i < 3; i++)
        {
            int    key     = _playerBehaviour.CheckInputQueue(i);
            string newText = InputText(i, key);

            // 텍스트가 실제로 바뀔 때만 애니메이션
            if (_tmp_Text[i].text == newText) continue;

            _tmp_Text[i].text = newText;

            _tmp_Text[i].transform.DOKill();
            _tmp_Text[i].transform.localScale = Vector3.one;
            _tmp_Text[i].transform
                .DOPunchScale(Vector3.one * 0.4f, 0.2f, 5, 0.5f);
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
            _playerBehaviour.OnInputQueueChanged += UpdateUI;
            UpdateUI();
        }
    }

    private void OnDestroy()
    {
        if (_playerBehaviour != null)
            _playerBehaviour.OnInputQueueChanged -= UpdateUI;
    }
}