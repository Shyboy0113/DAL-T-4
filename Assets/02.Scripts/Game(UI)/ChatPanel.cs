using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization.Settings;

public class ChatPanel : MonoBehaviour
{
    private readonly struct ChatCommand
    {
        public readonly string[] Keywords;
        public readonly string   EasterEggKey; // String Table 키
        public readonly Action   Effect;

        public ChatCommand(string[] keywords, string easterEggKey, Action effect)
        {
            Keywords     = keywords;
            EasterEggKey = easterEggKey;
            Effect       = effect;
        }
    }

    private static readonly string[] ProfanityList =
    {
        "fuck", "shit", "bitch", "bastard", "cock", "dick", "pussy", "cunt", "sex", "ass",
        "씨발", "시발", "ㅅㅂ", "ㅆㅂ", "병신", "ㅂㅅ", "지랄", "ㅈㄹ",
        "개새끼", "새끼", "ㅅㄲ", "미친", "꺼져", "닥쳐", "존나", "ㅈㄴ",
    };

    [Header("메시지 설정")]
    [SerializeField] private TextMeshProUGUI messagePrefab;
    [SerializeField] private Transform       content;
    [SerializeField] private int             maxMessages  = 5;
    [SerializeField] private float           fadeDuration = 0.5f;

    [Header("채팅 입력")]
    [SerializeField] private TMP_InputField chatInputField;
    [SerializeField] private TMP_Text       enterTextLabel;

    [Header("채팅 커맨드 효과음")]
    [SerializeField] private AudioSource chatAudioSource;
    [SerializeField] private AudioClip   whistleSound;

    [Header("로컬라이징")]
    [SerializeField] private string stringTableName = "Game Chat Strings";

    private readonly List<TextMeshProUGUI> _messages = new();
    private ChatCommand[]                  _chatCommands;
    private int                            _lastClosedFrame = -1;
    private GameObject                     _previousSelected;

    private void Awake()
    {
        _chatCommands = new[]
        {
            new ChatCommand(
                keywords     : new[] { "suicide" },
                easterEggKey : "chat_suicide",
                effect       : GameEvents.RaiseChatCommandSuicide
            ),
            new ChatCommand(
                keywords     : new[] { "counterrotate", "counter rotate", "TAB", "tab", "탭", "역회전", "반대로", "반대로 회전" },
                easterEggKey : "chat_ccw",
                effect       : GameEvents.RaiseChatCommandRotateCCW
            ),
            new ChatCommand(
                keywords     : new[] { "rotate", "turn", "TURN","ALT", "alt", "LeftALT", "Leftalt", "Left alt", "알트", "레프트알트", "레프트 알트", "회전" },
                easterEggKey : "chat_cw",
                effect       : GameEvents.RaiseChatCommandRotateCW
            ),
            new ChatCommand(
                keywords     : new[] { "move", "F4", "f4", "push", "go", "앞으로", "앞", "무브", "이동" },
                easterEggKey : "chat_move",
                effect       : GameEvents.RaiseChatCommandMove
            ),
            new ChatCommand(
                keywords     : new[] { "dance", "댄스" },
                easterEggKey : "chat_dance",
                effect       : GameEvents.RaiseChatCommandDance
            ),
            new ChatCommand(
                keywords     : new[] { "i love you", "love" },
                easterEggKey : "chat_love",
                effect       : GameEvents.RaiseChatCommandLove
            ),
            new ChatCommand(
                keywords     : new[] { "whistle", "whistling" },
                easterEggKey : "chat_whistle",
                effect       : GameEvents.RaiseChatCommandWhistle
            ),
            new ChatCommand(
                keywords     : new[] { "esc", "escape", "옵션","일시정지","pause", "PAUSE", "멈춤" },
                easterEggKey : "chat_pause",
                effect       : GameEvents.RaiseChatCommandPause
            ),
            new ChatCommand(
                keywords     : new[] { "restart", "reset", "재시작", "다시 시작", "다시시작", "리스타트", "리셋", "처음", "초기화" },
                easterEggKey : "chat_restart",
                effect       : GameEvents.RaiseChatCommandRestart
            ),
            new ChatCommand(
                keywords     : new[] { "undo", "control + z", "ctrl + z", "ctrl z", "control z", "취소", "되돌리기", "언두","언도" },
                easterEggKey : "chat_undo",
                effect       : GameEvents.RaiseChatCommandUndo
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
        GameEvents.ChatCommandWhistle += PlayWhistle;
    }

    private void OnDisable()
    {
        GameEvents.PlayerDied     -= OnPlayerDied;
        GameEvents.EnemyDied      -= OnEnemyDied;
        GameEvents.StageRestarted -= OnStageRestarted;
        GameEvents.StageRestarted -= ClearAll;
        GameEvents.StageCleared   -= ClearAll;
        GameEvents.ChatCommandWhistle -= PlayWhistle;
    }

    private void Start()
    {
        chatInputField.gameObject.SetActive(false);
        if (enterTextLabel != null) enterTextLabel.gameObject.SetActive(true);
        chatInputField.onSubmit.AddListener(OnChatSubmit);
    }

    private void Update()
    {
        if (GameManager.Instance is null
            || GameManager.Instance.isOption) return;
        
        if (!chatInputField.gameObject.activeSelf &&
            Input.GetKeyDown(KeyCode.T) &&
            Time.frameCount != _lastClosedFrame)
        {
            OpenChatInput();
            return;
        }

        if (chatInputField.gameObject.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            CloseChatInput();
    }

    // ── 로컬라이즈 헬퍼 ──────────────────────────────────────────────────────
    private string L(string key)
    {
        return LocalizationSettings.StringDatabase.GetLocalizedString(stringTableName, key);
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

        string filtered = ApplyProfanityFilter(trimmed);
        AddMessage($"{L("chat_me")} <color=#FFFFFF>{filtered}</color>");

        if (filtered != trimmed)
            AddMessage(L("chat_profanity"));

        ProcessChatCommand(trimmed);
    }
    
    // ── 욕설 필터 ─────────────────────────────────────────────────────────────
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
                if (pos < 0 || pos >= bestPos) continue;

                bool startOk = (pos == 0 || !char.IsLetterOrDigit(lower[pos - 1]));
                bool endOk   = (pos + kw.Length >= lower.Length || !char.IsLetterOrDigit(lower[pos + kw.Length]));

                if (startOk && endOk)
                {
                    bestPos = pos;
                    bestIdx = i;
                    break;
                }
            }
        }

        if (bestIdx < 0) return;

        ChatCommand cmd = _chatCommands[bestIdx];

        if (!string.IsNullOrEmpty(cmd.EasterEggKey))
            AddMessage(L(cmd.EasterEggKey));

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
        => AddMessage(L("chat_died"));

    private void OnEnemyDied()
        => AddMessage(L("chat_enemy_died"));

    private void OnStageRestarted()
        => AddMessage(L("chat_restart"));

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

    public void ClearAll()
    {
        foreach (TextMeshProUGUI msg in _messages)
            StartCoroutine(IFadeOut(msg));

        _messages.Clear();
    }

    private IEnumerator IFadeOut(TextMeshProUGUI msg)
    {
        yield return new WaitForSeconds(1.0f);

        msg.DOFade(0f, fadeDuration).OnComplete(() =>
        {
            if (msg != null) Destroy(msg.gameObject);
        });
    }
}