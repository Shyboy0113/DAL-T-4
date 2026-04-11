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

    [Header("Stars")]
    [SerializeField] private GameObject star1;
    [SerializeField] private GameObject star2;
    [SerializeField] private GameObject star3;

    [Header("Info Panel Offset")]
    [SerializeField] private Vector2 panelOffset;

    [SerializeField] private StageInfoPanel infoPanel;
    
    // 플레이어가 이 노드에서 확인 입력을 눌렀을 때 발동
    public event Action<StageNode> OnConfirmed;

    private NodeState         _state;
    private Vector3           _originScale;
    private StageSelectPlayer _selectPlayer;
    private Button            _button;
    private bool              _isPanelOpen;

    public bool IsPanelOpen => _isPanelOpen;

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
        // 비활성 오브젝트에서 호출될 경우 Awake()가 아직 실행되지 않았을 수 있으므로 lazy init
        if (_button == null) _button = GetComponent<Button>();

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

    // ── EventSystem 콜백 ──────────────────────────────────────────────

    public void OnSelect(BaseEventData eventData)
    {
        // 플레이어 캐릭터를 이 노드 위치로 이동 (패널은 스페이스바 입력 시 표시)
        _selectPlayer?.MoveTo(GetComponent<RectTransform>());
    }

    public void OnDeselect(BaseEventData eventData)
    {
        transform.DOKill();
        transform.localScale = _originScale;
        ClosePanel();
    }

    /// <summary>열려 있는 패널을 닫습니다. Esc 입력 시 StageSelectManagement에서도 호출합니다.</summary>
    public void ClosePanel()
    {
        if (!_isPanelOpen) return;
        infoPanel?.Hide();
        _isPanelOpen = false;
    }

    // ── 진입 확인 ────────────────────────────────────────────────────

    /// <summary>
    /// Space/Enter(Button.onClick) 입력 시 호출됩니다.
    /// 패널이 닫혀 있으면 패널을 열고, 열려 있으면 스테이지로 진입합니다.
    /// </summary>
    public void Confirm()
    {
        if (!CanEnter()) return;

        if (!_isPanelOpen)
        {
            infoPanel?.Show(this, GetComponent<RectTransform>(), panelOffset);
            if (infoPanel != null)
            {
                infoPanel.transform.DOKill();
                infoPanel.transform.localScale = _originScale * 4;
                infoPanel.transform.DOPunchScale(Vector3.one * 0.2f, 0.25f, 6, 0.5f);
            }
            _isPanelOpen = true;
        }
        else
        {
            _selectPlayer?.PlayEnterSound();
            OnConfirmed?.Invoke(this);
        }
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
