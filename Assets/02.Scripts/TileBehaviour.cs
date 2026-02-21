using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections; // 코루틴 사용
using System; // [Flags] 사용

public enum TileType
{
    None,
    
    // 맵 회전 판정
    QuarterClockwiseRotation,
    HalfClockRotation,
    QuarterCounterClockwiseRotation,
    HalfCounterClockRotation,

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

public class TileBehaviour : BaseTile
{

    [Header("Scriptable Object Data")]
    [SerializeField] private SOTileData tileData; // ScriptableObject로 타일 데이터 관리

    // --- 데이터 값 결정 로직 (Property) ---
    private int CurrentMaxActivationCount => overrideStats ? overrideMaxActivationCount : (tileData ? tileData.baseMaxActivationCount : maxActivationCount);
    private int CurrentBreakHitCount => overrideStats ? overrideBreakHitCount : (tileData ? tileData.baseBreakHitCount : breakHitCount);
    private float CurrentBreakDelay => tileData ? tileData.baseBreakDelay : breakDelay;
    private TileColor CurrentTileColor => overrideStats ? overrideColor : (tileData ? tileData.baseColor : TileColor.White);
    private int CurrentToggleActivationCount => tileData ? tileData.baseToggleActivationCount : toggleActivationCount;


    [Header("Individual Overrides")]
    [SerializeField] private bool overrideStats = false;
    [SerializeField] private int overrideMaxActivationCount = -1;
    [SerializeField] private int overrideBreakHitCount = 2;
    [SerializeField] private TileColor overrideColor = TileColor.White;

    [Header("Tile Settings")]
    [SerializeField] private TileType manualTileType;
    public TileType currentTileType => tileData != null ? tileData.tileType : manualTileType; // 외부에서 읽기 전용으로 접근

    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] tileSprites;

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
    [SerializeField] private bool isToggled = true;
    [SerializeField] private Sprite toggleOffSprite;

    [SerializeField] private int toggleActivationCount = 2;
    [SerializeField] private StackManager player; // stackCount 참조용
    
    [Header("Teleport")]
    [SerializeField] private TileBehaviour teleportTarget;

    private Tilemap _tilemap;

    private void Awake()
    {
        _tilemap = GetComponentInParent<Tilemap>();
        _effectSound = GetComponentInParent<AudioSource>();
        
        if (player == null) player = FindObjectOfType<StackManager>();

        UpdateSprite();
    }

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

        UpdateSprite();

        if (GetComponent<Collider2D>() is Collider2D col) col.enabled = isToggled;
        if (currentTileType == TileType.RotationToggle && !isToggled && _isPlayerOnMe && player != null) player.PlayExplosion();
    }

    private void UpdateSprite()
    {
        if (spriteRenderer == null) return;
        if (currentTileType == TileType.ToggleTargeted || currentTileType == TileType.ColorToggle)
        {
            spriteRenderer.sprite = isToggled ? tileSprites[(int)currentTileType] : toggleOffSprite;
            return;
        }
        if (currentTileType == TileType.Breakable && breakableSprites?.Length > 0)
            spriteRenderer.sprite = breakableSprites[Mathf.Clamp(_currentHit, 0, breakableSprites.Length - 1)];
        else if (tileSprites != null && (int)currentTileType < tileSprites.Length)
            spriteRenderer.sprite = tileSprites[(int)currentTileType];        
    }


    protected override void OnPlayerEnter(StackManager player)
    {
        _isPlayerOnMe = true;
        if (CurrentMaxActivationCount != -1 && _currentActivationCount >= maxActivationCount) return;
        if (_isWaitExit) return;
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
            
            case TileType.HalfClockRotation:
            RotateTile(-180f);
            if (rotationSound) _effectSound.PlayOneShot(rotationSound);
            break;
            
            case TileType.QuarterCounterClockwiseRotation: 
            RotateTile(90f);
            if (rotationSound) _effectSound.PlayOneShot(rotationSound);
            break;

            case TileType.HalfCounterClockRotation: 
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
            player.ReachedDestination();
            break;

            case TileType.SecondDestination: 
            player.ReachedDestination();
            break;

            case TileType.StepOnToggle:
            GameEvents.RaiseToggleTriggered(-1); // -1일 경우 
            if (toggleSound) _effectSound.PlayOneShot(toggleSound);
            break;

            case TileType.ToggleTargeted:
            if (!isToggled) player.PlayExplosion();
            break;

            case TileType.ActiveToggle:
            break;

            case TileType.MoveToggle:
            break;            

            case TileType.RotationToggle: 
            if (!isToggled) player.PlayExplosion();
            break;

            case TileType.ColorToggle:
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
        return currentTileType == TileType.HalfClockRotation ||
               currentTileType == TileType.HalfCounterClockRotation ||
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