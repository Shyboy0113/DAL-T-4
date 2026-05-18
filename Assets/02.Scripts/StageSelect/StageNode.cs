using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using System;

/// <summary>
/// 스테이지 셀렉트 월드맵의 각 스테이지 노드.
/// 자신의 시각 상태(잠금/해금/클리어)와 패널 UI를 보유하되,
/// 패널을 언제 열고 닫을지는 StageSelectManagement가 결정합니다.
/// </summary>
[RequireComponent(typeof(Button))]
public class StageNode : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    public enum NodeState { Locked, Available, Cleared }

    [Header("Data")]
    public SO_StageData stageData;

    [Header("Visuals")]
    [SerializeField] private Image  iconImage;
    [SerializeField] private Sprite spriteAvailable;
    [SerializeField] private Sprite spriteCleared;
    [SerializeField] private Sprite spriteLocked;
    [SerializeField] private GameObject availableObject;
    [SerializeField] private GameObject clearEffectObject;
    [SerializeField] private GameObject lockedOverlay;

    [Header("Stars")]
    [SerializeField] private GameObject star1;
    [SerializeField] private GameObject star2;
    [SerializeField] private GameObject star3;

    [Header("Info Panel")]
    [SerializeField] private Vector2 panelOffset;
    [SerializeField] private StageInfoPanel infoPanel;

    private Canvas _stageInfoCanvas;
    
    // ── 이벤트 (StageSelectManagement가 구독) ──
    public event Action<StageNode> OnConfirmed;
    public event Action<StageNode> OnSelected;
    public event Action<StageNode> OnDeselected;

    private NodeState _state;
    private Vector3   _originScale;
    private Button    _button;

    public NodeState CurrentState => _state;

    private void Awake()
    {
        _originScale = transform.localScale;
        _button      = GetComponent<Button>();

        _button.onClick.AddListener(Confirm);

        _stageInfoCanvas = GetComponentInChildren<Canvas>();
        
    }

    private void Start()
    {
        RefreshVisuals();
    }

    // ── 시각 상태 ─────────────────────────────────────────────

    /// <summary>세이브 데이터를 읽어 노드의 시각 상태를 갱신합니다.</summary>
    public void RefreshVisuals()
    {
        if (_button == null) _button = GetComponent<Button>();

        _state = GetCurrentState();
        _button.interactable = _state != NodeState.Locked;

        if (iconImage != null)
        {
            iconImage.sprite = _state switch
            {
                NodeState.Locked  => spriteLocked,
                NodeState.Cleared => spriteCleared,
                _                 => spriteAvailable,
            };
        }

        if (availableObject  != null) availableObject.SetActive(_state == NodeState.Available);
        if (lockedOverlay    != null) lockedOverlay.SetActive(_state == NodeState.Locked);
        if (clearEffectObject != null) clearEffectObject.SetActive(_state == NodeState.Cleared);

        RefreshStars();
    }

    private void RefreshStars()
    {
        int cleared = 0;

        if (stageData != null)
        {
            var jdm      = GameManager.Instance?.jsonDataManager;
            var progress = jdm?.GetStageData(stageData.chapterNum, stageData.stageNum);
            if (progress != null)
            {
                if (progress.isFirstMissionCleared)  cleared++;
                if (progress.isSecondMissionCleared) cleared++;
                if (progress.isThirdMissionCleared)  cleared++;
            }
        }

        if (star1 != null) star1.SetActive(cleared >= 1);
        if (star2 != null) star2.SetActive(cleared >= 2);
        if (star3 != null) star3.SetActive(cleared >= 3);
    }

    public bool CanEnter() => _state != NodeState.Locked;

    // ── 패널 제어 (Management가 호출) ─────────────────────────

    /// <summary>이 노드의 정보 패널을 표시합니다.</summary>
    public void ShowPanel()
    {
        if (infoPanel == null)
        {
            Debug.LogWarning($"[StageNode] {name}: infoPanel이 null - Inspector에서 StageInfoPanel을 할당해야 합니다.", this);  
            return;
        }
        
        _stageInfoCanvas.sortingOrder = stageData.chapterNum+1;

        var nodeRect = GetComponent<RectTransform>();
        infoPanel.Show(this, nodeRect, panelOffset);

        infoPanel.transform.DOKill();
        infoPanel.transform.localScale = _originScale * 4;
        infoPanel.transform.DOPunchScale(Vector3.one * 0.2f, 0.25f, 6, 0.5f);
    }

    /// <summary>이 노드의 정보 패널을 숨깁니다.</summary>
    public void HidePanel()
    {
        if (infoPanel == null) return;

        infoPanel.transform.DOKill();
        infoPanel.Hide();
    }

    // ── EventSystem 콜백 ──────────────────────────────────────

    public void OnSelect(BaseEventData eventData)
    {
        OnSelected?.Invoke(this);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        transform.DOKill();
        transform.localScale = _originScale;
        OnDeselected?.Invoke(this);
    }

    /// <summary>
    /// Space/Enter(Button.onClick) 입력 시 호출됩니다.
    /// 패널 열기/진입 판단은 Management가 처리합니다.
    /// </summary>
    public void Confirm()
    {
        Debug.Log($"[StageNode] Confirm | name={name} | state={_state} | CanEnter={CanEnter()} | listeners={OnConfirmed?.GetInvocationList().Length ?? 0}");
        if (!CanEnter()) return;
        OnConfirmed?.Invoke(this);
    }

    // ── 상태 계산 ─────────────────────────────────────────────

    private NodeState GetCurrentState()
    {
        if (stageData == null) return NodeState.Locked;

        var jdm = GameManager.Instance?.jsonDataManager;
        if (jdm == null) return NodeState.Available;

        var progress = jdm.GetStageData(stageData.chapterNum, stageData.stageNum);
        if (progress == null || !progress.isAppeared) return NodeState.Locked;
        if (progress.isCleared)                       return NodeState.Cleared;
        return NodeState.Available;
    }

    private void OnDestroy()
    {
        transform.DOKill(this);
    }
}