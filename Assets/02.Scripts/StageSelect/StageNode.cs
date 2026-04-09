using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using System;
using TMPro;

/// <summary>
/// 스테이지 셀렉트 월드맵의 각 스테이지 노드.
/// Button + ISelectHandler + IDeselectHandler 조합으로 EventSystem Explicit Navigation과 연동됩니다.
/// StageSelectionMarker와 같은 GameObject에 함께 부착 가능합니다.
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

    [Header("Info Panel Offset")]
    [SerializeField] private Vector2 panelOffset;

    [SerializeField] private StageInfoPanel infoPanel;
    
    // 플레이어가 이 노드에서 확인 입력을 눌렀을 때 발동
    public event Action<StageNode> OnConfirmed;

    private NodeState         _state;
    private Vector3           _originScale;
    private StageSelectPlayer _selectPlayer;
    private Button            _button;

    public NodeState CurrentState => _state;

    private void Awake()
    {
        _originScale  = transform.localScale;
        infoPanel.gameObject.SetActive(false);
        _selectPlayer = FindObjectOfType<StageSelectPlayer>();
        _button       = GetComponent<Button>();

        // Button onClick → Confirm() (Enter/Space 자동 연결)
        _button.onClick.AddListener(Confirm);
    }

    private void Start()
    {
        RefreshVisuals();
    }

    /// <summary>세이브 데이터를 읽어 노드의 시각 상태를 갱신합니다.</summary>
    public void RefreshVisuals()
    {
        _state = GetCurrentState();

        // 잠금 상태면 버튼 비활성화 (EventSystem 네비게이션에서 건너뜀)
        _button.interactable = _state != NodeState.Locked;

        if (iconImage != null)
        {
            iconImage.sprite = _state switch
            {
                NodeState.Locked   => spriteLocked,
                NodeState.Cleared  => spriteCleared,
                _                  => spriteAvailable,
            };
        }

        if (availableObject != null) availableObject.SetActive(_state == NodeState.Available);
        if (lockedOverlay    != null) lockedOverlay.SetActive(_state == NodeState.Locked);
        if (clearEffectObject != null) clearEffectObject.SetActive(_state == NodeState.Cleared);
    }

    public bool CanEnter() => _state != NodeState.Locked;

    // ── EventSystem 콜백 ──────────────────────────────────────────────

    public void OnSelect(BaseEventData eventData)
    {
        // 플레이어 캐릭터를 이 노드 위치로 이동
        _selectPlayer?.MoveTo(GetComponent<RectTransform>());

        // 정보 패널 갱신
        infoPanel?.Show(this, GetComponent<RectTransform>(), panelOffset);

        // 선택 강조 애니메이션
        infoPanel?.transform.DOKill();
        if(infoPanel != null) infoPanel.transform.localScale = _originScale* 4;
        infoPanel?.transform.DOPunchScale(Vector3.one * 0.2f, 0.25f, 6, 0.5f);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        transform.DOKill();
        transform.localScale = _originScale;
        infoPanel?.Hide();
    }

    // ── 진입 확인 ────────────────────────────────────────────────────

    /// <summary>Enter/Space(Button.onClick) 또는 F4(StageSelectManagement) 입력 시 호출됩니다.</summary>
    public void Confirm()
    {
        if (!CanEnter()) return;
        _selectPlayer?.PlayEnterSound();
        OnConfirmed?.Invoke(this);
    }

    // ── 상태 계산 ────────────────────────────────────────────────────

    private NodeState GetCurrentState()
    {
        if (stageData == null) return NodeState.Locked;

        var jdm = GameManager.Instance?.jsonDataManager;
        if (jdm == null) return NodeState.Available; // 에디터 단독 테스트 fallback

        var progress = jdm.GetStageData(stageData.chapterNum, stageData.stageNum);
        if (progress == null || !progress.isAppeared) return NodeState.Locked;
        if (progress.isCleared)                       return NodeState.Cleared;
        return NodeState.Available;
    }
}
