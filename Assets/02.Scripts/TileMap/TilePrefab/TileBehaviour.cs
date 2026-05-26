using System;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using DG.Tweening;
using UnityEngine.VFX;

#region Struct/Enum

public enum TileType
{
    None,
    QuarterClockwiseRotation,
    HalfClockwiseRotation,
    QuarterCounterClockwiseRotation,
    HalfCounterClockwiseRotation,
    StartTeleport,
    EndTeleport,
    Breakable,
    Ice,
    Stop,
    FirstDestination,
    SecondDestination,
    StepOn,
    ToggleTargeted,
    TrapToggle,
    ActiveToggle,
    MoveToggle,
    RotationToggle,
    ColorToggle,
    ConditionalToggle,
    Help,
    Star,
    Start,
    FirstEnemySpawn,
    SecondEnemySpawn
}

[System.Flags]
public enum TileColor
{
    Black   = 0,
    Blue    = 1 << 0,
    Green   = 1 << 1,
    Red     = 1 << 2,
    Yellow  = Red | Green,
    Cyan    = Green | Blue,
    Magenta = Red | Blue,
    White   = Red | Green | Blue
}
#endregion

public class TileBehaviour : BaseTile
{
    [SerializeField] private BehaviourManager behaviourManager;
    [SerializeField] private PlayerUndoStateBridge undoState;

    [Header("Scriptable Object Data")] [SerializeField]
    private List<SO_TileData> allDataAssets;

    [SerializeField] private SO_TileData tileData;

    [Header("Breakable Tile Data")] [SerializeField]
    private OverridableInt maxBreakCount;

    [SerializeField] private OverridableInt breakHitCount;
    [SerializeField] private OverridableFloat breakDelay;

    [Header("Toggle Tile Data")] [SerializeField]
    private OverridableInt toggleActivationCount;

    [Header("Color Tile Data")] [SerializeField]
    private TileColor overrideColor = TileColor.White;

    [SerializeField] private int overrideTeleportID = 0;

    private int CurrentMaxBreakCount => maxBreakCount.GetValue(tileData ? tileData.baseMaxBreakCount : -1);
    private int CurrentBreakHitCount => breakHitCount.GetValue(tileData ? tileData.baseBreakHitCount : 2);
    private float CurrentBreakDelay => breakDelay.GetValue(tileData ? tileData.baseBreakDelay : 0.5f);

    private int CurrentToggleActivationCount =>
        toggleActivationCount.GetValue(tileData ? tileData.baseToggleActivationCount : 2);

    private TileColor CurrentTileColor =>
        overrideColor != TileColor.White ? overrideColor : (tileData ? tileData.baseColor : TileColor.White);

    private int CurrentTeleportID =>
        overrideTeleportID != 0 ? overrideTeleportID : (tileData ? tileData.baseTeleportID : 0);

    [Header("Tile Settings")] [SerializeField]
    private TileType manualTileType;

    public TileType currentTileType => tileData != null ? tileData.tileType : manualTileType;

    // Star 타일이 수집되었는지 여부 (미션 달성 판정에서 사용)
    public bool IsCollected => currentTileType == TileType.Star && !iconRenderer.enabled;

    [SerializeField] private Sprite[] tileSprites;

    [Header("Renderers")] [SerializeField] private SpriteRenderer backgroundRenderer;
    [SerializeField] private SpriteRenderer iconRenderer;

    [Header("Activation & Stats")] private bool _isWaitPlayerExit = false;
    private bool _isWaitEnemyExit = false;
    private bool _isPlayerOnMe = false;
    private bool _isEnemyOnMe = false;
    private EnemyBehaviour _currentEnemyOnMe;

    // OnTriggerEnter2D에서 등록 후, 타일 로직 턴에서 처리할 pending 참조
    private PlayerBehaviour _pendingPlayer = null;
    private EnemyBehaviour _pendingEnemy = null;

    [Header("SFX & Visuals")] private AudioSource _effectSound;
    [SerializeField] private AudioClip toggleSound;
    [SerializeField] private AudioClip rotationSound;
    [SerializeField] private AudioClip crackSound;
    [SerializeField] private AudioClip breakSound;
    [SerializeField] private AudioClip starSound;

    [Header("Breakable")] [SerializeField] private Sprite[] breakableSprites;
    private int _currentHit = 0;
    private Coroutine _shakeCoroutine;
    private bool _isShaking = false;

    [Header("Toggle")] [SerializeField] private bool isToggled = false;
    public bool IsToggled => isToggled;

    // 그 외 Toggle의 On 상태의 까만 Sprite
    [SerializeField] private Sprite toggleOnSprite;
    [SerializeField] private Sprite trapToggleOnSprite;
    [SerializeField] private PlayerBehaviour player;

    [Header("Animations")] [SerializeField]
    private Animator animator;

    [Header("Tilemap")] private Tilemap _tilemap;
    [SerializeField] private MapManager mapManager;

    private Collider2D _collider;

    
    // Breaktile 관련 변수
    private Coroutine _breakCoroutine;

    private bool IsUndoOr => undoState != null && undoState.IsUndo;

    #region Undo/

    public TileStateSnapshot GetSnapShot()
    {
        return new TileStateSnapshot(
            _currentHit,
            isToggled,
            player.IsMap1Layer(),
            player ? player.gameObject.layer : 0,
            player ? player.map1MoveCount : 0,
            player ? player.map1RotationCount : 0,
            player ? player.map1ActionCount : 0,
            player ? player.map2MoveCount : 0,
            player ? player.map2RotationCount : 0,
            player ? player.map2ActionCount : 0,
            player ? player.TotalActionCount : 0,
            transform.rotation,
            iconRenderer.enabled,
            _isShaking,
            transform.localPosition
        );
    }

    public void RestoreSnapshot(TileStateSnapshot snapshot)
    {
        _currentHit = snapshot.hitCount;
        isToggled = snapshot.isToggled;

        if (player != null) player.gameObject.layer = snapshot.playerLayer;

        if (IsRotationTile())
            transform.rotation = snapshot.rotation;

        iconRenderer.enabled = snapshot.isVisible;
        backgroundRenderer.enabled = snapshot.isVisible;
        _collider.enabled = snapshot.isVisible;

        _isWaitPlayerExit = false;
        _isWaitEnemyExit = false;
        _pendingPlayer = null;
        _pendingEnemy = null;

        StopShake();
        // BreakTile Coroutine 명시적 취소
        if (_breakCoroutine != null)
        {
            StopCoroutine(_breakCoroutine);
            _breakCoroutine = null;
        }

        transform.localPosition = snapshot.localPosition;

        if (snapshot.isShaking && _shakeCoroutine == null)
            _shakeCoroutine = StartCoroutine(ShakeUntilBreak());

        UpdateVisuals(true);

        if (snapshot.playerIsMap1)
        {
            int snapshotCount = currentTileType switch
            {
                TileType.MoveToggle => snapshot.playerMap1MoveCount,
                TileType.RotationToggle => snapshot.playerMap1RotationCount,
                TileType.ActiveToggle => snapshot.playerMap1ActionCount,
                _ => 0
            };

            UpdateCountText(snapshotCount);
        }
        else
        {
            int snapshotCount = currentTileType switch
            {
                TileType.MoveToggle => snapshot.playerMap2MoveCount,
                TileType.RotationToggle => snapshot.playerMap2RotationCount,
                TileType.ActiveToggle => snapshot.playerMap2ActionCount,
                _ => 0
            };

            UpdateCountText(snapshotCount);
        }


    }

    public void ApplyTileCommand(PlayerBehaviour pb = null, EnemyBehaviour eb = null)
    {
        if (pb != null) _isWaitPlayerExit = true;
        if (eb != null) _isWaitEnemyExit = true;

        if (IsRotationTile() ||
            currentTileType == TileType.Ice ||
            currentTileType == TileType.Stop ||
            currentTileType == TileType.FirstDestination ||
            currentTileType == TileType.SecondDestination
            )
        {
            Transform target = pb != null ? pb.transform : (eb != null ? eb.transform : null);
            if (target != null)
                target.position = new Vector3(transform.position.x, transform.position.y, target.position.z);
        }

        switch (currentTileType)
        {
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
                if (teleportTarget && pb != null)
                {
                    bool wasOnIce = pb.IsOnIce();

                    // Map 1 <-> Map 2 전환인 경우에만 크로스맵으로 판정.
                    // Static 레이어는 양쪽 맵에 공유되므로 크로스맵 판정에서 제외한다.
                    int staticLayer = LayerMask.NameToLayer("Static");
                    bool startIsStatic = this.gameObject.layer == staticLayer;
                    bool endIsStatic   = teleportTarget.gameObject.layer == staticLayer;
                    bool isCrossMap    = !startIsStatic && !endIsStatic &&
                                        (this.gameObject.layer != teleportTarget.gameObject.layer);

                    if (isCrossMap)
                    {
                        pb.gameObject.layer = teleportTarget.gameObject.layer;
                        GameEvents.RaiseTileMapChanged();
                    }

                    // 플레이어 위치 이동 (이 시점에 active root는 이미 도착 맵)
                    pb.TeleportTo(teleportTarget.transform.position);

                    // Ice 슬라이딩 중 텔레포트: continueIceModeAfterTeleport 토글로 동작 분기
                    // false(기본): EndTeleport 도착 즉시 멈춤 (Stop 타일과 동일 효과)
                    // true       : EndTeleport 도착 후 같은 방향으로 Ice 슬라이딩 유지
                    if (wasOnIce && !continueIceModeAfterTeleport)
                        pb.StopIceAndFinish();
                }

                break;

            case TileType.EndTeleport: break;

            case TileType.Breakable:
                _currentHit++;
                UpdateSprite();
                if (crackSound) _effectSound.PlayOneShot(crackSound);
                if (_currentHit >= CurrentMaxBreakCount && _shakeCoroutine == null)
                    _shakeCoroutine = StartCoroutine(ShakeUntilBreak());
                break;

            case TileType.Ice:
                if (pb != null) pb.EnableIceMode(true);
                if (eb != null) eb.EnableIceMode(true);
                break;

            case TileType.Stop:
                if (pb != null) pb.StopIceAndFinish();
                if (eb != null) eb.EnableIceMode(false);
                break;

            case TileType.FirstDestination:
                if (pb != null && (pb.IsFirstTile() || LayerMask.LayerToName(gameObject.layer) == "Static") &&
                    !GameManager.Instance.isCleared)
                {
                    GameManager.Instance.isCleared = true;
                    pb.ReachedDestination();
                }

                break;

            case TileType.SecondDestination:
                if (pb != null && (!pb.IsFirstTile() || LayerMask.LayerToName(gameObject.layer) == "Static") &&
                    !GameManager.Instance.isCleared)
                {
                    GameManager.Instance.isCleared = true;
                    pb.ReachedDestination();
                }

                break;

            case TileType.StepOn:
                GameEvents.RaiseToggleTriggered(-1, gameObject.layer);
                if (toggleSound) _effectSound.PlayOneShot(toggleSound);
                break;

            case TileType.ToggleTargeted:
            case TileType.ActiveToggle:
            case TileType.MoveToggle:
            case TileType.RotationToggle:
                Debug.Log("토글되었습니다.");
                isToggled = !isToggled;
                if (isToggled)
                {
                    if (_isPlayerOnMe && player != null) player.PlayExplosion();
                    if (_isEnemyOnMe && _currentEnemyOnMe != null) _currentEnemyOnMe.PlayExplosion();
                }

                break;

            case TileType.TrapToggle:
                isToggled = !isToggled;
                break;

            case TileType.ColorToggle:
                GameEvents.RaiseColorToggleTriggered(CurrentTileColor, gameObject.layer);
                break;

            case TileType.ConditionalToggle:
                break;

            case TileType.Star:
                // 플레이어가 처음 밟았을 때만 수집 처리 (이미 수집된 타일 재발동 방지)
                if (pb != null && iconRenderer.enabled)
                {
                    iconRenderer.enabled = false;
                    GameEvents.RaiseStarCollected();
                    if (starSound) _effectSound.PlayOneShot(starSound);
                }

                break;
        }

        UpdateVisuals(false);
    }

    #endregion

    #region Text

    [SerializeField] private TMP_Text countText;

    private Vector3 _countTextOriginLocalPos;
    private bool _countTextOriginCached = false;
    
    
    public void UpdateCountText(int count)
    {
        if (countText == null) return;

        string targetText = "";

        if (IsCountableTile())
        {
            if (CurrentToggleActivationCount <= 0) return;
            int safeCount = Mathf.Max(0, count);
            int remaining = CurrentToggleActivationCount - (safeCount % CurrentToggleActivationCount);
            if (remaining == 0) remaining = CurrentToggleActivationCount;
            targetText = remaining.ToString();

            if (countText.text != targetText)
            {
                countText.text = targetText;

                countText.transform.DOKill();
                countText.transform.localScale = Vector3.one;
                if (_countTextOriginCached)
                    countText.transform.localPosition = _countTextOriginLocalPos;  // ← 위치도 명시적 리셋

                countText.transform.DOPunchPosition(new Vector3(0, 10f, 0), 0.3f, 10, 1);
                countText.transform.DOPunchScale(new Vector3(0.2f, 0.2f, 0.2f), 0.3f, 10, 1);
            }

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

    #region Type Helpers

    public bool IsPlayerActionTile() =>
        currentTileType == TileType.ActiveToggle ||
        currentTileType == TileType.MoveToggle ||
        currentTileType == TileType.RotationToggle;

    public bool IsStepOnTile() =>
        currentTileType == TileType.ToggleTargeted ||
        currentTileType == TileType.TrapToggle;

    public bool IsAnimationTile() =>
        currentTileType == TileType.ToggleTargeted ||
        currentTileType == TileType.ActiveToggle ||
        currentTileType == TileType.MoveToggle ||
        currentTileType == TileType.RotationToggle ||
        currentTileType == TileType.TrapToggle;

    public bool IsCountableTile() =>
        currentTileType == TileType.ActiveToggle ||
        currentTileType == TileType.MoveToggle ||
        currentTileType == TileType.RotationToggle;

    private bool IsRotationTile() =>
        currentTileType == TileType.HalfClockwiseRotation ||
        currentTileType == TileType.HalfCounterClockwiseRotation ||
        currentTileType == TileType.QuarterClockwiseRotation ||
        currentTileType == TileType.QuarterCounterClockwiseRotation;

    #endregion

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
            TileColor.Magenta => new Color(0.5f, 0f, 0.5f),
            TileColor.White => Color.white,
            _ => Color.white
        };
    }

    #region Layer Sync

    private void SyncLayerWithParent()
    {
        Tilemap parentTilemap = GetComponentInParent<Tilemap>();
        if (parentTilemap != null)
            gameObject.layer = parentTilemap.gameObject.layer;
    }

    #endregion

    #region Teleport Logic

    [Header("Teleport")] [SerializeField] private TileBehaviour teleportTarget;
    
    // 텔레포트에 할당되는 VFX
    [SerializeField] private VisualEffect startTeleportVFX;
    [SerializeField] private VisualEffect endTeleportVFX;
    
    /// <summary>
    /// true  : Ice 슬라이딩 중 텔레포트 → EndTeleport 도착 후 Ice 유지 (계속 미끄러짐)
    /// false : Ice 슬라이딩 중 텔레포트 → EndTeleport 도착 후 Ice 종료 (Stop 타일과 동일)
    /// </summary>
    [SerializeField] private bool continueIceModeAfterTeleport = false;

    [SerializeField] private bool overrideContinueIceModeAfterTeleport = false;

    // StageLoader에서 스테이지를 호출할 때, SO_StageData에 있는 continueIceModeAfterTeleport 변수 값을 반영시킴
    public void SetcontinueIceModeAfterTeleport(bool setting)
    {
        if (currentTileType == TileType.StartTeleport || currentTileType == TileType.EndTeleport)
        {
            if (overrideContinueIceModeAfterTeleport) return;

            continueIceModeAfterTeleport = setting;
        }
    }

    private void AutoLinkTeleport()
    {
        int myID = CurrentTeleportID;
        if (myID == 0)
        {
            if (teleportTarget != null)
            {
                teleportTarget = null;

#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }

            return;
        }

        TileType targetType = (currentTileType == TileType.StartTeleport)
            ? TileType.EndTeleport
            : TileType.StartTeleport;

        TileBehaviour[] allTiles = FindObjectsByType<TileBehaviour>(FindObjectsSortMode.None);
        TileBehaviour foundTarget = null;

        foreach (var tile in allTiles)
        {
            if (tile == this) continue;
            if (tile.currentTileType == targetType && tile.CurrentTeleportID == myID)
            {
                foundTarget = tile;
                break;
            }
        }

        if (teleportTarget != foundTarget)
        {
            teleportTarget = foundTarget;

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
    }

    #endregion

    private void Awake()
    {
        SyncLayerWithParent();

        if (behaviourManager == null) behaviourManager = FindFirstObjectByType<BehaviourManager>();
        if (undoState == null) undoState = FindFirstObjectByType<PlayerUndoStateBridge>();

        _tilemap = GetComponentInParent<Tilemap>();
        _effectSound = GetComponentInParent<AudioSource>();

        if (currentTileType == TileType.Breakable)
            _currentHit = CurrentBreakHitCount;

        if (animator == null) animator = GetComponent<Animator>();
        animator.enabled = IsAnimationTile();

        if (player == null) player = FindFirstObjectByType<PlayerBehaviour>();
        if (mapManager == null) mapManager = FindFirstObjectByType<MapManager>();

        if (currentTileType == TileType.StartTeleport)
        {
            endTeleportVFX.Stop();
            startTeleportVFX.Play();
            
            AutoLinkTeleport();
        }
        else if (currentTileType == TileType.EndTeleport)
        {
            startTeleportVFX.Stop();
            endTeleportVFX.Play();
            
            AutoLinkTeleport();
        }
        else
        {
            startTeleportVFX.Stop();
            endTeleportVFX.Stop();
        }
        
        _collider = GetComponent<Collider2D>();
        if (IsPlayerActionTile() || currentTileType == TileType.ToggleTargeted)
            _collider.enabled = !isToggled;
        else if (currentTileType == TileType.TrapToggle)
            _collider.enabled = true;
        else
            _collider.enabled = true;
        
        if (countText != null)
        {
            _countTextOriginLocalPos = countText.transform.localPosition;
            _countTextOriginCached = true;
        }
    }

    private void Start()
    {
        UpdateCountText(0);
        UpdateVisuals(true);
        CheckOccupantsAfterToggle();
        
        // Star 획득 도전과제 클리어 시, Star 비활성화
        CheckStarMissionCleared();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        SyncLayerWithParent();

        if (currentTileType == TileType.StartTeleport)
        {
            endTeleportVFX.Stop();
            startTeleportVFX.Play();
            
            AutoLinkTeleport();
        }
        else if (currentTileType == TileType.EndTeleport)
        {
            startTeleportVFX.Stop();
            endTeleportVFX.Play();
            AutoLinkTeleport();
        }
        else
        {
            startTeleportVFX.Stop();
            endTeleportVFX.Stop();
        }

        if (allDataAssets != null && allDataAssets.Count > 0)
        {
            SO_TileData matchedData = allDataAssets.Find(data => data != null && data.tileType == manualTileType);
            if (matchedData != null && tileData != matchedData)
            {
                tileData = matchedData;
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }
        }

        if (currentTileType == TileType.Breakable)
            _currentHit = CurrentBreakHitCount;

        UpdateCountText(0);
        UpdateVisuals(true);
    }
#endif

    private void OnEnable()
    {
        GameEvents.ColorToggleTriggered += HandleColorToggle;
        GameEvents.TileLogicTurnStarted += OnTileLogicTurn;
        GameEvents.StageLoaded += SetcontinueIceModeAfterTeleport;

        switch (currentTileType)
        {
            case TileType.ToggleTargeted:
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
        GameEvents.MapRotationCompleted += OnAfterMapRotated;

        // Ice 슬라이딩 중 자동 반응해야 하는 타일만 IceTileLogicTurnStarted를 구독
        if (currentTileType == TileType.Stop ||
            currentTileType == TileType.StartTeleport ||
            currentTileType == TileType.FirstDestination ||
            currentTileType == TileType.SecondDestination ||
            currentTileType == TileType.Star)
            GameEvents.IceTileLogicTurnStarted += OnIceTileLogicTurn;
    }

    private void OnDisable()
    {
        GameEvents.ColorToggleTriggered -= HandleColorToggle;
        GameEvents.TileLogicTurnStarted -= OnTileLogicTurn;
        GameEvents.IceTileLogicTurnStarted -= OnIceTileLogicTurn;
        GameEvents.ToggleTriggered -= HandleToggle;
        GameEvents.PlayerActed -= HandleToggle;
        GameEvents.PlayerMoved -= HandleToggle;
        GameEvents.PlayerRotated -= HandleToggle;
        GameEvents.TileIconRotated -= RotateTileIcon;
        GameEvents.MapRotationCompleted -= OnAfterMapRotated;
        GameEvents.StageLoaded -= SetcontinueIceModeAfterTeleport;
    }

    private void HandleColorToggle(TileColor color, int layer)
    {
        if (currentTileType != TileType.ToggleTargeted) return;
        bool isStaticLayer = gameObject.layer == LayerMask.NameToLayer("Static");
        if (gameObject.layer != layer && !isStaticLayer) return;
        
        bool shouldToggle = (CurrentTileColor & color) == CurrentTileColor;
        if (shouldToggle)
        {
            behaviourManager.ExecuteCommand(new TileCommand(this));

            if (isToggled && IsAnimationTile())
            {
                if (_isPlayerOnMe && player != null) player.PlayExplosion();
                if (_isEnemyOnMe && _currentEnemyOnMe != null) _currentEnemyOnMe.PlayExplosion();
            }
        }
    }

    private void HandleToggle(int currentCount, int layer)
    {
        bool isStaticLayer = gameObject.layer == LayerMask.NameToLayer("Static");
        if (gameObject.layer != layer && !isStaticLayer) return;

        UpdateCountText(currentCount);

        // Undo 중에는 새 TileCommand를 생성하지 않습니다.
        // isToggled 복원은 TileCommand.Undo() → RestoreSnapshot이 담당하므로 여기서 재계산하지 않습니다.
        if (IsUndoOr) return;

        if (currentCount == -1 ||
            (currentCount > 0 && currentCount % CurrentToggleActivationCount == 0))
        {
            behaviourManager.ExecuteCommand(new TileCommand(this));
        }
    }

    private void CheckOccupantsAfterToggle()
    {
        if (currentTileType == TileType.TrapToggle)
        {
            if (!isToggled)
            {
                if (_isPlayerOnMe && player != null) player.PlayExplosion();
                if (_isEnemyOnMe && _currentEnemyOnMe != null) _currentEnemyOnMe.PlayExplosion();
            }

            return;
        }

        if ((IsPlayerActionTile() || currentTileType == TileType.ToggleTargeted) && isToggled)
        {
            if (_isPlayerOnMe && player != null) player.PlayExplosion();
            if (_isEnemyOnMe && _currentEnemyOnMe != null) _currentEnemyOnMe.PlayExplosion();
        }
    }

    private void UpdateVisuals(bool toggle = false)
    {
        UpdateSprite();

        if (animator != null && animator.isActiveAndEnabled && IsAnimationTile())
        {
            string stateName = currentTileType.ToString();
            if (!isToggled) stateName = "Reverse" + stateName;

            float startTime = toggle ? 1f : 0f;
            animator.Play(stateName, 0, startTime);
        }

        if (_collider == null) _collider = GetComponent<Collider2D>();

        if (IsPlayerActionTile() || currentTileType == TileType.ToggleTargeted)
            _collider.enabled = !isToggled;
        else
            _collider.enabled = true;
    }

    private void UpdateSprite()
    {
        if (backgroundRenderer == null || iconRenderer == null) return;

        backgroundRenderer.sprite = tileSprites[0];
        backgroundRenderer.color = (currentTileType == TileType.ToggleTargeted)
            ? GetUnityColor(CurrentTileColor)
            : Color.white;

        iconRenderer.color = (currentTileType == TileType.ColorToggle || IsPlayerActionTile())
            ? GetUnityColor(CurrentTileColor)
            : Color.white;

        Sprite nextIcon = null;

        if (currentTileType == TileType.Breakable && breakableSprites?.Length > 0)
        {
            int remaining = CurrentMaxBreakCount - _currentHit;
            int spriteIndex = Mathf.Clamp(remaining - 1, 0, breakableSprites.Length - 1);
            nextIcon = breakableSprites[spriteIndex];
        }
        else if (currentTileType == TileType.ToggleTargeted || IsPlayerActionTile())
        {
            nextIcon = isToggled ? toggleOnSprite : tileSprites[(int)currentTileType];
        }
        else if (currentTileType == TileType.TrapToggle)
        {
            nextIcon = isToggled ? trapToggleOnSprite : tileSprites[(int)currentTileType];
        }
        else
        {
            nextIcon = tileSprites[(int)currentTileType];
        }

        iconRenderer.sprite = nextIcon;
    }

    // OnTriggerEnter2D: 점유 등록만 담당. 실제 로직은 타일 로직 턴에서 처리
    protected override void OnPlayerEnter(PlayerBehaviour pb)
    {
        bool isSameLayer = pb.gameObject.layer == gameObject.layer;
        bool isStaticLayer = gameObject.layer == LayerMask.NameToLayer("Static");

        // 플레이어가 속한 맵 레이어와 타일의 레이어가 다르면 무시 (크로스맵 오감지 방지)
        if (!isSameLayer && !isStaticLayer) return;
        _isPlayerOnMe = true;
        if (!IsUndoOr)
            _pendingPlayer = pb;
    }

    protected override void OnEnemyEnter(EnemyBehaviour eb)
    {
        bool isSameLayer = eb.gameObject.layer == gameObject.layer;
        bool isStaticLayer = gameObject.layer == LayerMask.NameToLayer("Static");

        if (!isSameLayer && !isStaticLayer) return;
        _isEnemyOnMe = true;
        _currentEnemyOnMe = eb;
        _pendingEnemy = eb;
    }

    // 타일 로직 턴: BehaviourManager가 시퀀스를 제어하며 발동
    private void OnTileLogicTurn()
    {
        var pb = _pendingPlayer;
        var eb = _pendingEnemy;
        _pendingPlayer = null;
        _pendingEnemy = null;

        // 플레이어 로직
        if (pb != null && !IsUndoOr && !_isWaitPlayerExit)
        {
            if (currentTileType == TileType.TrapToggle)
            {
                if (!isToggled) pb.PlayExplosion();
            }
            else if (IsAnimationTile())
            {
                CheckOccupantsAfterToggle();
            }
            else
            {
                behaviourManager.ExecuteCommand(new TileCommand(this, pb: pb));
            }
        }

        // 적 로직 (Ice, Stop, TrapToggle에만 반응)
        if (eb != null && !eb.IsDead && !IsUndoOr && !_isWaitEnemyExit)
        {
            if (currentTileType == TileType.TrapToggle)
            {
                if (!isToggled) eb.PlayExplosion();
            }
            else if (currentTileType == TileType.Ice || currentTileType == TileType.Stop)
            {
                behaviourManager.ExecuteCommand(new TileCommand(this, eb: eb));
            }
        }

        // 플레이어가 타일을 완전히 벗어난 후 한 턴이 지나야 wait 플래그를 해제한다.
        // OnTriggerExit2D에서 즉시 해제하면 같은 프레임 내 물리 jitter로 인한
        // 재진입 시 _pendingPlayer가 재설정되어 RotateTile 등이 연속 발동하는 버그가 있었음.
        if (!_isPlayerOnMe && !mapManager.IsRotating) _isWaitPlayerExit = false;
    }

    // Ice 슬라이딩 전용 타일 로직 턴 (Slide 코루틴에서 매 물리 스텝 후 발화)
    // Stop / StartTeleport 타일만 이 이벤트를 구독함
    private void OnIceTileLogicTurn()
    {
        if (_pendingPlayer == null || IsUndoOr || _isWaitPlayerExit) return;
        // 다른 타일이 먼저 처리되어 ice가 이미 종료됐을 경우 실행하지 않음
        if (!_pendingPlayer.IsOnIce()) return;

        var pb = _pendingPlayer;
        _pendingPlayer = null;
        behaviourManager.ExecuteCommand(new TileCommand(this, pb: pb));
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            _isPlayerOnMe = false;
            _pendingPlayer = null;

            if (currentTileType == TileType.Breakable &&
                _currentHit >= CurrentMaxBreakCount &&
                gameObject.activeInHierarchy)
            {
                if (_breakCoroutine != null) StopCoroutine(_breakCoroutine); // 중복 방지
                _breakCoroutine = StartCoroutine(BreakTile());
            }
        }
        else if (other.CompareTag("Enemy"))
        {
            _isEnemyOnMe = false;
            _currentEnemyOnMe = null;
            _pendingEnemy = null;

            if (!mapManager.IsRotating)
                _isWaitEnemyExit = false;
        }
    }

    private void RotateTile(float angle)
    {
        if (IsUndoOr || mapManager.IsRotating) return;
        GameEvents.RaiseTileMapRotated(player, angle);
    }



    private void RotateTileIcon(float angle, bool isFirst)
    {
        if (LayerMask.LayerToName(gameObject.layer) == "Static") return;

        // 자신이 속한 맵이 아니면 무시
        string myLayer = LayerMask.LayerToName(gameObject.layer);
        string targetLayer = isFirst ? "Map 1" : "Map 2";
        if (myLayer != targetLayer) return;

        transform.DORotate(new Vector3(0, 0, angle), 0.5f, RotateMode.LocalAxisAdd)
            .SetEase(Ease.OutBounce);

    }

    private void OnAfterMapRotated(bool freeze)
    {
        if (freeze) return;
        _isWaitEnemyExit = false;
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }

    #region BreakTile

    private IEnumerator BreakTile()
    {
        yield return new WaitForSeconds(CurrentBreakDelay);

        if (IsUndoOr)
        {
            _breakCoroutine = null;
            yield break;
        }

        if (_currentHit < CurrentMaxBreakCount)
        {
            _breakCoroutine = null;
            yield break;
        }

        StopShake();

        _breakCoroutine = null;

        // 타일이 파괴되는 상황도 커맨드로 등록
        behaviourManager.ExecuteCommand(new TileBreakCommand(this));
    }

    private IEnumerator ShakeUntilBreak()
    {
        _isShaking = true;
        while (_isShaking)
        {
            yield return transform
                .DOShakePosition(0.25f, new Vector3(0.06f, 0.06f, 0f), 30, 90f, false, true)
                .WaitForCompletion();
        }
    }

    private void StopShake()
    {
        if (_shakeCoroutine == null) return;
        _isShaking = false;
        StopCoroutine(_shakeCoroutine);
        _shakeCoroutine = null;
        transform.DOKill();
    }

    public void ApplyBreak()
    {
        iconRenderer.enabled = false;
        backgroundRenderer.enabled = false;
        _collider.enabled = false;
        if (breakSound) _effectSound.PlayOneShot(breakSound);
    }

    public void RevertBreak()
    {
        iconRenderer.enabled = true;
        backgroundRenderer.enabled = true;
        _collider.enabled = true;
    }

    #endregion

    #region Star Tile

    private void CheckStarMissionCleared()
    {
        if (currentTileType == TileType.Star)
        {
            var gm = GameManager.Instance;
            if (gm?.currentStageData != null && gm.currentProgressData != null)
            {
                var sd = gm.currentStageData;
                var pd = gm.currentProgressData;

                bool starMissionCleared =
                    (sd.firstMissionType  == MissionType.CollectStar && pd.isFirstMissionCleared) ||
                    (sd.secondMissionType == MissionType.CollectStar && pd.isSecondMissionCleared);

                if (starMissionCleared)
                {
                    iconRenderer.enabled = false;
                }
            }
        }
    }

#endregion
}