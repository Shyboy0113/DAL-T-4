using System.Collections;
using System.Collections.Generic;
using TMPro;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 게임 이벤트 알림을 Chat Panel에 순차적으로 표시합니다.
///
/// [씬 설정]
/// - content         : VerticalLayoutGroup + ContentSizeFitter가 붙은 자식 오브젝트
/// - messagePrefab   : TextMeshProUGUI 컴포넌트가 붙은 프리팹
/// - maxMessages     : 최대 표시 메시지 수 (초과 시 가장 오래된 것 제거)
/// - fadeDelay       : 메시지가 사라지기 시작하기까지의 대기 시간 (초)
/// </summary>
public class ChatPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI messagePrefab;
    [SerializeField] private Transform       content;
    [SerializeField] private int             maxMessages  = 5;
    [SerializeField] private float           fadeDelay    = 3f;
    [SerializeField] private float           fadeDuration = 1f;

    private readonly List<(TextMeshProUGUI text, Coroutine coroutine)> _messages = new();

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

    private void OnPlayerDied()     => AddMessage("<color=#FF4444>[사망]</color> <color=#FFFFFF>플레이어가 사망했습니다.</color>");
    private void OnEnemyDied()      => AddMessage("<color=#4488FF>[처치]</color> <color=#FFFFFF>적이 처치됐습니다.</color>");
    private void OnStageRestarted() => AddMessage("<color=#FFDD44>[재시작]</color> <color=#FFFFFF>스테이지가 재시작됐습니다.</color>");

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

        Color c = msg.color;
        msg.color = new Color(c.r, c.g, c.b, 1f);

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
