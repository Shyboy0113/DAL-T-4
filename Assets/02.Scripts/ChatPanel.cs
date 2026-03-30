using System.Collections;
using System.Collections.Generic;
using TMPro;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 게임 이벤트 알림 + 플레이어 직접 채팅을 Chat Panel에 표시합니다.
///
/// [씬 설정]
/// - messagePrefab  : TextMeshProUGUI 프리팹
/// - content        : VerticalLayoutGroup + ContentSizeFitter 자식 오브젝트
/// - chatInputField : TMP_InputField (기본 비활성 상태)
///
/// [한글 입력 처리]
/// TMP_InputField.onSubmit은 IME 조합이 완전히 확정된 뒤 호출됩니다.
/// Input.GetKeyDown(Return)으로 text를 직접 읽으면 마지막 글자가 누락되므로
/// 반드시 onSubmit 이벤트를 사용합니다.
/// </summary>
public class ChatPanel : MonoBehaviour
{
    [Header("메시지 설정")]
    [SerializeField] private TextMeshProUGUI messagePrefab;
    [SerializeField] private Transform       content;
    [SerializeField] private int             maxMessages  = 5;
    [SerializeField] private float           fadeDelay    = 3f;
    [SerializeField] private float           fadeDuration = 1f;

    [Header("채팅 입력")]
    [SerializeField] private TMP_InputField chatInputField;
    [SerializeField] private TMP_Text       enterTextLabel; // 평상시 표시되는 "Enter Text" 레이블

    private readonly List<(TextMeshProUGUI text, Coroutine coroutine)> _messages = new();
    private int        _lastClosedFrame    = -1;
    private GameObject _previousSelected; // 채팅 열기 전 EventSystem 타겟 보존

    private void OnEnable()
    {
        GameEvents.PlayerDied     += OnPlayerDied;
        GameEvents.EnemyDied      += OnEnemyDied;
        GameEvents.StageRestarted += OnStageRestarted;
    }

    private void OnDisable()
    {
        GameEvents.PlayerDied     -= OnPlayerDied;
        GameEvents.EnemyDied      -= OnEnemyDied;
        GameEvents.StageRestarted -= OnStageRestarted;
    }

    private void Start()
    {
        chatInputField.gameObject.SetActive(false);
        if (enterTextLabel != null) enterTextLabel.gameObject.SetActive(true);
        // onSubmit: IME 조합 완료 후 호출 → 한글 마지막 글자 누락 없음
        chatInputField.onSubmit.AddListener(OnChatSubmit);
    }

    private void Update()
    {
        // 입력 필드가 닫혀있을 때 Enter → 채팅 열기
        // _lastClosedFrame 체크: onSubmit과 같은 프레임에 다시 열리는 것 방지
        if (!chatInputField.gameObject.activeSelf &&
            Input.GetKeyDown(KeyCode.Return) &&
            Time.frameCount != _lastClosedFrame)
        {
            OpenChatInput();
            return;
        }

        // Escape → 채팅 취소
        if (chatInputField.gameObject.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseChatInput();
        }
    }

    private void OpenChatInput()
    {
        // 현재 EventSystem 타겟 저장 (Option 창 포커스 등 유지 필요)
        _previousSelected = EventSystem.current.currentSelectedGameObject;

        if (enterTextLabel != null) enterTextLabel.gameObject.SetActive(false);
        chatInputField.text = "";
        chatInputField.gameObject.SetActive(true);
        chatInputField.ActivateInputField();
        GameManager.Instance.isChatting = true;
    }

    private void CloseChatInput()
    {
        _lastClosedFrame = Time.frameCount;
        chatInputField.DeactivateInputField();
        chatInputField.gameObject.SetActive(false);
        if (enterTextLabel != null) enterTextLabel.gameObject.SetActive(true);
        GameManager.Instance.isChatting = false;

        // 채팅 열기 전 타겟으로 복원
        EventSystem.current.SetSelectedGameObject(_previousSelected);
        _previousSelected = null;
    }

    // onSubmit: IME 조합이 완전히 끝난 뒤 호출 → 한글 안전
    private void OnChatSubmit(string text)
    {
        string trimmed = text.Trim();
        if (!string.IsNullOrEmpty(trimmed))
            AddMessage($"<color=#AAFFAA>[나]</color> <color=#FFFFFF>{trimmed}</color>");

        CloseChatInput();
    }

    private void OnPlayerDied()
        => AddMessage("<color=#FF4444>[사망]</color> <color=#FFFFFF>플레이어가 사망했습니다.</color>");

    private void OnEnemyDied()
        => AddMessage("<color=#4488FF>[처치]</color> <color=#FFFFFF>적이 처치됐습니다.</color>");

    private void OnStageRestarted()
        => AddMessage("<color=#FFDD44>[재시작]</color> <color=#FFFFFF>스테이지가 재시작됐습니다.</color>");

    private void AddMessage(string text)
    {
        if (_messages.Count >= maxMessages)
        {
            var oldest = _messages[0];
            if (oldest.coroutine != null) StopCoroutine(oldest.coroutine);
            Destroy(oldest.text.gameObject);
            _messages.RemoveAt(0);
        }

        TextMeshProUGUI msg = Instantiate(messagePrefab, content);
        msg.text = text;
        msg.color = new Color(msg.color.r, msg.color.g, msg.color.b, 1f);

        Coroutine co = StartCoroutine(FadeOut(msg));
        _messages.Add((msg, co));
    }

    private IEnumerator FadeOut(TextMeshProUGUI msg)
    {
        yield return new WaitForSeconds(fadeDelay);

        msg.DOFade(0f, fadeDuration).OnComplete(() =>
        {
            int index = _messages.FindIndex(m => m.text == msg);
            if (index >= 0) _messages.RemoveAt(index);
            Destroy(msg.gameObject);
        });
    }
}
