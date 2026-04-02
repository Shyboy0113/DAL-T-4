using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UndoButtonUI : MonoBehaviour
{
    public enum ButtonType { Undo,  }
    [SerializeField] private ButtonType type;

    private Button _button;
    private TextMeshProUGUI _text; // 혹은 이미지 사용 시 Image
    private CanvasGroup _canvasGroup; // 투명도 조절용 (권장)

    // Button의 Navigation 관련 할당
    [SerializeField] private Button leftButton;  // Map Button
    [SerializeField] private Button rightButton; // Restart Button
    
    private void Awake()
    {
        _button = GetComponent<Button>();
        _text = GetComponentInChildren<TextMeshProUGUI>();
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        GameEvents.UndoCountChanged += UpdateButtonState;

        // 씬 시작 또는 재활성화 시 항상 비활성 상태로 초기화
        // Start()에서 BehaviourManager를 직접 조회하던 방식은 실행 순서에 따라
        // 타이밍이 불안정했으므로 제거합니다.
        UpdateButtonState(0, 0);
    }

    private void OnDisable()
    {
        GameEvents.UndoCountChanged -= UpdateButtonState;
    }

    private void UpdateButtonState(int undoCount, int Count)
    {
        int currentCount = (type == ButtonType.Undo) ? undoCount : Count;
        bool isActive = currentCount > 0;

        // 1. 버튼 클릭 기능 활성/비활성화
        _button.interactable = isActive;
        
        // 주변 버튼들의 Navigation 동적 변경
        if (leftButton != null)
        {
            Navigation leftNav = leftButton.navigation;
            leftNav.selectOnRight = isActive ? _button : rightButton;
            leftButton.navigation = leftNav;
        }

        if (rightButton != null)
        {
            Navigation rightNav = rightButton.navigation;
            rightNav.selectOnLeft = isActive ? _button : leftButton;
            rightButton.navigation = rightNav;
        }

        // 2. 시각적 연출 (투명도 및 색상)
        if (isActive)
        {
            _canvasGroup.alpha = 1f;
        }
        else
        {
            _canvasGroup.alpha = 0.75f; // 반투명
        }
    }
}