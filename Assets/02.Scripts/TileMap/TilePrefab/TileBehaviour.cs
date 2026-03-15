using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections; // 코루틴 사용
using System;
using System.Collections.Generic;
using TMPro;

using DG.Tweening;

#region Struct/Enum

[System.Serializable]
public struct TileStateSnapshot
{
    public int hitCount;
    public bool isToggled;
    public int playerMoveCount;
    public int playerRotationCount;
    public int playerTotalActionCount;
    public Quaternion rotation;
    public bool isVisible; // Breakable 타일의 파괴 여부 체크용

    public TileStateSnapshot(int hit, bool toggled, int moveCount, int rotationCount, int totalActionCount, Quaternion rot, bool visible)
    {
        hitCount = hit;
        isToggled = toggled;
        playerMoveCount = moveCount;
        playerRotationCount = rotationCount;
        playerTotalActionCount = totalActionCount;
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
    
    StepOn, // 플레이어가 '직접' 밟았을 때 ToggleTargeted 타일 토글 처리
    ToggleTargeted, // 토글되는 타일, 토글 상태일 때 밟으면 게임오버
    
    // 토글 판정
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
#endregion

public class TileBehaviour : BaseTile
{
    [SerializeField] private BehaviourManager behaviourManager;
    
    [Header("Scriptable Object Data")]
    [SerializeField] private List<SOTileData> allDataAssets; // 모든 스크립터블 오브젝트가 포함돼있는 리스트
    [SerializeField] private SOTileData tileData; // ScriptableObject로 타일 데이터 관리
    
    // --- 데이터 값 결정 로직 (Property) ---
    [Header("Individual Overrides")]
    [SerializeField] private OverridableInt maxActivationCount;
    [SerializeField] private OverridableInt breakHitCount;
    [SerializeField] private OverridableFloat breakDelay;
    [SerializeField] private OverridableInt toggleActivationCount;

    // TileColor와 TeleportID는 0/White를 기본값으로 간주하여 별도 처리
    [SerializeField] private TileColor overrideColor = TileColor.White;
    [SerializeField] private int overrideTeleportID = 0;
    
    private int CurrentMaxActivationCount => maxActivationCount.GetValue(tileData ? tileData.baseMaxActivationCount : -1);
    private int CurrentBreakHitCount      => breakHitCount.GetValue(tileData ? tileData.baseBreakHitCount : 2);
    private float CurrentBreakDelay       => breakDelay.GetValue(tileData ? tileData.baseBreakDelay : 0.5f);
    private int CurrentToggleActivationCount => toggleActivationCount.GetValue(tileData ? tileData.baseToggleActivationCount : 2);

    // TileColor, TeleportID는 override값이 기본값(White/0)이면 SO값 사용
    private TileColor CurrentTileColor =>
        overrideColor != TileColor.White ? overrideColor : (tileData ? tileData.baseColor : TileColor.White);
    private int CurrentTeleportID =>
        overrideTeleportID != 0 ? overrideTeleportID : (tileData ? tileData.baseTeleportID : 0);
    
    
    [Header("Tile Settings")]
    [SerializeField] private TileType manualTileType;

    public TileType currentTileType => tileData != null ? tileData.tileType : manualTileType; // 외부에서 읽기 전용으로 접근

    [SerializeField] private Sprite[] tileSprites;

    // ColorTile일 경우 검정색일 때 버튼이 사라지는 문제 해결
    [Header("Renderers")]
    [SerializeField] private SpriteRenderer backgroundRenderer; // 하얀 배경용 (항상 흰색)
    [SerializeField] private SpriteRenderer iconRenderer; // 가운데 버튼/아이콘용 (색상 변경)

    [Header("Activation & Stats")]
    private bool _isWaitPlayerExit = false;
    private bool _isWaitEnemyExit = false;
    private bool _isPlayerOnMe = false;
    private bool _isEnemyOnMe = false; // 적이 현재 타일 위에 있는지 여부 체크
    private EnemyBehaviour _currentEnemyOnMe; // 현재 타일 위의 적 참조

    [Header("SFX & Visuals")]
    private AudioSource _effectSound;
    [SerializeField] private AudioClip toggleSound;
    [SerializeField] private AudioClip rotationSound;
    [SerializeField] private AudioClip crackSound;
    [SerializeField] private AudioClip breakSound;

    [Header("Breakable")]
    [SerializeField] private Sprite[] breakableSprites;
    private int _currentHit = 0;

    [Header("Toggle")]
    [SerializeField] private bool isToggled = false;
    public bool IsToggled => isToggled;
    
    [SerializeField] private Sprite toggleOffSprite;
    [SerializeField] private PlayerBehaviour player; // stackCount 참조용
    
    [Header("Animations")]
    [SerializeField] private Animator animator;
    
    [Header("Tilemap")]
    private Tilemap _tilemap;
    [SerializeField] private MapManager mapManager;
    
    // 물리 효과
    private Collider2D _collider;

    #region Undo/Redo
    public TileStateSnapshot GetSnapShot()
    {
        return new TileStateSnapshot(
            _currentHit,
            isToggled,
            player ? player.moveCount : 0,
            player ? player.rotationCount : 0,
            player ? player.TotalActionCount : 0,
            transform.rotation,
            iconRenderer.enabled
        );
    }
    
    public void RestoreSnapshot(TileStateSnapshot snapshot)
    {
        _currentHit = snapshot.hitCount;
        isToggled = snapshot.isToggled;

        // rotation은 회전 타일(RotationTile)일 때만 복원합니다.
        // 일반 타일에 rotation을 복원하면 맵 전체 회전(mapPivot)과 충돌하여
        // Undo 시 타일이 애매한 각도로 꺾이는 버그가 발생합니다.
        if (IsRotationTile())
        {
            transform.rotation = snapshot.rotation;
        }
        
        // 비주얼 복구
        iconRenderer.enabled = snapshot.isVisible;
        backgroundRenderer.enabled = snapshot.isVisible;
        _collider.enabled = snapshot.isVisible;

        _isWaitPlayerExit = false;
        _isWaitEnemyExit = false;
        
        UpdateVisuals(true);

        int snapshotCount = currentTileType switch
        {
            TileType.MoveToggle => snapshot.playerMoveCount,
            TileType.RotationToggle => snapshot.playerRotationCount,
            TileType.ActiveToggle => snapshot.playerTotalActionCount,
            _ => 0

        };
        
        UpdateCountText(snapshotCount);
    }
    
    public void ApplyTileCommand(PlayerBehaviour pb = null, EnemyBehaviour eb = null)
    {
        if (pb != null) _isWaitPlayerExit = true;
        if (eb != null) _isWaitEnemyExit = true;

        // 맵 회전 혹은 미끄러짐 로직일 때
        if (IsRotationTile() || currentTileType == TileType.Ice || currentTileType == TileType.Stop)
        {
            Transform target = pb != null ? pb.transform : (eb != null ? eb.transform : null);
            if (target != null)
            {
                target.position = new Vector3(transform.position.x, transform.position.y, target.position.z);
            }
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

            case TileType.StepOn:
                GameEvents.RaiseToggleTriggered(-1); // -1일 경우 
                if (toggleSound) _effectSound.PlayOneShot(toggleSound);
                break;

            case TileType.ToggleTargeted:
            case TileType.ActiveToggle:
            case TileType.MoveToggle:
            case TileType.RotationToggle:
                isToggled = !isToggled;
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
                isToggled = !isToggled;
                return; // break대신 return이 호출돼야 맨 아래줄의 UpdateVisuals가 호출 안됨
            
            case TileType.ColorToggle:
                GameEvents.RaiseColorToggleTriggered(CurrentTileColor);
                break;

            case TileType.ConditionalToggle:
                break;
        }

        // isToggled는 ApplyTileCommand 상단에서 이미 한 번만 뒤집힙니다.
        // 이전: 하단에 동일한 토글 코드가 중복되어 결과적으로 상태가 원래대로 돌아오는 버그 있었음
        
        UpdateVisuals(false); // 애니메이션 및 콜라이더 갱신
    }

    #endregion

    #region Text

    [SerializeField] private TMP_Text countText;

    public void UpdateCountText(int count)
    {
        if (countText == null) return;

        if (IsCountableTile())
        {
            if (CurrentToggleActivationCount <= 0) return;
            int safeCount = Mathf.Max(0, count);
            int remaining = CurrentToggleActivationCount - (safeCount % CurrentToggleActivationCount);
            if (remaining == 0) remaining = CurrentToggleActivationCount;
            countText.text = remaining.ToString();
        }
        else if (currentTileType == TileType.StartTeleport || currentTileType == TileType.EndTeleport)
        {
            countText.text = CurrentTeleportID > 0 ? CurrentTeleportID.ToString() : "";
        }
        else
        {
            countText.text = "";
        }
    }

    #endregion

    public bool IsPlayerActionTile()
    {
        return currentTileType == TileType.ActiveToggle
               || currentTileType == TileType.MoveToggle
               || currentTileType == TileType.RotationToggle
            ;
    }
    
    public bool IsStepOnTile() //StepOn Toggle을 밟았을 때 작동하는 타일
    {
        return currentTileType == TileType.ToggleTargeted
               || currentTileType == TileType.TrapToggle
               // || currentTileType == TileType.ColorToggle
               ;
    }
    
    public bool IsAnimationTile()
    {
        return currentTileType == TileType.ToggleTargeted
            || currentTileType == TileType.ActiveToggle
            || currentTileType == TileType.MoveToggle
            || currentTileType == TileType.RotationToggle 
            || currentTileType == TileType.TrapToggle
            // || currentTileType == TileType.ColorToggle
            ;
    }

    public bool IsCountableTile() // 수치 특정이 되는 타일
    {
        return currentTileType == TileType.ActiveToggle
               || currentTileType == TileType.MoveToggle
               || currentTileType == TileType.RotationToggle
            ;
    }
    
    private bool IsRotationTile()
    {
        return currentTileType == TileType.HalfClockwiseRotation ||
               currentTileType == TileType.HalfCounterClockwiseRotation ||
               currentTileType == TileType.QuarterClockwiseRotation ||
               currentTileType == TileType.QuarterCounterClockwiseRotation;
    }


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

    [Header("Teleport")]
    [SerializeField] private TileBehaviour teleportTarget;

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

    private void Awake()
    {
        behaviourManager = FindObjectOfType<BehaviourManager>();
        
        _tilemap = GetComponentInParent<Tilemap>();
        _effectSound = GetComponentInParent<AudioSource>();

        if (animator == null) animator = GetComponent<Animator>();
        animator.enabled = IsAnimationTile();

        if (player == null) player = FindObjectOfType<PlayerBehaviour>();

        // Teleport타일일 경우, SOTileData 내부의 teleportID가 동일한 Teleport 타일을 자동으로 탐색
        if (currentTileType == TileType.StartTeleport || currentTileType == TileType.EndTeleport)
        {
            AutoLinkTeleport();
        }
        mapManager = FindObjectOfType<MapManager>();

        // isToggled와 콜라이더 동기화
        _collider = GetComponent<Collider2D>();
        if (IsPlayerActionTile() || currentTileType == TileType.ToggleTargeted)
        {
            //None 타일의 isToggled 초기값이 false이고
            //collider2D는 평소에 켜져있어야 하므로 !isToggled
            _collider.enabled = !isToggled;
        }
        else if (currentTileType == TileType.TrapToggle) // 함정 토글은 항상 활성화
        {
            _collider.enabled = true;
        }
        else
        {
            _collider.enabled = true; // 나머지 타일 활성화
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
            case TileType.TrapToggle:
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
        
        GameEvents.TileIconRotated += RotateTileIcon;
        // 맵 회전 후
        GameEvents.AfterMapRotated += OnAfterMapRotated;

    }

    private void OnDisable()
    {
        GameEvents.ColorToggleTriggered -= HandleColorToggle;
        GameEvents.ToggleTriggered -= HandleToggle;
        GameEvents.PlayerActed -= HandleToggle;
        GameEvents.PlayerMoved -= HandleToggle;
        GameEvents.PlayerRotated -= HandleToggle;
        GameEvents.TileIconRotated -= RotateTileIcon;
        // 맵 회전 후
        GameEvents.AfterMapRotated -= OnAfterMapRotated;
    }

    private void HandleColorToggle(TileColor color)
    {
        if (currentTileType == TileType.ColorToggle) return; // 스위치는 신호를 받아도 변하지 않음

        if ((CurrentTileColor & color) != 0) // 비트 플래그 검사
        {
            //isToggled = !isToggled;
            behaviourManager.ExecuteCommand(new TileCommand(this));
            UpdateVisuals(false);

            // 일반 Toggle일 때
            if (isToggled && IsAnimationTile())
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
        // 타일 변화 후 UI 갱신
        UpdateCountText(currentCount);
        
        // Undo/Redo 중에는 새 TileCommand를 생성하지 않습니다.
        // Redo 시 PopNonPlayerCommands가 기존 TileCommand를 재실행하므로
        // 여기서 추가 생성하면 중복 실행이 됩니다.
        if (player != null && player.isUndoRedo) //Undo/Redo 중일 때
        {
            if (IsCountableTile())
            {
                bool shouldBeToggled = (currentCount > 0 && (currentCount / CurrentToggleActivationCount) % 2 != 0);
                if (isToggled != shouldBeToggled) 
                {
                    isToggled = shouldBeToggled;
                    UpdateVisuals(true); // 비주얼 강제 동기화
                }
            }
            return;
        }
        
        if (currentCount == -1 || // currentCount == -1은 '무조건 작동' 신호
        (currentCount > 0 && currentCount % CurrentToggleActivationCount == 0))
        {
            // 현재 타일의 스냅샷을 기록함
            behaviourManager.ExecuteCommand(new TileCommand(this));
        }
        
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
        if ((IsPlayerActionTile() || currentTileType == TileType.ToggleTargeted)&& isToggled)
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
            if (IsAnimationTile())
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

        // IsPlayerActionTile인 경우에만 isToggled 상태에 따라 콜라이더를 제어합니다.
        // TrapToggle은 항상 콜라이더가 켜져 있어야 합니다 (함정 감지를 위해).
        // 그 외 일반 타일은 콜라이더를 항상 유지합니다.
        
        if (IsPlayerActionTile() || currentTileType == TileType.ToggleTargeted)
        {
            // isToggled=true(문 열림) → 콜라이더 OFF (통과 가능)
            // isToggled=false(문 닫힘) → 콜라이더 ON (막혀 있음)
            _collider.enabled = !isToggled;
        }
        else if (currentTileType == TileType.TrapToggle)
        {
            _collider.enabled = true;
        }
        else
        {
            _collider.enabled = true;
        }
        
    }

    private void UpdateSprite()
    {
        if (backgroundRenderer == null || iconRenderer == null) return;

        // 1. 공통 배경 설정
        backgroundRenderer.sprite = tileSprites[0]; // 0번을 기본 배경으로 사용
        backgroundRenderer.color = Color.white;

        // 2. 아이콘 색상 설정
        iconRenderer.color = (currentTileType == TileType.ColorToggle || IsPlayerActionTile())
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

    // 맵 회전 / 텔레포트 / 미끄러짐 / 클리어 / Breakable / StepOn 타일은 아래 함수에서 발동됨
    protected override void OnPlayerEnter(PlayerBehaviour playerBehaviour)
    {
        // 플레이어 행동 - 타일 로직 발동 - 타일 물리 발동
        // ActiveToggle, MoveToggle, RotationToggle은 HandleToggle로 처리해야 함
        
        _isPlayerOnMe = true;
        
        // Undo/Redo 중에는 타일 밟기 로직 무시
        if (playerBehaviour.isUndoRedo || _isWaitPlayerExit) return;

        // trapToggle을 밟았을 때, 플레이어 사망처리
        if (currentTileType == TileType.TrapToggle)
        {
            if (!isToggled) playerBehaviour.PlayExplosion();
            return;
        }
        
        // HandleToggle 이벤트로 처리되는 타일은 여기서 실행 금지
        if (IsAnimationTile())
        {
            CheckOccupantsAfterToggle(); // _isPlayerOnMe=true인 상태에서 체크
            return;
        }
        
        // 각 타일의 currentTileType에 맞는 커맨드를 생성 후, 
        TileCommand command = new TileCommand(this, pb : playerBehaviour);
        behaviourManager.ExecuteCommand(command);
    }

    protected override void OnEnemyEnter(EnemyBehaviour enemy)
{
    _isEnemyOnMe = true;
    _currentEnemyOnMe = enemy;
    
    if (player.isUndoRedo || _isWaitEnemyExit || enemy.IsDead) return;

    if (currentTileType == TileType.TrapToggle)
    {
        if( !isToggled) enemy.PlayExplosion();
        return;
    }
    
    // 적에게 반응하는 타일만 허용
    switch (currentTileType)
    {
        case TileType.Ice:
        case TileType.Stop:
            break;
        default:
            return;
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
                _isWaitPlayerExit = false;
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
            
            if (!mapManager.IsRotating)
            {
                _isWaitEnemyExit = false;
            }
        }
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
        backgroundRenderer.enabled = false;
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
    
    private void OnAfterMapRotated(bool freeze)
    {
        if (freeze) return; // false(해제)일 때만 처리
        _isWaitEnemyExit = false;
    }
}