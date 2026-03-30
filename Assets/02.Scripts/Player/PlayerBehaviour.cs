using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public enum KeyType       { None = 0, Alt = 1, F4 = 2, Tab = 3 }
public enum PlayerDirection { Right, Down, Left, Up }

/// <summary>
/// 플레이어의 물리 이동, 방향 관리, 퍼즐 카운터, 시퀀스 UI를 담당합니다.
/// 입력 감지는 PlayerInputHandler, 애니메이션은 PlayerAnimator가 담당합니다.
/// </summary>
public class PlayerBehaviour : MonoBehaviour
{
    #region References
    [SerializeField] private BehaviourManager          behaviourManager;
    [SerializeField] private PlayerUndoStateBridge undoRedoState;
    [SerializeField] private PlayerAnimator            playerAnimator;
    [SerializeField] private MapManager                mapManager;
    [SerializeField] private SoundEffectPlayer         soundEffectPlayer;
    [SerializeField] private AudioClip                 triggerSound;
    [SerializeField] private AudioClip                 cancelSound;
    [SerializeField] private PlayerShadow              playerShadow;
    #endregion

    #region Sequence UI
    public event Action OnInputQueueChanged;
    private int _stack = 0;
    private const int MaxQueueSize = 3;
    private List<int> _inputQueue = new List<int>(new int[MaxQueueSize]);
    [SerializeField] private List<KeyType> _inputHistory = new List<KeyType>();

    public void UpdateSequenceCanvas(int amount)
    {
        _stack = Mathf.Clamp(_stack + amount, 0, MaxQueueSize);
        if (amount < 0 && _stack < _inputQueue.Count) _inputQueue[_stack] = 0;
        OnInputQueueChanged?.Invoke();
    }
    #endregion

    #region Physics
    private Rigidbody2D _rigidbody2D;
    private Collider2D  _collider2D;
    [SerializeField] private float forceAmount = 1f;

    public void StopVelocity() => _rigidbody2D.velocity = Vector2.zero;
    #endregion

    #region Input Lock
    [SerializeField] private bool _isInputLocked = false;
    [SerializeField] private bool _isEnemyActing = false;
    [SerializeField] private bool _isMapBusy     = false;

    private void SetInputLock(bool locked) => _isInputLocked = locked;

    private IEnumerator ISetInputLock(bool locked, float time)
    {
        yield return new WaitForSeconds(time);
        _isInputLocked = locked;
    }

    public bool CheckSkip() =>
        playerAnimator.IsRotating || _isInputLocked || _isMapBusy || _isEnemyActing;
    #endregion

    #region Puzzle Stats
    public int moveCount     = 0;
    public int rotationCount = 0;
    public int TotalActionCount => moveCount + rotationCount;

    public void CalculateMoveCount(int delta)     => moveCount     = Mathf.Max(0, moveCount     + delta);
    public void CalculateRotationCount(int delta) => rotationCount = Mathf.Max(0, rotationCount + delta);
    #endregion

    #region Direction
    private PlayerDirection _playerDirection = PlayerDirection.Right;

    public void UpdateDirection(int rotation) =>
        _playerDirection = (PlayerDirection)(((int)_playerDirection + rotation + 4) % 4);
    #endregion

    #region Last Move
    private Vector2 _lastMoveDirection;
    public Vector2 GetLastMoveDirection()          => _lastMoveDirection;
    public void    SetLastMoveDirection(Vector2 d) => _lastMoveDirection = d;
    #endregion

    #region Ice Mode
    [Header("Ice Mode")]
    [SerializeField] private float slideSpeed = 5f;
    private bool      _isOnIce        = false;
    private Coroutine _slideCoroutine = null;

    public bool IsOnIce() => _isOnIce;

    public void EnableIceMode(bool enable)
    {
        _isOnIce = enable;

        if (enable)
        {
            if (_slideCoroutine == null)
                _slideCoroutine = StartCoroutine(Slide(_lastMoveDirection));
        }
        else
        {
            if (_slideCoroutine != null)
            {
                StopCoroutine(_slideCoroutine);
                _slideCoroutine = null;
            }
            _rigidbody2D.velocity = Vector2.zero;
        }
    }

    /// Stop 타일 진입 시 호출 — 슬라이딩 중단 후 ActionFinished 발화
    public void StopIceAndFinish()
    {
        EnableIceMode(false);
        if (!undoRedoState.IsUndo)
            GameEvents.RaisePlayerActionFinished();
    }

    private IEnumerator Slide(Vector2 direction)
    {
        while (_isOnIce)
        {
            Vector2 nextPos = _rigidbody2D.position + direction * slideSpeed * Time.fixedDeltaTime;
            _rigidbody2D.MovePosition(nextPos);
            yield return new WaitForFixedUpdate();
            Physics2D.SyncTransforms();
            CheckForGround();

            if (GameManager.Instance.isGameOver || GameManager.Instance.isCleared)
            {
                _rigidbody2D.velocity = Vector2.zero;
                yield break;
            }
        }
    }
    #endregion

    #region Particle
    [SerializeField] private ParticleSystem particle;
    private bool _isTriggered = false;
    #endregion

    #region Fade
    [SerializeField] private CanvasGroup changePanelCanvasGroup;
    #endregion

    private IEnumerator WaitForGameManager()
    {
        yield return new WaitUntil(() => GameManager.Instance != null);
        GameManager.Instance.RegisterStackManager(this);
    }
    
    #region Lifecycle

    private void Awake()
    {
        StartCoroutine(WaitForGameManager());

        _rigidbody2D      = GetComponent<Rigidbody2D>();
        _collider2D       = GetComponent<Collider2D>();
        soundEffectPlayer = GetComponent<SoundEffectPlayer>();

        _isInputLocked = true;
        
    }

    private void OnEnable()
    {
        GameEvents.PlayerDied          += OnPlayerDied;
        GameEvents.StageCleared        += StopParticle;
        GameEvents.InputLockChanged    += SetInputLock;
        GameEvents.OnEnemyTurnStarted  += (_) => _isEnemyActing = true;
        GameEvents.OnPlayerTurnStarted += ()  => _isEnemyActing = false;
        GameEvents.BeforeMapRotated    += FreezePlayerPhysicalLogic;
        GameEvents.AfterMapRotated     += FreezePlayerPhysicalLogic;
    }

    private void OnDisable()
    {
        GameEvents.PlayerDied          -= OnPlayerDied;
        GameEvents.StageCleared        -= StopParticle;
        GameEvents.InputLockChanged    -= SetInputLock;
        GameEvents.BeforeMapRotated    -= FreezePlayerPhysicalLogic;
        GameEvents.AfterMapRotated     -= FreezePlayerPhysicalLogic;
    }

    private void Start()
    {
        particle.Stop();
        _isInputLocked = true;
    }

    private void Update()
    {
        if (CheckSkip()) return;
        if (GameManager.Instance.isGameOver || GameManager.Instance.isCleared) return;

        UpdateParticle();

        Debug.Log(TotalActionCount);
    }

    #endregion

    #region Movement

    public void MovePlayer()
    {
        Vector2 dir = _playerDirection switch
        {
            PlayerDirection.Right => Vector2.right,
            PlayerDirection.Down  => Vector2.down,
            PlayerDirection.Left  => Vector2.left,
            PlayerDirection.Up    => Vector2.up,
            _                     => Vector2.zero
        };

        _lastMoveDirection = dir;

        if (_isOnIce)
        {
            if (_slideCoroutine != null) StopCoroutine(_slideCoroutine);
            _slideCoroutine = StartCoroutine(Slide(dir));
        }
        else
        {
            _rigidbody2D.AddForce(dir * forceAmount, ForceMode2D.Impulse);
        }

        StartCoroutine(MoveSequence());
    }

    private IEnumerator MoveSequence()
    {
        SetInputLock(true);

        yield return new WaitForSeconds(0.1f);
        CheckForGround();

        yield return new WaitForSeconds(0.05f); // 총 0.15s 후
        // Ice 위에서는 Stop 타일 진입 시 StopIceAndFinish()가 ActionFinished를 발화합니다.
        // 일반 이동은 여기서 발화합니다.
        if (!undoRedoState.IsUndo && !_isOnIce)
            GameEvents.RaisePlayerActionFinished();

        yield return new WaitForSeconds(0.05f); // 총 0.2s 후
        SetInputLock(false);
    }

    public void RotateArrow(bool immediate = false)
    {
        playerAnimator.RotateArrow(_playerDirection, immediate, rotationCount);
        StartCoroutine(RotateGroundCheck());
    }

    private IEnumerator RotateGroundCheck()
    {
        yield return new WaitForSeconds(0.1f);
        CheckForGround();
    }

    #endregion

    #region Input Handling

    public bool HandleInput(KeyType keyType)
    {
        int slotInCycle = _inputHistory.Count % MaxQueueSize;

        if (slotInCycle == 0 && _inputHistory.Count > 0)
            ResetQueue();

        _inputHistory.Add(keyType);
        _inputQueue[slotInCycle] = (int)keyType;
        _stack = slotInCycle + 1;
        OnInputQueueChanged?.Invoke();

        if (CheckGameOver())
        {
            _collider2D.enabled = false;
            PlayExplosion();
            return false;
        }
        else if (CheckMapChange())
        {
            return true; // 맵 전환 발생 알림, 실제 전환은 PlayerInputHandler에서 커맨드로 처리
        }

        return false;
    }

    public void ChangePlayerTransform(Vector3 tilePosition)
    {
        Vector3 newPosition = transform.position;
        newPosition.z = tilePosition.z;
        transform.position = newPosition;
    }

    private IEnumerator DelayedGroundCheck(float delay)
    {
        yield return new WaitForSeconds(delay);
        CheckForGround();
    }

    public bool CheckGameOver() =>
        (_inputQueue[0] == (int)KeyType.Alt && _inputQueue[1] == (int)KeyType.F4) ||
        (_inputQueue[1] == (int)KeyType.Alt && _inputQueue[2] == (int)KeyType.F4);

    public bool CheckMapChange() =>
        GameManager.Instance.hasSecondMap &&
        ((_inputQueue[0] == (int)KeyType.Alt && _inputQueue[1] == (int)KeyType.Tab) ||
         (_inputQueue[1] == (int)KeyType.Alt && _inputQueue[2] == (int)KeyType.Tab));

    public int CheckInputQueue(int slot) => _inputQueue[slot];

    private void ResetQueue()
    {
        _inputQueue = new List<int>(new int[MaxQueueSize]);
        _stack = 0;
        OnInputQueueChanged?.Invoke();
    }

    #endregion

    #region Undo State

    public void UndoState()
    {
        GameManager.Instance.isGameOver = false;
        playerAnimator.PlayIdle();
        playerShadow?.Show();

        _collider2D.enabled   = true;
        _rigidbody2D.simulated = true;

        if (_inputHistory.Count > 0)
        {
            _inputHistory.RemoveAt(_inputHistory.Count - 1);
            RebuildUI();
        }

        Physics2D.SyncTransforms();
    }

    private void RebuildUI()
    {
        ResetQueue();

        int count  = _inputHistory.Count;
        int remain = count % MaxQueueSize;
        if (count == 0) return;

        int startIndex = remain == 0 ? count - MaxQueueSize : count - remain;

        for (int i = 0; i < MaxQueueSize; i++)
        {
            int idx = startIndex + i;
            if (idx >= count) break;
            _inputQueue[i] = (int)_inputHistory[idx];
            _stack++;
        }

        OnInputQueueChanged?.Invoke();
    }

    #endregion

    #region Ground & Explosion

    private void CheckForGround()
    {
        Physics2D.SyncTransforms();

        Transform activeRoot = mapManager.GetActiveMapRoot();
        if (activeRoot == null) { PlayExplosion(); return; }

        bool hasGround = false;
        foreach (var col in Physics2D.OverlapPointAll(transform.position))
        {
            if (col.transform.IsChildOf(activeRoot)) { hasGround = true; break; }
        }

        if (!hasGround) PlayExplosion();
    }

    public void PlayExplosion()
    {
        EnableIceMode(false);
        undoRedoState.Reset();
        SetInputLock(false);
        _rigidbody2D.velocity = Vector2.zero;
        playerAnimator.PlayExplosion();
        GameEvents.RaisePlayerDied();
        GameEvents.RaiseInputLockChanged(false);
    }

    private void OnPlayerDied()
    {
        StopParticle();
    }

    #endregion

    #region Particle

    private void UpdateParticle()
    {
        if (CheckBackTile())
        {
            var main = particle.main;
            main.startColor = mapManager.IsFirstRoot()
                ? new Color(144 / 255f, 57 / 255f, 205 / 255f)
                : Color.white;

            if (!particle.isPlaying)
            {
                if (!_isTriggered) soundEffectPlayer.PlaySoundEffect(triggerSound);
                particle.Play();
                _isTriggered = true;
            }
        }
        else
        {
            if (!particle.isStopped && _isTriggered)
            {
                soundEffectPlayer.PlaySoundEffect(cancelSound);
                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                _isTriggered = false;
            }
        }
    }

    private void StopParticle()
    {
        if (particle.isPlaying)
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        _isTriggered = false;
    }

    private bool CheckBackTile()
    {
        Transform inactiveRoot = mapManager.GetInactiveMapRoot();
        if (inactiveRoot == null) return false;

        foreach (var col in Physics2D.OverlapPointAll(transform.position))
        {
            if (col.transform.IsChildOf(inactiveRoot)) return true;
        }
        return false;
    }

    #endregion

    #region Physics Freeze

    public void FreezePlayerPhysicalLogic(bool freeze)
    {
        _isMapBusy = freeze;

        if (freeze)
        {
            _rigidbody2D.velocity        = Vector2.zero;
            _rigidbody2D.angularVelocity = 0f;
        }

        _collider2D.enabled    = !freeze;
        _rigidbody2D.simulated = !freeze;

        if (!freeze)
            StartCoroutine(DelayedGroundCheck(0.05f));
    }

    #endregion

    #region Stage

    public void ReachedDestination()
    {
        _isInputLocked = true;
        playerAnimator.PlayClear();
        StartCoroutine(StageClear(1.0f));
    }

    private IEnumerator StageClear(float time)
    {
        yield return new WaitForSeconds(time);
        GameEvents.RaiseStageCleared();
    }

    public void TeleportTo(Vector3 targetPosition)
    {
        transform.SetParent(null);

        _rigidbody2D.velocity        = Vector2.zero;
        _rigidbody2D.angularVelocity = 0f;
        _rigidbody2D.simulated       = false;

        transform.position = targetPosition;
        Physics2D.SyncTransforms();

        _rigidbody2D.simulated = true;
        CheckForGround();
        SetInputLock(false);
    }

    public bool IsFirstTile() => mapManager.IsFirstRoot();

    #endregion

    #region Init

    public void InitPlayer(Vector3? spawnPosition = null)
    {

        _isInputLocked = false;
        _isMapBusy = false;
        _isEnemyActing = false;

        _rigidbody2D.velocity = Vector2.zero;
        _collider2D.enabled   = true;
        _rigidbody2D.simulated = true;

        StopParticle();

        _isOnIce = false;
        if (_slideCoroutine != null)
        {
            StopCoroutine(_slideCoroutine);
            _slideCoroutine = null;
        }

        moveCount     = 0;
        rotationCount = 0;

        _inputHistory.Clear();
        ResetQueue();

        _playerDirection = PlayerDirection.Right;
        playerAnimator.PlayIdle();
        playerAnimator.RotateArrow(_playerDirection, immediate: true);

        transform.position = spawnPosition ?? new Vector3(0.5f, 0.5f, 0f);
        Physics2D.SyncTransforms();

        playerShadow?.Show();
        StartCoroutine(ISetInputLock(false, 1.0f));

    }

    #endregion
}