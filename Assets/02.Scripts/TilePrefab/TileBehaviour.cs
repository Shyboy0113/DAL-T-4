using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections; // 코루틴 사용
using System;
using System.Collections.Generic; // [Flags] 사용

public enum TileType
{
    None,
    
    // 맵 회전 판정
    QuarterClockwiseRotation,
    HalfClockwiseRotation,
    QuarterCounterClockwiseRotation,
    HalfCounterClockwiseRotation,

    // 텔레포트 판정
    StartTeleport,
    EndTeleport,
    
    // 밟을 수 있는 횟수에 제한이 있는 타일
    Breakable,

    // 미끄러짐 판정
    Ice,
    Stop,

    // 클리어 판정
    FirstDestination,
    SecondDestination,
    
    // 토글 판정
    StepOnToggle, // 플레이어가 '직접' 밟았을 때 ToggleTargeted 타일 토글 처리
    ToggleTargeted, // 토글되는 타일, 토글 상태일 때 밟으면 게임오버

    TrapToggle, //
    ActiveToggle,
    MoveToggle,
    RotationToggle,
    //GameEvents : RaiseToggleTriggered 호출
    //Logic : 모든 ToggleTargeted 타일이 토글

    ColorToggle,
    ConditionalToggle // 자율적으로 구현
}

[Flags] // 비트 플래그 사용
public enum TileColor
{
    Black = 0,
    Blue = 1 << 0,
    Green = 1 << 1,
    Red = 1 << 2,
    Yellow = Red | Green,
    Cyan = Green | Blue,
    Magenta = Red | Blue,
    White = Red | Green | Blue
}

/*
TileSprite 배치 순서
0	None	빈 타일 혹은 기본 바닥
1	QuarterClockwiseRotation	90도 시계 방향 회전 타일
2	HalfClockRotation	180도 회전 타일
3	QuarterCounterClockwiseRotation	90도 반시계 방향 회전 타일
4	HalfCounterClockRotation	180도 반시계 방향 회전 타일
5	StartTeleport	텔레포트 시작 (ID가 0일 때 기본 이미지)
6	EndTeleport	텔레포트 도착 (ID가 0일 때 기본 이미지)
7	Breakable	파괴 가능 타일 (breakableSprites 배열이 우선됨)
8	Ice	얼음 타일 (미끄러짐)
9	Stop	정지 타일
10	FirstDestination	첫 번째 목적지
11	SecondDestination	두 번째 목적지
12	StepOnToggle	발판형 토글 스위치
13	ToggleTargeted	토글 대상 타일
14	TrapToggle	함정 토글 타일
15	ActiveToggle	행동 횟수 토글
16	MoveToggle	이동 횟수 토글
17	RotationToggle	회전 횟수 토글
18	ColorToggle	컬러 토글 스위치
19	ConditionalToggle	조건부 토글 타일
*/

public class TileBehaviour : BaseTile
{
    [Header("Scriptable Object Data")]
    [SerializeField] private List<SOTileData> allDataAssets; // 모든 스크립터블 오브젝트가 포함돼있는 리스트
    [SerializeField] private SOTileData tileData; // ScriptableObject로 타일 데이터 관리

    [Header("Individual Overrides")]
    [SerializeField] private bool overrideStats = false;
    [SerializeField] private int overrideMaxActivationCount = -1;
    [SerializeField] private int overrideBreakHitCount = 2;
    [SerializeField] private TileColor overrideColor = TileColor.White;

   // --- 데이터 값 결정 로직 (Property) ---
    private int CurrentMaxActivationCount => overrideStats ? overrideMaxActivationCount : (tileData ? tileData.baseMaxActivationCount : maxActivationCount);
    private int CurrentBreakHitCount => overrideStats ? overrideBreakHitCount : (tileData ? tileData.baseBreakHitCount : breakHitCount);
    private float CurrentBreakDelay => tileData ? tileData.baseBreakDelay : breakDelay;
    private TileColor CurrentTileColor => overrideStats ? overrideColor : (tileData ? tileData.baseColor : TileColor.White);
    private int CurrentToggleActivationCount => tileData ? tileData.baseToggleActivationCount : toggleActivationCount;
    
    [Header("Tile Settings")]
    [SerializeField] private TileType manualTileType; 
    public TileType currentTileType => tileData != null ? tileData.tileType : manualTileType; // 외부에서 읽기 전용으로 접근

    [SerializeField] private Sprite[] tileSprites;

    // ColorTile일 경우 검정색일 때 버튼이 사라지는 문제 해결
    [Header("Renderers")]
    [SerializeField] private SpriteRenderer bgRenderer;   // 하얀 배경용 (항상 흰색)
    [SerializeField] private SpriteRenderer iconRenderer; // 가운데 버튼/아이콘용 (색상 변경)

    [Header("Activation & Stats")]
    [SerializeField] private int maxActivationCount = -1;

    private int _currentActivationCount = 0;
    private bool _isWaitExit = false;
    private bool _isPlayerOnMe = false;

    [Header("SFX & Visuals")]
    private AudioSource _effectSound;
    
    [SerializeField] private AudioClip toggleSound;
    [SerializeField] private AudioClip rotationSound;
    [SerializeField] private AudioClip crackSound;
    [SerializeField] private AudioClip breakSound;

    [Header("Breakable")]
    [SerializeField] private Sprite[] breakableSprites;
    [SerializeField] private int breakHitCount = 2;
    [SerializeField] private float breakDelay = 0.5f;

    private int _currentHit = 0;

    [Header("Toggle")]
    [SerializeField] private bool isToggled = false;
    [SerializeField] private Sprite toggleOffSprite;

    [SerializeField] private int toggleActivationCount = 2;
    [SerializeField] private StackManager player; // stackCount 참조용

    public bool IsReactiveTile()
    {
        return currentTileType == TileType.ToggleTargeted || 
           //currentTileType == TileType.StepOnToggle ||
           currentTileType == TileType.ActiveToggle ||
           currentTileType == TileType.MoveToggle ||
           currentTileType == TileType.RotationToggle ||
           //currentTileType == TileType.ColorToggle ||
           currentTileType == TileType.TrapToggle;
    }
    
    [Header("Animations")]
    [SerializeField] private Animator animator;

    private bool IsAnimatedTile()
    {
        return currentTileType == TileType.ToggleTargeted || 
            currentTileType == TileType.StepOnToggle ||
            currentTileType == TileType.ActiveToggle ||
            currentTileType == TileType.MoveToggle ||
            currentTileType == TileType.RotationToggle ||
            //currentTileType == TileType.ColorToggle ||
            currentTileType == TileType.TrapToggle;
    }

    private Color GetUnityColor(TileColor tileColor)
    {
        return tileColor switch
        {
            TileColor.Black   => new Color(50/255f, 50/255f, 50/255f, 1f),
            TileColor.Blue    => Color.blue,
            TileColor.Green   => Color.green,
            TileColor.Red     => Color.red,
            TileColor.Yellow  => Color.yellow,
            TileColor.Cyan    => Color.cyan,
            TileColor.Magenta => Color.magenta,
            TileColor.White   => Color.white,
            _                 => Color.white
        };
    }

    #region Teleport Logic
    [Header("Teleport")]
    [SerializeField] private TileBehaviour teleportTarget;
    [SerializeField] private int overrideTeleportID = 0;

    private int CurrentTeleportID => overrideStats ? overrideTeleportID : (tileData? tileData.baseTeleportID : 0);

        private void FindTeleportTargetByID()
    {
        int myID = CurrentTeleportID;
        if (myID == 0) return; // ID가 0이면 무시하거나 수동 연결 대기

        // 맵상의 모든 TileBehaviour를 탐색 (FindObjectsSortMode는 성능을 위해 None)
        TileBehaviour[] allTiles = FindObjectsByType<TileBehaviour>(FindObjectsSortMode.None);
        TileBehaviour foundTarget = null;
        int matchCount = 0;

        foreach (var tile in allTiles)
        {
            // 타입이 EndTeleport이고 ID가 일치하는지 확인
            if (tile.currentTileType == TileType.EndTeleport && tile.CurrentTeleportID == myID)
            {
                foundTarget = tile;
                matchCount++;
            }
        }

        // --- Fat Finger 디버깅 로직 ---
        if (matchCount > 1)
        {
            // 에러 로그에 해당 오브젝트를 첨부하여 하이라이트되게 함
            Debug.LogError($"<color=red>[Teleport Error]</color> 중복된 <b>EndTeleport ID</b> 발견! (ID: {myID}). 현재 맵에 {matchCount}개가 존재합니다.", gameObject);
        }
        else if (foundTarget == null)
        {
            Debug.LogWarning($"<color=yellow>[Teleport Warning]</color> ID {myID}에 해당하는 <b>EndTeleport</b>를 찾지 못했습니다.", gameObject);
        }

        // 찾은 타겟을 할당 (중복일 경우 마지막에 찾은 것으로 일단 할당됨)
        teleportTarget = foundTarget;
    }

    #endregion

    private Tilemap _tilemap;

    private void Awake()
    {
        _tilemap = GetComponentInParent<Tilemap>();
        _effectSound = GetComponentInParent<AudioSource>();
        
        if (animator == null) animator = GetComponent<Animator>();
        animator.enabled = IsAnimatedTile();
        
        if (player == null) player = FindObjectOfType<StackManager>();

        // StartTeleport 타일일 경우, SOTileData 내부의 teleportID가 동일한 EndTeleport 타일을 자동으로 탐색
        if (currentTileType == TileType.StartTeleport)
        {
            FindTeleportTargetByID();
        }

        UpdateVisuals();

    }

    #if UNITY_EDITOR
    private void OnValidate()
    {
        // [자동 할당] manualTileType에 맞는 SOTileData를 리스트에서 찾아 할당
        if (allDataAssets != null && allDataAssets.Count > 0)
        {
            SOTileData matchedData = allDataAssets.Find(data => data != null && data.tileType == manualTileType);
            if (matchedData != null && tileData != matchedData)
            {
                tileData = matchedData;
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }

        // [즉시 갱신] 인스펙터의 수치나 타입이 바뀔 때마다 스프라이트와 색상을 다시 그림
        UpdateVisuals();

    }

    #endif

    private void OnEnable()
    {

        // ColorToggle 공통 구독
        GameEvents.ColorToggleTriggered += HandleColorToggle;

        switch (currentTileType)
        {
            case TileType.ToggleTargeted:
            GameEvents.ToggleTriggered += HandleToggle;
            break;

            case TileType.ActiveToggle:
            GameEvents.PlayerActed += HandleToggle;
            break;

            case TileType.MoveToggle:
            GameEvents.PlayerMoved += HandleToggle; 
            break;

            case TileType.RotationToggle: 
            GameEvents.PlayerRotated += HandleToggle;
            break;
        }
    }

    private void OnDisable()
    {
        GameEvents.ColorToggleTriggered -= HandleColorToggle;
        GameEvents.ToggleTriggered -= HandleToggle;
        GameEvents.PlayerActed -= HandleToggle;
        GameEvents.PlayerMoved -= HandleToggle;
        GameEvents.PlayerRotated -= HandleToggle;
    }

    private void HandleColorToggle(TileColor color)
    {
        if (currentTileType == TileType.ColorToggle) return; // 스위치는 신호를 받아도 변하지 않음

        if ((CurrentTileColor & color) != 0) // 비트 플래그 검사
        {
            ToggleState();
        }
    }

    private void HandleToggle(int currentCount)
    {
        //toggleActivationCount : 몇 번째 행동마다 토글할지 설정

        // ActiveToggle : toggleActionCount
        // MoveToggle : moveCount
        // RotationToggle : rotationCount
        
        if (currentCount == -1 ||
            (currentCount > 0 && currentCount % CurrentToggleActivationCount == 0))
        {
            ToggleState();
        }
    }
    
    private void ToggleState()
    {
        isToggled = !isToggled;

        UpdateVisuals();

        if (GetComponent<Collider2D>() is Collider2D col) col.enabled = isToggled;
        if (currentTileType == TileType.RotationToggle && !isToggled && _isPlayerOnMe && player != null) player.PlayExplosion();
    }

    // 애니메이션 추가로 생성된 코드
    private void UpdateVisuals()
    {
        UpdateSprite();

        // 애니메이션을 사용하는 타일일 경우
        if (animator != null && animator.enabled)
        {
            string stateName = currentTileType.ToString();
            if (!isToggled) stateName = "Reverse" + stateName;

            // Transition 없이 즉시 해당 애니메이션의 0초 지점으로 이동
            animator.Play(stateName, 0, 0f);
        }
    }

    private void UpdateSprite()
    {
        if (bgRenderer == null || iconRenderer == null) return;

        // 1. 공통 배경 설정
        bgRenderer.sprite = tileSprites[0]; // 0번을 기본 배경으로 사용
        bgRenderer.color = Color.white;

        // 2. 아이콘 색상 설정
        iconRenderer.color = (currentTileType == TileType.ColorToggle || IsReactiveTile()) 
                            ? GetUnityColor(CurrentTileColor) 
                            : Color.white;

        // 3. 상황에 맞는 아이콘 스프라이트 선택 (불필요한 중복 제거 및 통합)
        Sprite nextIcon = null;

        if (currentTileType == TileType.Breakable && breakableSprites?.Length > 0)
        {
            nextIcon = breakableSprites[Mathf.Clamp(_currentHit, 0, breakableSprites.Length - 1)];
        }
        else if (currentTileType == TileType.StartTeleport || currentTileType == TileType.EndTeleport)
        {
            int id = CurrentTeleportID;
            int spriteIdx = (int)currentTileType + (id > 0 ? id : 0);
            nextIcon = (spriteIdx < tileSprites.Length) ? tileSprites[spriteIdx] : tileSprites[(int)currentTileType];
        }
        else if (currentTileType == TileType.ToggleTargeted)
        {
            nextIcon = isToggled ? tileSprites[(int)currentTileType] : toggleOffSprite;
        }
        else
        {
            nextIcon = tileSprites[(int)currentTileType];
        }

        // 4. 최종 적용
        iconRenderer.sprite = nextIcon;
    }

    protected override void OnPlayerEnter(StackManager player)
    {
        _isPlayerOnMe = true;
        if (CurrentMaxActivationCount != -1 && _currentActivationCount >= CurrentMaxActivationCount) return;
        
        if (_isWaitExit) return;
        _isWaitExit = true;

        _currentActivationCount++;
        
        if (IsRotationTile() || currentTileType == TileType.Ice || currentTileType == TileType.Stop || currentTileType == TileType.StartTeleport)
            player.transform.position = new Vector3(transform.position.x, transform.position.y, player.transform.position.z);

        switch (currentTileType)
        {        
            // 회전 타일
            case TileType.QuarterClockwiseRotation:
            RotateTile(-90f);
            if (rotationSound) _effectSound.PlayOneShot(rotationSound);
            break;
            
            case TileType.HalfClockwiseRotation:
            RotateTile(-180f);
            if (rotationSound) _effectSound.PlayOneShot(rotationSound);
            break;
            
            case TileType.QuarterCounterClockwiseRotation: 
            RotateTile(90f);
            if (rotationSound) _effectSound.PlayOneShot(rotationSound);
            break;

            case TileType.HalfCounterClockwiseRotation: 
            RotateTile(180f);
            if (rotationSound) _effectSound.PlayOneShot(rotationSound);
            break;

            case TileType.StartTeleport:
            if (teleportTarget) player.TeleportTo(teleportTarget.transform.position);
            break;

            case TileType.EndTeleport: break;

            case TileType.Breakable:
                _currentHit++;
                UpdateSprite();
                if (crackSound) _effectSound.PlayOneShot(crackSound);
            break;

            case TileType.Ice:
            player.EnableIceMode(true);
            break;

            case TileType.Stop:
            player.StopAllCoroutines();
            player.EnableIceMode(false);
            break;

            case TileType.FirstDestination:
                if (player.IsFirstTile())
                {
                    player.ReachedDestination();
                }
                break;

            case TileType.SecondDestination:
                if (!player.IsFirstTile())
                {
                    player.ReachedDestination();
                }
                break;

            case TileType.StepOnToggle:
            GameEvents.RaiseToggleTriggered(-1); // -1일 경우 
            if (toggleSound) _effectSound.PlayOneShot(toggleSound);
            break;

            case TileType.ToggleTargeted:
            if (!isToggled) player.PlayExplosion();
            break;

            case TileType.ActiveToggle:
            if (!isToggled) player.PlayExplosion();
            break;

            case TileType.MoveToggle:
            if (!isToggled) player.PlayExplosion();
            break;            

            case TileType.RotationToggle: 
            if (!isToggled) player.PlayExplosion();
            break;

            case TileType.ColorToggle:
            GameEvents.RaiseColorToggleTriggered(CurrentTileColor);
            break;

            case TileType.ConditionalToggle:
            break;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            _isPlayerOnMe = false;
            if (player && player.IsRotating()) return;
            _isWaitExit = false;
            if (currentTileType == TileType.Breakable && _currentHit >= CurrentBreakHitCount) StartCoroutine(BreakTile());
        }
    }

    private bool IsRotationTile(){    
        return currentTileType == TileType.HalfClockwiseRotation ||
               currentTileType == TileType.HalfCounterClockwiseRotation ||
               currentTileType == TileType.QuarterClockwiseRotation ||
               currentTileType == TileType.QuarterCounterClockwiseRotation;
    }

    private void RotateTile(float angle) => GameEvents.RaiseTileMapRotated(_tilemap.WorldToCell(transform.position), angle);

    private IEnumerator BreakTile()
    {
        yield return new WaitForSeconds(CurrentBreakDelay);
        if (breakSound) _effectSound.PlayOneShot(breakSound);
        _tilemap.SetTile(_tilemap.WorldToCell(transform.position), null);
        Destroy(gameObject);
    }
}