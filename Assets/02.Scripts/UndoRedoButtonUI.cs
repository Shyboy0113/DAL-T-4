using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UndoRedoButtonUI : MonoBehaviour
{
    public enum ButtonType { Undo, Redo }
    [SerializeField] private ButtonType type;

    private Button _button;
    private TextMeshProUGUI _text; // 혹은 이미지 사용 시 Image
    private CanvasGroup _canvasGroup; // 투명도 조절용 (권장)

    [SerializeField] private Color activeColor = new Color(255f/255f, 255f/255f, 255f/255f, 196f/255f);
    [SerializeField] private Color deActiveColor = new Color (0.75f, 0.75f, 0.75f, 196f/255f);

    private void Awake()
    {
        _button = GetComponent<Button>();
        _text = GetComponentInChildren<TextMeshProUGUI>();
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void Start()
    {
        BehaviourManager behaviourManager = FindObjectOfType<BehaviourManager>();
        if (behaviourManager != null)
        {
            UpdateButtonState(behaviourManager.UndoCount, behaviourManager.RedoCount);
        }
    }

    private void OnEnable()
    {
        GameEvents.UndoRedoCountChanged += UpdateButtonState;
        
    }

    private void OnDisable()
    {
        GameEvents.UndoRedoCountChanged -= UpdateButtonState;
    }

    private void UpdateButtonState(int undoCount, int redoCount)
    {
        int currentCount = (type == ButtonType.Undo) ? undoCount : redoCount;
        bool isActive = currentCount > 0;

        // 1. 버튼 클릭 기능 활성/비활성화
        _button.interactable = isActive;

        // 2. 시각적 연출 (투명도 및 색상)
        if (isActive)
        {
            _canvasGroup.alpha = 1f;
            if (_text != null) _text.color = activeColor; // 원래 색
        }
        else
        {
            _canvasGroup.alpha = 0.75f; // 반투명
            if (_text != null) _text.color = deActiveColor; // 회색
        }
    }
}