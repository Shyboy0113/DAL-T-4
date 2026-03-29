using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using DG.Tweening;

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
    Help
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
    [SerializeField] private BehaviourManager          behaviourManager;
    [SerializeField] private PlayerUndoStateBridge undoState;

    [Header("Scriptable Object Data")]
    [SerializeField] private List<SO_TileData> allDataAssets;
    [SerializeField] private SO_TileData       tileData;

    [Header("Individual Overrides")]
    [SerializeField] private OverridableInt   maxActivationCount;
    [SerializeField] private OverridableInt   breakHitCount;
    [SerializeField] private OverridableFloat breakDelay;
    [SerializeField] private OverridableInt   toggleActivationCount;

    [SerializeField] private TileColor overrideColor      = TileColor.White;
    [SerializeField] private int       overrideTeleportID = 0;

    private int   CurrentMaxActivationCount    => maxActivationCount.GetValue(tileData ? tileData.baseMaxActivationCount : -1);
    private int   CurrentBreakHitCount         => breakHitCount.GetValue(tileData ? tileData.baseBreakHitCount : 2);
    private float CurrentBreakDelay            => breakDelay.GetValue(tileData ? tileData.baseBreakDelay : 0.5f);
    private int   CurrentToggleActivationCount => toggleActivationCount.GetValue(tileData ? tileData.baseToggleActivationCount : 2);

    private TileColor CurrentTileColor =>
        overrideColor != TileColor.White ? overrideColor : (tileData ? tileData.baseColor : TileColor.White);
    private int CurrentTeleportID =>
        overrideTeleportID != 0 ? overrideTeleportID : (tileData ? tileData.baseTeleportID : 0);

    [Header("Tile Settings")]
    [SerializeField] private TileType manualTileType;
    public TileType currentTileType => tileData != null ? tileData.tileType : manualTileType;

    [SerializeField] private Sprite[] tileSprites;

    [Header("Renderers")]
    [SerializeField] private SpriteRenderer backgroundRenderer;
    [SerializeField] private SpriteRenderer iconRenderer;

    [Header("Activation & Stats")]
    private bool _isWaitPlayerExit = false;
    private bool _isWaitEnemyExit  = false;
    private bool _isPlayerOnMe     = false;
    private bool _isEnemyOnMe      = false;
    private EnemyBehaviour _currentEnemyOnMe;

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

    [SerializeField] private Sprite          toggleOffSprite;
    [SerializeField] private PlayerBehaviour player;

    [Header("Animations")]
    [SerializeField] private Animator animator;

    [Header("Tilemap")]
    private Tilemap _tilemap;
    [SerializeField] private MapManager mapManager;

    private Collider2D _collider;

    private bool IsUndoOr => undoState != null && undoState.IsUndo;

    #region Undo/

    public TileStateSnapshot GetSnapShot()
    {
        return new TileStateSnapshot(
            _currentHit,
            isToggled,
            player ? player.moveCount        : 0,
            player ? player.rotationCount    : 0,
            player ? player.TotalActionCount : 0,
            transform.rotation,
            iconRenderer.enabled
        );
    }

    public void RestoreSnapshot(TileStateSnapshot snapshot)
    {
        _currentHit = snapshot.hitCount;
        isToggled   = snapshot.isToggled;

        if (IsRotationTile())
            transform.rotation = snapshot.rotation;

        iconRenderer.enabled       = snapshot.isVisible;
        backgroundRenderer.enabled = snapshot.isVisible;
        _collider.enabled          = snapshot.isVisible;

        _isWaitPlayerExit = false;
        _isWaitEnemyExit  = false;

        UpdateVisuals(true);

        int snapshotCount = currentTileType switch
        {
            TileType.MoveToggle     => snapshot.playerMoveCount,
            TileType.RotationToggle => snapshot.playerRotationCount,
            TileType.ActiveToggle   => snapshot.playerTotalActionCount,
            _                       => 0
        };

        UpdateCountText(snapshotCount);
    }

    public void ApplyTileCommand(PlayerBehaviour pb = null, EnemyBehaviour eb = null)
    {
        if (pb != null) _isWaitPlayerExit = true;
        if (eb != null) _isWaitEnemyExit  = true;

        if (IsRotationTile() || currentTileType == TileType.Ice || currentTileType == TileType.Stop)
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
                if (teleportTarget && pb != null) pb.TeleportTo(teleportTarget.transform.position);
                break;

            case TileType.EndTeleport: break;

            case TileType.Breakable:
                _currentHit++;
                UpdateSprite();
                if (crackSound) _effectSound.PlayOneShot(crackSound);
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
                GameEvents.RaiseToggleTriggered(-1);
                if (toggleSound) _effectSound.PlayOneShot(toggleSound);
                break;

            case TileType.ToggleTargeted:
            case TileType.ActiveToggle:
            case TileType.MoveToggle:
            case TileType.RotationToggle:
                isToggled = !isToggled;
                if (isToggled)
                {
                    if (_isPlayerOnMe && player != null)            player.PlayExplosion();
                    if (_isEnemyOnMe  && _currentEnemyOnMe != null) _currentEnemyOnMe.PlayExplosion();
                }
                break;

            case TileType.TrapToggle:
                isToggled = !isToggled;
                return;

            case TileType.ColorToggle:
                GameEvents.RaiseColorToggleTriggered(CurrentTileColor);
                break;

            case TileType.ConditionalToggle:
                break;
        }

        UpdateVisuals(false);
    }

    #endregion

    #region Text

    [SerializeField] private TMP_Text countText;

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
        }
        else if (currentTileType == TileType.StartTeleport || currentTileType == TileType.EndTeleport)
        {
            countText.text = CurrentTeleportID > 0 ? CurrentTeleportID.ToString() : "";
        }

        if (countText.text != targetText)
        {
            countText.text = targetText;
            
            countText.transform.DOKill();
            countText.transform.localScale = Vector3.one;

            countText.transform.DOPunchPosition(new Vector3(0, 10f, 0), 0.3f, 10, 1);
            countText.transform.DOPunchScale(new Vector3(0.2f, 0.2f, 0.2f), 0.3f, 10, 1);
        }
        
    }

    #endregion

    #region Type Helpers

    public bool IsPlayerActionTile() =>
        currentTileType == TileType.ActiveToggle   ||
        currentTileType == TileType.MoveToggle     ||
        currentTileType == TileType.RotationToggle;

    public bool IsStepOnTile() =>
        currentTileType == TileType.ToggleTargeted ||
        currentTileType == TileType.TrapToggle;

    public bool IsAnimationTile() =>
        currentTileType == TileType.ToggleTargeted  ||
        currentTileType == TileType.ActiveToggle    ||
        currentTileType == TileType.MoveToggle      ||
        currentTileType == TileType.RotationToggle  ||
        currentTileType == TileType.TrapToggle;

    public bool IsCountableTile() =>
        currentTileType == TileType.ActiveToggle   ||
        currentTileType == TileType.MoveToggle     ||
        currentTileType == TileType.RotationToggle;

    private bool IsRotationTile() =>
        currentTileType == TileType.HalfClockwiseRotation         ||
        currentTileType == TileType.HalfCounterClockwiseRotation   ||
        currentTileType == TileType.QuarterClockwiseRotation       ||
        currentTileType == TileType.QuarterCounterClockwiseRotation;

    #endregion

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
        behaviourManager = FindObjectOfType<BehaviourManager>();
        undoState    = FindObjectOfType<PlayerUndoStateBridge>();

        _tilemap     = GetComponentInParent<Tilemap>();
        _effectSound = GetComponentInParent<AudioSource>();

        if (animator == null) animator = GetComponent<Animator>();
        animator.enabled = IsAnimationTile();

        if (player == null) player = FindObjectOfType<PlayerBehaviour>();

        if (currentTileType == TileType.StartTeleport || currentTileType == TileType.EndTeleport)
            AutoLinkTeleport();

        mapManager = FindObjectOfType<MapManager>();

        _collider = GetComponent<Collider2D>();
        if (IsPlayerActionTile() || currentTileType == TileType.ToggleTargeted)
            _collider.enabled = !isToggled;
        else if (currentTileType == TileType.TrapToggle)
            _collider.enabled = true;
        else
            _collider.enabled = true;
    }

    private void Start()
    {
        UpdateCountText(0);
        CheckOccupantsAfterToggle();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (currentTileType == TileType.StartTeleport || currentTileType == TileType.EndTeleport)
            AutoLinkTeleport();

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

        UpdateCountText(0);
        UpdateVisuals(true);
    }
#endif

    private void OnEnable()
    {
        GameEvents.ColorToggleTriggered += HandleColorToggle;

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
        GameEvents.AfterMapRotated += OnAfterMapRotated;
    }

    private void OnDisable()
    {
        GameEvents.ColorToggleTriggered -= HandleColorToggle;
        GameEvents.ToggleTriggered      -= HandleToggle;
        GameEvents.PlayerActed          -= HandleToggle;
        GameEvents.PlayerMoved          -= HandleToggle;
        GameEvents.PlayerRotated        -= HandleToggle;
        GameEvents.TileIconRotated      -= RotateTileIcon;
        GameEvents.AfterMapRotated      -= OnAfterMapRotated;
    }

    private void HandleColorToggle(TileColor color)
    {
        if (currentTileType == TileType.ColorToggle) return;

        if ((CurrentTileColor & color) != 0)
        {
            behaviourManager.ExecuteCommand(new TileCommand(this));
            UpdateVisuals(false);

            if (isToggled && IsAnimationTile())
            {
                if (_isPlayerOnMe && player != null)            player.PlayExplosion();
                if (_isEnemyOnMe  && _currentEnemyOnMe != null) _currentEnemyOnMe.PlayExplosion();
            }
        }
    }

    private void HandleToggle(int currentCount)
    {
        UpdateCountText(currentCount);

        // Undo 중에는 새 TileCommand를 생성하지 않습니다.
        if (IsUndoOr)
        {
            if (IsCountableTile())
            {
                bool shouldBeToggled = currentCount > 0 &&
                    (currentCount / CurrentToggleActivationCount) % 2 != 0;
                if (isToggled != shouldBeToggled)
                {
                    isToggled = shouldBeToggled;
                    UpdateVisuals(true);
                }
            }
            return;
        }

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
                if (_isPlayerOnMe && player != null)            player.PlayExplosion();
                if (_isEnemyOnMe  && _currentEnemyOnMe != null) _currentEnemyOnMe.PlayExplosion();
            }
            return;
        }

        if ((IsPlayerActionTile() || currentTileType == TileType.ToggleTargeted) && isToggled)
        {
            if (_isPlayerOnMe && player != null)            player.PlayExplosion();
            if (_isEnemyOnMe  && _currentEnemyOnMe != null) _currentEnemyOnMe.PlayExplosion();
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
        backgroundRenderer.color  = Color.white;

        iconRenderer.color = (currentTileType == TileType.ColorToggle || IsPlayerActionTile())
            ? GetUnityColor(CurrentTileColor)
            : Color.white;

        Sprite nextIcon = null;

        if (currentTileType == TileType.Breakable && breakableSprites?.Length > 0)
        {
            nextIcon = _currentHit == 0
                ? tileSprites[(int)TileType.Breakable]
                : breakableSprites[Mathf.Clamp(_currentHit - 1, 0, breakableSprites.Length - 1)];
        }
        else if (currentTileType == TileType.ToggleTargeted)
        {
            nextIcon = isToggled ? tileSprites[(int)currentTileType] : toggleOffSprite;
        }
        else
        {
            nextIcon = tileSprites[(int)currentTileType];
        }

        iconRenderer.sprite = nextIcon;
    }

    protected override void OnPlayerEnter(PlayerBehaviour pb)
    {
        _isPlayerOnMe = true;

        if (IsUndoOr || _isWaitPlayerExit) return;

        if (currentTileType == TileType.TrapToggle)
        {
            if (!isToggled) pb.PlayExplosion();
            return;
        }

        if (IsAnimationTile())
        {
            CheckOccupantsAfterToggle();
            return;
        }

        behaviourManager.ExecuteCommand(new TileCommand(this, pb: pb));
    }

    protected override void OnEnemyEnter(EnemyBehaviour enemy)
    {
        _isEnemyOnMe      = true;
        _currentEnemyOnMe = enemy;

        if (IsUndoOr || _isWaitEnemyExit || enemy.IsDead) return;

        if (currentTileType == TileType.TrapToggle)
        {
            if (!isToggled) enemy.PlayExplosion();
            return;
        }

        // 적에게 반응하는 타일만 허용 (Ice, Stop)
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
                _isWaitPlayerExit = false;

            if (currentTileType == TileType.Breakable &&
                _currentHit >= CurrentBreakHitCount   &&
                gameObject.activeInHierarchy)
            {
                StartCoroutine(BreakTile());
            }
        }
        else if (other.CompareTag("Enemy"))
        {
            _isEnemyOnMe      = false;
            _currentEnemyOnMe = null;

            if (!mapManager.IsRotating)
                _isWaitEnemyExit = false;
        }
    }

    private void RotateTile(float angle)
    {
        if (IsUndoOr || mapManager.IsRotating) return;
        GameEvents.RaiseTileMapRotated(player, angle);
    }

    private IEnumerator BreakTile()
    {
        yield return new WaitForSeconds(CurrentBreakDelay);

        if (IsUndoOr) yield break;
        if (_currentHit < CurrentBreakHitCount) yield break;

        iconRenderer.enabled       = false;
        backgroundRenderer.enabled = false;
        _collider.enabled          = false;

        if (breakSound) _effectSound.PlayOneShot(breakSound);
    }

    private void RotateTileIcon(float angle)
    {
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
}