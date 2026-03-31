using System;
using System.Collections.Generic;
using TMPro;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 게임 이벤트 알림 + 플레이어 직접 채팅을 Chat Panel에 표시합니다.
///
/// [메시지 유지 정책]
/// - 메시지는 스테이지 리셋 / 클리어 전까지 자동으로 사라지지 않습니다.
/// - maxMessages 초과 시 가장 오래된 메시지를 즉시 제거하고 새 메시지를 추가합니다.
/// - FadeOut은 ClearAll 시 전체 메시지에 일괄 적용됩니다.
///
/// [채팅 커맨드 / 이스터에그]
/// - 입력 문자열에서 등록된 키워드가 처음 등장하는 위치 기준으로 커맨드 하나를 실행합니다.
/// - 대소문자 무관, 부분 일치 허용.
///
/// [욕설 필터]
/// - _profanityList에 등록된 단어를 첫 글자 + *** 형태로 치환합니다.
///
/// [씬 설정]
/// - messagePrefab  : TextMeshProUGUI 프리팹
/// - content        : VerticalLayoutGroup + ContentSizeFitter 자식 오브젝트
/// - chatInputField : TMP_InputField (기본 비활성 상태)
/// </summary>
public class ChatPanel : MonoBehaviour
{
    // ── 커맨드 데이터 ─────────────────────────────────────────────────────────
    private readonly struct ChatCommand
    {
        public readonly string[] Keywords;   // 대소문자 무관 부분 일치
        public readonly string   EasterEgg;  // null이면 시스템 메시지 없음
        public readonly Action   Effect;     // null이면 미구현

        public ChatCommand(string[] keywords, string easterEgg, Action effect)
        {
            Keywords   = keywords;
            EasterEgg  = easterEgg;
            Effect     = effect;
        }
    }

    // ── 욕설 목록 (소문자로 작성) ────────────────────────────────────────────
    private static readonly string[] ProfanityList =
    {
        "fuck", "shit", "bitch", "bastard", "cock", "dick", "pussy", "cunt", "sex", "ass",
    };

    // ── Inspector ────────────────────────────────────────────────────────────
    [Header("메시지 설정")]
    [SerializeField] private TextMeshProUGUI messagePrefab;
    [SerializeField] private Transform       content;
    [SerializeField] private int             maxMessages  = 5;
    [SerializeField] private float           fadeDuration = 0.5f;

    [Header("채팅 입력")]
    [SerializeField] private TMP_InputField chatInputField;
    [SerializeField] private TMP_Text       enterTextLabel;

    [Header("채팅 커맨드 효과음")]
    [SerializeField] private AudioSource chatAudioSource; // 채팅 전용 AudioSource
    [SerializeField] private AudioClip   whistleSound;

    // ── 런타임 ───────────────────────────────────────────────────────────────
    private readonly List<TextMeshProUGUI> _messages = new();
    private ChatCommand[]                  _chatCommands;
    private int                            _lastClosedFrame = -1;
    private GameObject                     _previousSelected;

    // ── 초기화 ───────────────────────────────────────────────────────────────
    private void Awake()
    {
        // 커맨드 등록 ─ 키워드 배열, 시스템 메시지, 효과
        // counterrotate는 rotate보다 앞에 둬야 위치 비교에서 정확히 구분됩니다.
        _chatCommands = new[]
        {
            new ChatCommand(
                keywords  : new[] { "suicide" },
                easterEgg : "<color=#FF4444>[!!!]</color> <color=#FFAAAA>스스로 자멸을 선택했습니다...</color>",
                effect    : GameEvents.RaiseChatCommandSuicide
            ),
            new ChatCommand(
                keywords  : new[] { "counterrotate", "counter rotate" },
                easterEgg : "<color=#AAAAFF>[CCW]</color> <color=#FFFFFF>반시계 회전을 시도합니다.</color>",
                effect    : GameEvents.RaiseChatCommandRotateCCW
            ),
            new ChatCommand(
                keywords  : new[] { "rotate" },
                easterEgg : "<color=#AAAAFF>[CW]</color>  <color=#FFFFFF>시계 회전을 시도합니다.</color>",
                effect    : GameEvents.RaiseChatCommandRotateCW
            ),
            new ChatCommand(
                keywords  : new[] { "move" },
                easterEgg : "<color=#AAFFAA>[>>]</color>  <color=#FFFFFF>이동을 시도합니다.</color>",
                effect    : GameEvents.RaiseChatCommandMove
            ),
            new ChatCommand(
                keywords  : new[] { "dance" },
                easterEgg : "<color=#FFDD44>[DANCE]</color> <color=#FFFFFF>적들이 춤을 춥니다!</color>",
                effect    : GameEvents.RaiseChatCommandDance
            ),
            new ChatCommand(
                keywords  : new[] { "i love you" },
                easterEgg : "<color=#FF88CC>[LOVE]</color> <color=#FFFFFF>적들이 감동받았습니다!</color>",
                effect    : GameEvents.RaiseChatCommandLove
            ),
            new ChatCommand(
                keywords  : new[] { "whistle" },
                easterEgg : "<color=#AAFFFF>[~♪]</color>  <color=#FFFFFF>휘파람 소리가 울립니다.</color>",
                effect    : PlayWhistle
            ),
        };
    }

    private void OnEnable()
    {
        GameEvents.PlayerDied     += OnPlayerDied;
        GameEvents.EnemyDied      += OnEnemyDied;
        GameEvents.StageRestarted += OnStageRestarted;
        GameEvents.StageRestarted += ClearAll;
        GameEvents.StageCleared   += ClearAll;
    }

    private void OnDisable()
    {
        GameEvents.PlayerDied     -= OnPlayerDied;
        GameEvents.EnemyDied      -= OnEnemyDied;
        GameEvents.StageRestarted -= OnStageRestarted;
        GameEvents.StageRestarted -= ClearAll;
        GameEvents.StageCleared   -= ClearAll;
    }

    private void Start()
    {
        chatInputField.gameObject.SetActive(false);
        if (enterTextLabel != null) enterTextLabel.gameObject.SetActive(true);
        chatInputField.onSubmit.AddListener(OnChatSubmit);
    }

    // ── Update ───────────────────────────────────────────────────────────────
    private void Update()
    {
        if (!chatInputField.gameObject.activeSelf &&
            Input.GetKeyDown(KeyCode.Return) &&
            Time.frameCount != _lastClosedFrame)
        {
            OpenChatInput();
            return;
        }

        if (chatInputField.gameObject.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            CloseChatInput();
    }

    // ── 채팅 입력 열기 / 닫기 ────────────────────────────────────────────────
    private void OpenChatInput()
    {
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

        EventSystem.current.SetSelectedGameObject(_previousSelected);
        _previousSelected = null;
    }

    // ── 채팅 제출 ─────────────────────────────────────────────────────────────
    private void OnChatSubmit(string text)
    {
        string trimmed = text.Trim();
        CloseChatInput();

        if (string.IsNullOrEmpty(trimmed)) return;

        // 욕설 필터 적용 후 메시지 출력
        string filtered = ApplyProfanityFilter(trimmed);
        AddMessage($"<color=#AAFFAA>[나]</color> <color=#FFFFFF>{filtered}</color>");

        // 커맨드 / 이스터에그 처리
        ProcessChatCommand(trimmed);
    }

    // ── 욕설 필터 ─────────────────────────────────────────────────────────────
    /// <summary>
    /// 등록된 욕설을 '첫 글자 + *' 형태로 치환합니다. (대소문자 유지)
    /// 예: "FUCK" → "F***", "Sex" → "S**"
    /// </summary>
    private static string ApplyProfanityFilter(string text)
    {
        foreach (string word in ProfanityList)
        {
            int idx;
            while ((idx = text.IndexOf(word, StringComparison.OrdinalIgnoreCase)) >= 0)
            {
                char first    = text[idx];
                string masked = first + new string('*', word.Length - 1);
                text = text.Remove(idx, word.Length).Insert(idx, masked);
            }
        }
        return text;
    }

    // ── 커맨드 처리 ───────────────────────────────────────────────────────────
    /// <summary>
    /// 입력 문자열에서 가장 앞에 등장하는 키워드의 커맨드를 실행합니다.
    /// </summary>
    private void ProcessChatCommand(string original)
    {
        string lower   = original.ToLower();
        int    bestPos = int.MaxValue;
        int    bestIdx = -1;

        for (int i = 0; i < _chatCommands.Length; i++)
        {
            foreach (string kw in _chatCommands[i].Keywords)
            {
                int pos = lower.IndexOf(kw, StringComparison.Ordinal);
                if (pos >= 0 && pos < bestPos)
                {
                    bestPos = pos;
                    bestIdx = i;
                    break;
                }
            }
        }

        if (bestIdx < 0) return;

        ChatCommand cmd = _chatCommands[bestIdx];

        if (!string.IsNullOrEmpty(cmd.EasterEgg))
            AddMessage(cmd.EasterEgg);

        cmd.Effect?.Invoke();
    }

    // ── 채팅 커맨드 효과 ─────────────────────────────────────────────────────
    private void PlayWhistle()
    {
        if (chatAudioSource != null && whistleSound != null)
            chatAudioSource.PlayOneShot(whistleSound);
    }

    // ── 게임 이벤트 메시지 ────────────────────────────────────────────────────
    private void OnPlayerDied()
        => AddMessage("<color=#FF4444>[사망]</color> <color=#FFFFFF>플레이어가 사망했습니다.</color>");

    private void OnEnemyDied()
        => AddMessage("<color=#4488FF>[처치]</color> <color=#FFFFFF>적이 처치됐습니다.</color>");

    private void OnStageRestarted()
        => AddMessage("<color=#FFDD44>[재시작]</color> <color=#FFFFFF>스테이지가 재시작됐습니다.</color>");

    // ── 메시지 관리 ───────────────────────────────────────────────────────────
    private void AddMessage(string text)
    {
        if (_messages.Count >= maxMessages)
        {
            Destroy(_messages[0].gameObject);
            _messages.RemoveAt(0);
        }

        TextMeshProUGUI msg = Instantiate(messagePrefab, content);
        msg.text  = text;
        msg.color = new Color(msg.color.r, msg.color.g, msg.color.b, 1f);
        _messages.Add(msg);
    }

    private void ClearAll()
    {
        foreach (TextMeshProUGUI msg in _messages)
            FadeOut(msg);

        _messages.Clear();
    }

    private void FadeOut(TextMeshProUGUI msg)
    {
        msg.DOFade(0f, fadeDuration).OnComplete(() =>
        {
            if (msg != null) Destroy(msg.gameObject);
        });
    }
}
