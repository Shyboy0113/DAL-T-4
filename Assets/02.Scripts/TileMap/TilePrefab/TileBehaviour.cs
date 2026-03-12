using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections; // 코루틴 사용
using System;
using System.Collections.Generic;
using TMPro;

using DG.Tweening;

[System.Serializable]
public struct TileStateSnapshot
{
    public int hitCount;
    public bool isToggled;
    public int activationCount;
    public Quaternion rotation;
    public bool isVisible; // Breakable 타일의 파괴 여부 체크용

    public TileStateSnapshot(int hit, bool toggled, int activation, Quaternion rot, bool visible)
    {
        hitCount = hit;
        isToggled = toggled;
        activationCount = activation;
        rotation = rot;
        isVisible = visible;
    }
}

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

public class TileBehaviour : BaseTile
{
    [SerializeField] private BehaviourManager behaviourManager;
    
    [Header("Scriptable Object Data")] [SerializeField]
    private List<SOTileData> allDataAssets; // 모든 스크립터블 오브젝트가 포함돼있는 리스트

    [SerializeField] private SOTileData tileData; // ScriptableObject로 타일 데이터 관리

    [Header("Individual Overrides")] [SerializeField]
    private bool overrideStats = false;

    [SerializeField] private int overrideMaxActivationCount = -1;
    [SerializeField] private int overrideBreakHitCount = 2;
    [SerializeField] private float overrideBreakDelay = 0.5f;
    [SerializeField] private TileColor overrideColor = TileColor.White;
    [SerializeField] private int overrideToggleActivationCount = 2;

    // --- 데이터 값 결정 로직 (Property) ---
    private int CurrentMaxActivationCount => overrideStats
        ? overrideMaxActivationCount
        : (tileData ? tileData.baseMaxActivationCount : maxActivationCount);

    private int CurrentBreakHitCount =>
        overrideStats ? overrideBreakHitCount : (tileData ? tileData.baseBreakHitCount : breakHitCount);

    private float CurrentBreakDelay =>
        overrideStats ? overrideBreakDelay : (tileData ? tileData.baseBreakDelay : breakDelay);

    private TileColor CurrentTileColor =>
        overrideStats ? overrideColor : (tileData ? tileData.baseColor : TileColor.White);

    private int CurrentToggleActivationCount => overrideStats
        ? overrideToggleActivationCount
        : (tileData ? tileData.baseToggleActivationCount : toggleActivationCount);

    [Header("Tile Settings")] [SerializeField]
    private TileType manualTileType;

    public TileType currentTileType => tileData != null ? tileData.tileType : manualTileType; // 외부에서 읽기 전용으로 접근

    [SerializeField] private Sprite[] tileSprites;

    // ColorTile일 경우 검정색일 때 버튼이 사라지는 문제 해결
    [Header("Renderers")] [SerializeField] private SpriteRenderer bgRenderer; // 하얀 배경용 (항상 흰색)
    [SerializeField] private SpriteRenderer iconRenderer; // 가운데 버튼/아이콘용 (색상 변경)

    [Header("Activation & Stats")] [SerializeField]
    private int maxActivationCount = -1;

    private int _currentActivationCount = 0;
    private bool _isWaitExit = false;
    private bool _isPlayerOnMe = false;

    private bool _isEnemyOnMe = false; // 적이 현재 타일 위에 있는지 여부 체크
    private EnemyBehaviour _currentEnemyOnMe; // 현재 타일 위의 적 참조

    [Header("SFX & Visuals")] private AudioSource _effectSound;

    [SerializeField] private AudioClip toggleSound;
    [SerializeField] private AudioClip rotationSound;
    [SerializeField] private AudioClip crackSound;
    [SerializeField] private AudioClip breakSound;

    [Header("Breakable")] [SerializeField] private Sprite[] breakableSprites;
    [SerializeField] private int breakHitCount = 2;
    [SerializeField] private float breakDelay = 0.5f;

    private int _currentHit = 0;

    [Header("Toggle")] [SerializeField] private bool isToggled = false;
    public bool IsToggled => isToggled;
    
    [SerializeField] private Sprite toggleOffSprite;

    [SerializeField] private int toggleActivationCount = 2;
    [SerializeField] private PlayerBehaviour player; // stackCount 참조용

    private Collider2D _collider;

    #region Undo/Redo
    public TileStateSnapshot GetSnapShot()
    {
        return new TileStateSnapshot(
            _currentHit,
            isToggled,
            _currentActivationCount,
            transform.rotation,
            iconRenderer.enabled
        );
    }
    
    public void RestoreSnapshot(TileStateSnapshot snapshot)
    {
        _currentHit = snapshot.hitCount;
        isToggled = snapshot.isToggled;
        _currentActivationCount = snapshot.activationCount;

        // rotation은 회전 타일(RotationTile)일 때만 복원합니다.
        // 일반 타일에 rotation을 복원하면 맵 전체 회전(mapPivot)과 충돌하여
        // Undo 시 타일이 애매한 각도로 꺾이는 버그가 발생합니다.
        if (IsRotationTile())
        {
            transform.rotation = snapshot.rotation;
        }
        
        // 비주얼 복구
        iconRenderer.enabled = snapshot.isVisible;
        bgRenderer.enabled = snapshot.isVisible;
        _collider.enabled = snapshot.isVisible;
        
        UpdateVisuals(true);
        UpdateCountText(snapshot.hitCount);
    }
    
    public void ApplyAction(PlayerBehaviour pb = null, EnemyBehaviour eb = null)
    {
        _isWaitExit = true;
        _currentActivationCount++;

        // 맵 회전 혹은 미끄러짐 로직일 때
        if (IsRotationTile() || currentTileType == TileType.Ice || currentTileType == TileType.Stop)
        {
            Transform target = pb != null ? pb.transform : (eb != null ? eb.transform : null);
            if (target != null)
            {
                target.position = new Vector3(transform.position.x, transform.position.y, target.position.z);
            }
        }

        if (IsReactiveTile())
        {
            isToggled = !isToggled;
        }
        
        // 타일 로직 실행
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
                if (teleportTarget && pb != null) pb.TeleportTo(teleportTarget.transform.position);
                break;

            case TileType.EndTeleport: break;

            case TileType.Breakable:
                _currentHit++;
                UpdateSprite();
                if (crackSound) _effectSound.PlayOneShot(crackSound);
                break;

            case TileType.Ice:
                if (pb !=null) pb.EnableIceMode(true);
                if (eb !=null) eb.EnableIceMode(true);
                break;

            case TileType.Stop:
                if (pb != null)
                {
                    // StopAllCoroutines() 대신 StopIceAndFinish()를 사용합니다.
                    // StopAllCoroutines()는 StageClear 등 무관한 코루틴까지 죽여서
                    // 클리어 이벤트가 영원히 발생하지 않는 버그가 있었습니다.
                    pb.StopIceAndFinish(); // 슬라이드 중단 + PlayerActionFinished 발동
                }
                if (eb != null)
                {
                    // 적은 Ice 전용 코루틴만 정리합니다.
                    eb.EnableIceMode(false);
                }
                break;

            case TileType.FirstDestination:
                if (pb != null && pb.IsFirstTile() && !GameManager.Instance.isCleared)
                {
                    GameManager.Instance.isCleared = true;
                    pb.ReachedDestination();
                }
                break;

            case TileType.SecondDestination:
                if (pb != null && !pb.IsFirstTile() && !GameManager.Instance.isCleared)
                {
                    GameManager.Instance.isCleared = true;
                    pb.ReachedDestination();
                }
                break;

            case TileType.StepOnToggle:
                GameEvents.RaiseToggleTriggered(-1); // -1일 경우 
                if (toggleSound) _effectSound.PlayOneShot(toggleSound);
                break;

            case TileType.ToggleTargeted:
            case TileType.ActiveToggle:
            case TileType.MoveToggle:
            case TileType.RotationToggle:
                if (isToggled)
                {
                    if (_isPlayerOnMe && player != null)
                    {
                        player.PlayExplosion();
                    }
                    if (_isEnemyOnMe && _currentEnemyOnMe != null)
                    {
                        _currentEnemyOnMe.PlayExplosion();
                    }
                }
                break;
            case TileType.TrapToggle:
                if (!isToggled)
                {
                    if (_isPlayerOnMe && player != null)
                    {
                        player.PlayExplosion();
                    }
                    if (_isEnemyOnMe && _currentEnemyOnMe != null)
                    {
                        _currentEnemyOnMe.PlayExplosion();
                    }
                }
                break;
            case TileType.ColorToggle:
                GameEvents.RaiseColorToggleTriggered(CurrentTileColor);
                break;

            case TileType.ConditionalToggle:
                break;
        }

        // isToggled는 ApplyAction 상단에서 이미 한 번만 뒤집힙니다.
        // 이전: 하단에 동일한 토글 코드가 중복되어 결과적으로 상태가 원래대로 돌아오는 버그 있었음
        
        UpdateVisuals(false); // 애니메이션 및 콜라이더 갱신
    }

    #endregion

    #region Text

    [SerializeField] private TMP_Text countText;

    public void UpdateCountText(int count)
    {
        if (countText == null) return;

        // 1. 카운트 타일인 경우 (남은 횟수 표시)
        if (IsCountableTile())
        {
            // currentCount가 0일 때도 정상적으로 초기 수치가 나오도록 계산
            int safeCount = Mathf.Max(0, count);
            int remaining = CurrentToggleActivationCount - (safeCount % CurrentToggleActivationCount);
            countText.text = remaining.ToString();
        }
        // 2. 텔레포트 타일인 경우 (ID 표시)
        else if (currentTileType == TileType.StartTeleport || currentTileType == TileType.EndTeleport)
        {
            int id = CurrentTeleportID;
            countText.text = id > 0 ? id.ToString() : "";
        }
        // 3. 그 외
        else
        {
            countText.text = "";
        }
    }

    #endregion

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

    public bool IsCountableTile() // 수치 특정이 되는 타일
    {
        return currentTileType == TileType.ActiveToggle
               // || currentTileType == TileType.ToggleTargeted 
               // || rrentTileType == TileType.StepOnToggle
               || currentTileType == TileType.MoveToggle
               || currentTileType == TileType.RotationToggle
               // || currentTileType == TileType.ColorToggle
               // || currentTileType == TileType.TrapToggle
            ;
    }

    [Header("Animations")] [SerializeField]
    private Animator animator;

    private Color GetUnityColor(TileColor tileColor)
    {
        return tileColor switch
        {
            TileColor.Black => new Color(50 / 255f, 50 / 255f, 50 / 255f, 1f),
            TileColor.Blue => Color.blue,
            TileColor.Green => Color.green,
            TileColor.Red => Color.red,
            TileColor.Yellow => Color.yellow,
            TileColor.Cyan => Color.cyan,
            TileColor.Magenta => Color.magenta,
            TileColor.White => Color.white,
            _ => Color.white
        };
    }

    #region Teleport Logic

    [Header("Teleport")] [SerializeField] private TileBehaviour teleportTarget;
    [SerializeField] private int overrideTeleportID = 0;

    private int CurrentTeleportID => overrideStats ? overrideTeleportID : (tileData ? tileData.baseTeleportID : 0);

    private void AutoLinkTeleport()
    {
        int myID = CurrentTeleportID;
        if (myID == 0)
        {
            if (teleportTarget != null)
            {
                teleportTarget = null;
                UnityEditor.EditorUtility.SetDirty(this);
            }

            return;
        }

        TileType targetType = (currentTileType == TileType.StartTeleport)
            ? TileType.EndTeleport
            : TileType.StartTeleport;

        // 맵상의 모든 TileBehaviour를 탐색 (FindObjectsSortMode는 성능을 위해 None)
        TileBehaviour[] allTiles = FindObjectsByType<TileBehaviour>(FindObjectsSortMode.None);
        TileBehaviour foundTarget = null;

        foreach (var tile in allTiles)
        {
            if (tile == this) continue; // 자기 자신 제외

            // 타입이 EndTeleport이고 ID가 일치하는지 확인
            if (tile.currentTileType == targetType && tile.CurrentTeleportID == myID)
            {
                foundTarget = tile;
                break;
            }
        }

        // --- Fat Finger 디버깅 로직 ---
        if (teleportTarget != foundTarget)
        {
            teleportTarget = foundTarget;
            UnityEditor.EditorUtility.SetDirty(this); //에디터에서 변경사항 저장 허용
        }
    }

    #endregion

    private Tilemap _tilemap;

    [SerializeField] private MapManager mapManager;

    private void Awake()
    {
        behaviourManager = FindObjectOfType<BehaviourManager>();
        
        _tilemap = GetComponentInParent<Tilemap>();
        _effectSound = GetComponentInParent<AudioSource>();

        if (animator == null) animator = GetComponent<Animator>();
        animator.enabled = IsReactiveTile();

        if (player == null) player = FindObjectOfType<PlayerBehaviour>();

        // Teleport타일일 경우, SOTileData 내부의 teleportID가 동일한 Teleport 타일을 자동으로 탐색
        if (currentTileType == TileType.StartTeleport || currentTileType == TileType.EndTeleport)
        {
            AutoLinkTeleport();
        }
        mapManager = FindObjectOfType<MapManager>();

        // isToggled와 콜라이더 동기화
        _collider = GetComponent<Collider2D>();
        if (IsReactiveTile())
        {
            //None 타일의 isToggled 초기값이 false이고
            //collider2D는 평소에 켜져있어야 하므로 !isToggled
            _collider.enabled = !isToggled;
        }
        else
        {
            _collider.enabled = true;
        }
    }

    private void Start()
    {
        // 0을 전달해서 HandleToggle에도 0값 전달한 뒤 초기 UI 세팅
        UpdateCountText(0);

        // 게임 시작 시 이미 함정 위에 서있는 경우 판정
        // (OnTriggerEnter는 씬 시작 시 발생하지 않으므로 Start에서 직접 체크)
        CheckOccupantsAfterToggle();
    }


#if UNITY_EDITOR
    private void OnValidate()
    {
        if (currentTileType == TileType.StartTeleport || currentTileType == TileType.EndTeleport)
        {
            AutoLinkTeleport();
        }

        if (!overrideStats && tileData != null)
        {
            // 체크가 꺼져 있으면 SO의 값을 인스펙터 변수에 동기화 (미리보기 기능)
            overrideMaxActivationCount = tileData.baseMaxActivationCount;
            overrideBreakHitCount = tileData.baseBreakHitCount;
            overrideBreakDelay = tileData.baseBreakDelay;
            overrideColor = tileData.baseColor;
            overrideToggleActivationCount = tileData.baseToggleActivationCount;
            overrideTeleportID = tileData.baseTeleportID;
        }

        // 1. 자동 데이터 할당 로직 (기본 유지)
        if (allDataAssets != null && allDataAssets.Count > 0)
        {
            SOTileData matchedData = allDataAssets.Find(data => data != null && data.tileType == manualTileType);
            if (matchedData != null && tileData != matchedData)
            {
                tileData = matchedData;
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }

        // 2. [핵심] SO의 base 값과 현재 인스펙터의 override 값을 대조
        // 아직 overrideStats가 꺼져 있을 때, 하나라도 값이 다르면 자동으로 켭니다.
        if (tileData != null && !overrideStats)
        {
            bool ipbodified =
                overrideMaxActivationCount != tileData.baseMaxActivationCount ||
                overrideBreakHitCount != tileData.baseBreakHitCount ||
                !Mathf.Approximately(overrideBreakDelay, tileData.baseBreakDelay) || // float 비교
                overrideColor != tileData.baseColor ||
                overrideToggleActivationCount != tileData.baseToggleActivationCount ||
                overrideTeleportID != tileData.baseTeleportID;

            if (ipbodified)
            {
                overrideStats = true;
                // 에디터에서 변경사항을 인지하도록 Dirty 설정
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }

        UpdateCountText(0);

        // 3. 비주얼 즉시 갱신 (애니메이션 프레임 고정 포함)
        UpdateVisuals(true);
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
            case TileType.TrapToggle:
                GameEvents.ToggleTriggered += HandleToggle;
                break;

            case TileType.MoveToggle:
                GameEvents.PlayerMoved += HandleToggle;
                break;

            case TileType.RotationToggle:
                GameEvents.PlayerRotated += HandleToggle;
                break;
        }
        
        GameEvents.TileIconRotated += RotateTileIcon;

    }

    private void OnDisable()
    {
        GameEvents.ColorToggleTriggered -= HandleColorToggle;
        GameEvents.ToggleTriggered -= HandleToggle;
        GameEvents.PlayerActed -= HandleToggle;
        GameEvents.ToggleTriggered -= HandleToggle;
        GameEvents.PlayerMoved -= HandleToggle;
        GameEvents.PlayerRotated -= HandleToggle;
        
        GameEvents.TileIconRotated -= RotateTileIcon;
    }

    private void HandleColorToggle(TileColor color)
    {
        if (currentTileType == TileType.ColorToggle) return; // 스위치는 신호를 받아도 변하지 않음

        if ((CurrentTileColor & color) != 0) // 비트 플래그 검사
        {
            isToggled = !isToggled;
            UpdateVisuals(false);

            // 일반 Toggle일 때
            if (isToggled && IsReactiveTile())
            {
                if (_isPlayerOnMe && player != null)
                {
                    player.PlayExplosion();
                }

                if (_isEnemyOnMe && _currentEnemyOnMe != null)
                {
                    _currentEnemyOnMe.PlayExplosion();
                }
            
            }
        }
    }

    private void HandleToggle(int currentCount)
    {
        // Undo/Redo 중에는 새 TileCommand를 생성하지 않습니다.
        // Redo 시 PopNonPlayerCommands가 기존 TileCommand를 재실행하므로
        // 여기서 추가 생성하면 중복 실행이 됩니다.
        if (player != null && player.isUndoRedo) return;

        if (currentCount == -1 ||
            (currentCount > 0 && currentCount % CurrentToggleActivationCount == 0))
        {
            // 현재 타일의 스냅샷을 기록함
            behaviourManager.ExecuteCommand(new TileCommand(this));

            // 토글 후 위에 서있는 캐릭터 즉시 폭발 판정
            CheckOccupantsAfterToggle();
        }

        // 타일 변화 후 UI 갱신
        UpdateCountText(currentCount);
    }

    private void CheckOccupantsAfterToggle()
    {
        // TrapToggle: isToggled=false(함정 활성) 일 때 위험
        if (currentTileType == TileType.TrapToggle)
        {
            if (!isToggled)
            {
                if (_isPlayerOnMe && player != null) player.PlayExplosion();
                if (_isEnemyOnMe && _currentEnemyOnMe != null) _currentEnemyOnMe.PlayExplosion();
            }
            return;
        }

        // 그 외 ReactiveTile: isToggled=true(활성) 일 때 위험
        if (IsReactiveTile() && isToggled)
        {
            if (_isPlayerOnMe && player != null) player.PlayExplosion();
            if (_isEnemyOnMe && _currentEnemyOnMe != null) _currentEnemyOnMe.PlayExplosion();
        }
    }
    
    // 애니메이션 추가로 생성된 코드
    private void UpdateVisuals(bool toggle = false)
    {
        UpdateSprite();

        // 애니메이션을 사용하는 타일일 경우
        if (animator != null && animator.isActiveAndEnabled)
        {
            if (IsReactiveTile())
            {
                string stateName = currentTileType.ToString();

                if (!isToggled)
                {
                    stateName = "Reverse" + stateName;
                }

                // Transition 없이 즉시 해당 애니메이션의 0초 지점으로 이동
                float startTime = toggle ? 1f : 0f;
                animator.Play(stateName, 0, startTime);
            }
        }
        
        if (_collider == null)
        {
            _collider = GetComponent<Collider2D>();
        }

        // IsReactiveTile인 경우에만 isToggled 상태에 따라 콜라이더를 제어합니다.
        // TrapToggle은 항상 콜라이더가 켜져 있어야 합니다 (함정 감지를 위해).
        // 그 외 일반 타일은 콜라이더를 항상 유지합니다.
        if (currentTileType == TileType.TrapToggle)
        {
            _collider.enabled = true;
        }
        else if (IsReactiveTile())
        {
            // isToggled=true(문 열림) → 콜라이더 OFF (통과 가능)
            // isToggled=false(문 닫힘) → 콜라이더 ON (막혀 있음)
            _collider.enabled = !isToggled;
        }
        else
        {
            _collider.enabled = true;
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
            if (_currentHit == 0)
            {
                nextIcon = tileSprites[(int)TileType.Breakable];
            }
            else if (breakableSprites != null && breakableSprites.Length > 0)
            {
                // _currentHit이 1일 때 breakableSprites[0]이 나오도록 인덱스 조정 (-1)
                int breakIdx = Mathf.Clamp(_currentHit - 1, 0, breakableSprites.Length - 1);
                nextIcon = breakableSprites[breakIdx];
            }
        } // 원래 텔레포트 ID 별로 스프라이트를 별개로 만들려고 했는데, 현재는 안 쓰는 코드
        /*else if (currentTileType == TileType.StartTeleport || currentTileType == TileType.EndTeleport)
        {
            int id = CurrentTeleportID;
            int spriteIdx = (int)currentTileType + (id > 0 ? id : 0);
            nextIcon = (spriteIdx < tileSprites.Length) ? tileSprites[spriteIdx] : tileSprites[(int)currentTileType];
        }*/
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

    protected override void OnPlayerEnter(PlayerBehaviour playerBehaviour)
    {
        _isPlayerOnMe = true;
        
        // Undo/Redo 중에는 타일 밟기 로직 무시
        if (playerBehaviour.isUndoRedo || _isWaitExit) return;

        if (CurrentMaxActivationCount != -1 && _currentActivationCount >= CurrentMaxActivationCount) return;
        
        TileCommand command = new TileCommand(this, pb : playerBehaviour);
        behaviourManager.ExecuteCommand(command);

        // 타일 물리 반응이 끝난 뒤 토글 이벤트 발생
        GameEvents.RaisePlayerMoved(playerBehaviour.moveCount);
        GameEvents.RaisePlayerActed(playerBehaviour.totalActionCount);

        // PlayerActionFinished는 PlayerBehaviour.MovePlayer()의 Invoke(RaiseActionFinished, 0.15f)에서 발생합니다.
        // 타일이 여러 개 겹쳐도 1번만 발생하도록 타일이 아닌 플레이어 쪽에서 관리합니다.
    }

    protected override void OnEnemyEnter(EnemyBehaviour enemy)
{
    _isEnemyOnMe = true;
    _currentEnemyOnMe = enemy;
    
    if (player.isUndoRedo || _isWaitExit || enemy.IsDead) return;

    // 적에게 반응하는 타일만 허용 (화이트리스트)
    switch (currentTileType)
    {
        case TileType.Ice:
        case TileType.Stop:
        case TileType.ToggleTargeted:
        case TileType.TrapToggle:
        case TileType.ActiveToggle:
        case TileType.MoveToggle:
        case TileType.RotationToggle:
            break; // 통과
        default:
            return; // 나머지는 무시
    }
    
    behaviourManager.ExecuteCommand(new TileCommand(this, eb: enemy));
}

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            _isPlayerOnMe = false;

            if (!mapManager.IsRotating)
            {
                _isWaitExit = false;
            }
            
            // 맵 전환 등으로 타일이 비활성화된 상태에서 Exit 이벤트가 뒤늦게 발생할 수 있으므로
            // activeInHierarchy를 확인한 뒤 코루틴을 시작합니다.
            if (currentTileType == TileType.Breakable && _currentHit >= CurrentBreakHitCount
                && gameObject.activeInHierarchy)
            {
                StartCoroutine(BreakTile());
            }
        }
        else if (other.CompareTag("Enemy"))
        {
            _isEnemyOnMe = false;
            _currentEnemyOnMe = null;
        }
    }

    private bool IsRotationTile()
    {
        return currentTileType == TileType.HalfClockwiseRotation ||
               currentTileType == TileType.HalfCounterClockwiseRotation ||
               currentTileType == TileType.QuarterClockwiseRotation ||
               currentTileType == TileType.QuarterCounterClockwiseRotation;
    }

    private void RotateTile(float angle)
    {
        if (player.isUndoRedo || mapManager.IsRotating) return;
        
        // 현재 플레이어가 밟고 있는 타일의 월드 좌표를 넘겨줌
        GameEvents.RaiseTileMapRotated(_tilemap.WorldToCell(transform.position), angle);
    }

    private IEnumerator BreakTile()
    {
        yield return new WaitForSeconds(CurrentBreakDelay);
            
        if (player.isUndoRedo) yield break;

        // 이미 Undo로 _currentHit이 복원됐을 수 있으므로 체크 추가
        if (_currentHit < CurrentBreakHitCount) yield break;
            
        // 맵에서 타일을 없애는 대신 시각/물리적으로만 비활성화 (Undo 가능하게)
        iconRenderer.enabled = false;
        bgRenderer.enabled = false;
        _collider.enabled = false;
            
        if (breakSound) _effectSound.PlayOneShot(breakSound);
    }
    
    private void RotateTileIcon(float angle)
    {
        transform.DORotate(new Vector3(0,0,angle),
            0.5f, RotateMode.LocalAxisAdd)
            .SetEase(Ease.OutBounce);
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }
}