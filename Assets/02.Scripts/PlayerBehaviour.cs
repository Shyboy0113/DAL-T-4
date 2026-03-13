// Unity
using UnityEngine;

// System
using System; // Action
using System.Collections; // IEnumerator
using System.Collections.Generic; // List

//DoTween
using DG.Tweening;

public enum KeyType { None = 0, Alt = 1, F4 = 2, Tab = 3 } // 입력 큐에 저장할 키 타입을 정의하는 Enum

public enum PlayerDirection { Right, Down, Left, Up } //플레이어의 회전 방향을 Enum(문자)로 표시

public class PlayerBehaviour : MonoBehaviour
{
    #region Behaviour & References
    [SerializeField] private BehaviourManager behaviourManager;
    [SerializeField] private SoundEffectPlayer soundEffectPlayer;
    
    [SerializeField] private MapManager mapManager;
    #endregion
    
    #region Fields & Stats
    private PlayerDirection _playerDirection = PlayerDirection.Right;
    
    // UI 이벤트
    public event Action OnInputQueueChanged; // SequenceUI의 Update 함수 비용 줄이기
    private int _stack = 0;
    
    private const int MaxQueueSize = 3; // 최대로 쌓을 수 있는 큐 스택을 상수로 선언
    private List<int> _inputQueue = new List<int>(new int[MaxQueueSize]);
    [SerializeField] private List<KeyType> _inputHistory = new List<KeyType>();
    
    public void UpdateSequenceCanvas(int amount)
    {
        _stack = Mathf.Clamp(_stack + amount, 0, MaxQueueSize);
        if (amount < 0 && _stack < _inputQueue.Count) _inputQueue[_stack] = 0;
        OnInputQueueChanged?.Invoke();
    }
    
    [Header("Physical Effects")]
    private Rigidbody2D _rigidbody2D;
    [SerializeField] private float forceAmount = 1f;
    private Collider2D _collider2D;
    public GameObject arrow; //플레이어에 달려있는 회전 방향 표시용 화살표
    
    public void StopVelocity()
    {
        _rigidbody2D.velocity = Vector2.zero;
    }
    
    // Animator
    [SerializeField] private Animator _animator;

    //방향 전환 및 이동시 효과음 발동
    [SerializeField] private AudioClip rotateSound;
    [SerializeField] private AudioClip moveSound;
    [SerializeField] private AudioClip explosionSound;
    [SerializeField] private AudioClip triggerSound; // 2번째 맵에 발판이 있어 Alt + Tab 로직을 쓸 수 있는 경우
    [SerializeField] private AudioClip cancelSound; // Alt + Tab 트리거에서 벗어날 때

    [Header("Puzzle Stats")]
    public int moveCount = 0;
    public int rotationCount = 0;
    public int totalActionCount => moveCount + rotationCount; // 읽기 전용 속성
    
    public void CalculateMoveCount(int count)
    {
        moveCount = Mathf.Max(0, moveCount + count);
    }

    public void CalculateRotationCount(int count)
    {
        rotationCount = Mathf.Max(0, rotationCount + count);
    }
    
    #endregion
    
    #region DOTween & Effects
    public float DOTweenDuration;
    public Vector3 DOTweenPunch;
    public int DOTweenVibrato;

    //ALT + TAB trigger
    [SerializeField] private ParticleSystem particle; // 파티클 시스템
    private bool _isTriggerd = false; // Alt + Tab 트리거가 발동된 상태인지 추적하는 변수 (파티클과 효과음 제어용)
    #endregion

    #region InputLock
    private bool _isInputLocked = false;
    private bool _isRotating = false; // 회전중인지를 판단하는 bool 값
    private bool _isEnemyActing = false;
    private bool _isMapBusy = false;    
    private void SetInputLock(bool isLocked) => _isInputLocked = isLocked;
    public bool IsRotating() => _isRotating; // 외부에서 회전 중인지 확인할 수 있는 public 메서드
    
    #endregion
    
    #region FadePanel

    [SerializeField] private CanvasGroup changePanelCanvasGroup;
    
    IEnumerator FadeSwitchPanel(float time)
    {
        changePanelCanvasGroup.alpha = 1f; // 알파(불투명도) 1로 설정 후 즉시 반영
        changePanelCanvasGroup.DOFade(0f, time); // Fade Out
        yield return null;
    }

    #endregion

    #region IceMode
    private float _slideSpeed = 5f;
    private Vector2 _lastMoveDirection; // 마지막으로 움직인 방향을 기록
    public Vector2 GetLastMoveDirection() => _lastMoveDirection;
    public void SetLastMoveDirection(Vector2 dir) => _lastMoveDirection = dir;

    private bool _isOnIce = false;
    private Coroutine _slideCoroutine;

    public bool IsOnIce() => _isOnIce;
    public void EnableIceMode(bool enable)
    {
        _isOnIce = enable;

        if (enable)
        {
            // 얼음 타일에 '닿자마자' 마지막 이동 방향으로 미끄러짐을 시작합니다
            if (_slideCoroutine == null)
            {
                _slideCoroutine = StartCoroutine(Slide(_lastMoveDirection));
                SetInputLock(true); // 이동 중 입력 잠금
            }
        }
        else
        {
            // STOP 타일에 닿으면 미끄러짐을 즉시 멈추고 속도를 0으로 만듭니다
            if (_slideCoroutine != null)
            {
                StopCoroutine(_slideCoroutine);
                _slideCoroutine = null;
            }
            _rigidbody2D.velocity = Vector2.zero; // 타일 위에서 딱 멈추게 함
            SetInputLock(false); // Stop에 닿았을 경우 입력 가능해짐
        }
    }

    private IEnumerator Slide(Vector2 direction)
    {
        while (_isOnIce)
        {
            Vector2 nextPos = _rigidbody2D.position + (direction * _slideSpeed * Time.fixedDeltaTime);
            _rigidbody2D.MovePosition(nextPos);
            yield return new WaitForFixedUpdate();

            Physics2D.SyncTransforms(); // 물리 엔진에 변경된 트랜스폼 정보를 즉시 반영

            CheckForGround(); // 타일을 벗어났는지 확인

            // 4. 게임 오버(폭발) 혹은 클리어 상태가 되면 미끄러짐 루프를 즉시 탈출

            if (GameManager.Instance.isGameOver || GameManager.Instance.isCleared)
            {
                _rigidbody2D.velocity = Vector2.zero; // 물리적 움직임 완전 정지
                yield break; // 코루틴 종료
            }
        }
    }

    public void TeleportTo(Vector3 targetPosition)
    {
        // 1. 미끄러짐 코루틴 즉시 중단
        if (_slideCoroutine != null)
        {
            StopCoroutine(_slideCoroutine);
            _slideCoroutine = null;
        }

        // 2. [핵심] 맵 회전 등으로 인해 설정된 부모를 해제하여 월드 좌표 오차를 방지합니다.
        transform.SetParent(null);

        // 3. 물리 속도 제거 및 시뮬레이션 일시 정지
        _rigidbody2D.velocity = Vector2.zero;
        _rigidbody2D.angularVelocity = 0f;
        _rigidbody2D.simulated = false;

        // 4. 월드 좌표 기준으로 위치 이동
        transform.position = targetPosition;

        // 5. 물리 엔진에 바뀐 위치 즉시 동기화
        Physics2D.SyncTransforms();

        // 6. 물리 시뮬레이션 재개
        _rigidbody2D.simulated = true;

        // 7. 도착 즉시 바닥 체크 (폭발 방지)
        CheckForGround();

        SetInputLock(false);
    }

    private void UnlockInputAfterMove()
    {
        if (!_isOnIce) SetInputLock(false);
    }

    // Invoke 타겟 - 이동 후 물리/타일 반응이 끝난 뒤 호출됨
    // Ice 모드 중에는 발동을 차단합니다.
    // 슬라이드 중에는 행동이 "완료"되지 않으며,
    // Stop 타일에 닿을 때 StopIceAndFinish()가 대신 ActionFinished를 발동합니다.
    private void RaiseActionFinished()
    {
        if (GameManager.Instance.isGameOver || GameManager.Instance.isCleared) return;
        if (_isOnIce) return;      // 아직 슬라이드 중이면 차단
        if (isUndoRedo) return;    // Redo 중엔 차단 - BehaviourManager.RedoTurn이 직접 처리
        GameEvents.RaisePlayerActionFinished();
    }

    // Stop 타일에 닿았을 때 TileBehaviour에서 호출됩니다.
    // Ice 중 차단됐던 ActionFinished를 슬라이드 종료 시점에 발동합니다.
    public void StopIceAndFinish()
    {
        EnableIceMode(false);
        if (GameManager.Instance.isGameOver || GameManager.Instance.isCleared) return;
        GameEvents.RaisePlayerActionFinished();
    }

    #endregion

    #region Command Pattern
    //ICommand 인터페이스를 상속받은 커맨드를 전부 모아놓은 버퍼
    private Queue<ICommand> _commandBuffer = new Queue<ICommand>(); //커맨드는 queue로 구현

    public void SyncHistoryOnUndo()
    {
        if (_inputHistory.Count > 0)
        {
            _inputHistory.RemoveAt(_inputHistory.Count - 1);
            RebuildUI();
        }
    }

    // [추가] BehaviourManager가 Redo를 수행할 때 호출할 UI 동기화 함수
    public void SyncHistoryOnRedo(KeyType key)
    {
        _inputHistory.Add(key);
        RebuildUI();
    }
    
    private void RecordInput(KeyType key)
    {
        _inputHistory.Add(key);
    }

    private void RedoRecordInput(KeyType key)
    {
        _inputHistory.Add(key);
        RebuildUI();
    }

    private void RebuildUI()
    {
        ResetQueue();

        int count = _inputHistory.Count;
        int remain = count % MaxQueueSize;

        if (count == 0) return;

        int startIndex;

        if (remain == 0)
        {
            startIndex = count - MaxQueueSize;
        }
        else
        {
            startIndex = count - remain;
        }

        for (int i = 0; i < MaxQueueSize; i++)
        {
            int historyIndex = startIndex + i;

            if (historyIndex >= count) break;

            _inputQueue[i] = (int)_inputHistory[historyIndex];
            _stack++;
        }
        
        OnInputQueueChanged?.Invoke();

    }
    
    public bool isUndoRedo = false;

    public void UndoState()
    {
        // 게임오버 및 비주얼 복구
        GameManager.Instance.isGameOver = false;
        _collider2D.enabled = true;
        arrow.SetActive(true);
        _animator.Play("Idle");

        // UI 히스토리에서 마지막 입력 제거 및 재건축
        if (_inputHistory.Count > 0)
        {
            _inputHistory.RemoveAt(_inputHistory.Count - 1);
            RebuildUI();
        }
    
        Physics2D.SyncTransforms();
    }

    public void RedoState(KeyType key)
    {
        // 게임오버 상태일 수 있으므로 복구
        GameManager.Instance.isGameOver = false;
        _collider2D.enabled = true;
        arrow.SetActive(true);
        _animator.Play("Idle");

        // 히스토리에 다시 추가 후 UI 재건축
        _inputHistory.Add(key);
        RebuildUI();
    
        Physics2D.SyncTransforms();
    }
    
    #endregion

    #region Lifecycle

    private void Awake()
    {

        // GameManager에 자신을 등록합니다.
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterStackManager(this);
        }
        else
        {
            Debug.LogError("GameManager instance isn't registered!");
        }

        _animator = GetComponent<Animator>();
        _rigidbody2D = GetComponent<Rigidbody2D>();
        soundEffectPlayer = GetComponent<SoundEffectPlayer>();
        _collider2D = GetComponent<Collider2D>();

        _isInputLocked = true;

    }

    private void OnEnable()
    {
        //이벤트 추가
        GameEvents.PlayerDied += StopParticle;
        GameEvents.StageCleared += StopParticle;
        GameEvents.InputLockChanged += SetInputLock;
    

        GameEvents.OnEnemyTurnStarted += (_) => _isEnemyActing = true;
        GameEvents.OnPlayerTurnStarted += () => _isEnemyActing = false;

        // 맵 회전 시 플레이어와 적의 물리 로직을 활성화/비활성화 (여기선 플레이어의 물리 상태만)       
        GameEvents.BeforeMapRotated += FreezePlayerPhysicalLogic;
        GameEvents.AfterMapRotated  += FreezePlayerPhysicalLogic;

    }

    private void Start()
    {
        // 파티클 일단 끄기
        particle.Stop();

        // 트리거 false로 초기화
        _isInputLocked = true;
    }

    public bool CheckSkip()
    {
        return (_isRotating || _isInputLocked || _isMapBusy || _isEnemyActing);
    }
    
    private void Update()
    {
        // 회전 애니메이션 작동중일 땐 스킵
        if (CheckSkip()) return;
        
        // 게임이 진행 중일 때만 입력을 받도록 GameManager 상태를 확인합니다.
        if (GameManager.Instance.isGameOver || GameManager.Instance.isCleared) return;

        if (Input.GetKeyDown(KeyCode.LeftAlt) && GameManager.Instance.canUseLeftALT)
        {
            RecordInput(KeyType.Alt);
            HandleInput(KeyType.Alt); // 데이터 먼저 갱신
            
            _commandBuffer.Enqueue(new ClockwiseRotateCommand(this));
            soundEffectPlayer.PlaySoundEffect(rotateSound);
            
        }
        if (Input.GetKeyDown(KeyCode.Tab) && GameManager.Instance.canUseTAB)
        {
            RecordInput(KeyType.Tab);
            HandleInput(KeyType.Tab); // 데이터 먼저 갱신
            
            _commandBuffer.Enqueue(new CounterClockwiseRotateCommand(this));
            soundEffectPlayer.PlaySoundEffect(rotateSound);
            
        }
        if (Input.GetKeyDown(KeyCode.F4) && GameManager.Instance.canUseF4)
        {
            RecordInput(KeyType.F4);
            HandleInput(KeyType.F4); // 데이터 먼저 갱신
            
            _commandBuffer.Enqueue(new MoveCommand(this));
            soundEffectPlayer.PlaySoundEffect(moveSound);
            
        }

        // 예: 큐에 명령이 있고, 현재 애니메이션 중이 아닐 때만 하나씩 꺼내서 실행
        if (_commandBuffer.Count > 0 && !_isRotating && !_isInputLocked && !GameManager.Instance.isCleared)
        {
            ICommand cmd = _commandBuffer.Dequeue();
            behaviourManager.ExecuteCommand(cmd);

        }

        if (CheckBackTile()) // ALT + TAB 트리거가 가능한지 체크 
        {
            var main = particle.main; // 파티클의 Main 모듈을 가져옴 

            if (mapManager.IsFirstRoot()){
                
                Color purple = new Color(144 / 255f, 57 / 255f, 205 / 255f); //보라색
                main.startColor = purple; 
            }
            else {main.startColor = Color.white;}

            if (!particle.isPlaying) // 파티클이 재생 중이 아닐 때만 Play 호출
            {
                // 트리거 됐을 경우, 효과음 재생
                if (!_isTriggerd) soundEffectPlayer.PlaySoundEffect(triggerSound);

                particle.Play();
                _isTriggerd = true;
            }
        }
        else
        {
            // 파티클이 현재 멈춰있지 않고, 트리거 상태였다면 Stop()을 호출합니다.
            if (!particle.isStopped && _isTriggerd)
            {
                //트리거 취소 효과음 재생
                soundEffectPlayer.PlaySoundEffect(cancelSound);

                particle.Stop();
                _isTriggerd = false;
            }
        }
    }

    private void OnDisable()
    {
        //이벤트 추가
        GameEvents.PlayerDied -= StopParticle;
        GameEvents.StageCleared -= StopParticle;
        GameEvents.InputLockChanged -= SetInputLock;

        // 맵 회전 시 플레이어와 적의 물리 로직을 활성화/비활성화 (여기선 플레이어의 물리 상태만)
        GameEvents.BeforeMapRotated -= FreezePlayerPhysicalLogic;
        GameEvents.AfterMapRotated  -= FreezePlayerPhysicalLogic;
    }

    private void OnDestroy()
    {
        // 유니티 에디터에서 랜덤으로 OnDestroy를 실행해서, 가끔 NullReferenceException 오류가 뜸
        if (GameManager.Instance)
        {
            //파괴시, StackManager의 연결 해제
            GameManager.Instance.UnregisterStackManager();
        }
    }

    #endregion

    public bool CheckBackTile()
    {
        Transform inactiveMapRoot = mapManager.GetInactiveMapRoot();

        if (inactiveMapRoot == null) return false;

        // 플레이어 위치(transform.position)에 겹쳐 있는 모든 2D 충돌체 감지
        Collider2D[] hitColliders = Physics2D.OverlapPointAll(transform.position);

        foreach (var col in hitColliders)
        {
            if (col.transform.IsChildOf(inactiveMapRoot)) return true;
        }

        return false;
    }

    private void CheckForGround()
    {
        // 중요: 물리 엔진에 변경된 트랜스폼 정보를 즉시 반영
        Physics2D.SyncTransforms();

        Transform activeMapRoot = mapManager.GetActiveMapRoot();

        if (activeMapRoot == null)
        {
            PlayExplosion();
            return;
        }

        bool hasGround = false;

        // 현재 발밑 위치에 있는 모든 2D 콜라이더를 가져옴 
        Collider2D[] hitColliders = Physics2D.OverlapPointAll(transform.position);

        foreach (var col in hitColliders)
        {
            // 2. 이 콜라이더가 '현재 활성화된 맵 루트'의 자식인지 확인합니다.
            // 이 방식은 계층 구조가 아무리 깊어도(Grid 안에 있어도) 찾아냅니다.
            if (col.transform.IsChildOf(activeMapRoot))
            {
                hasGround = true;
                break;
            }
        }

        if (!hasGround)
        {
            PlayExplosion();
        }
    }
    public void HandleInput(KeyType keyType)
    {
        if (_stack >= MaxQueueSize) ResetQueue();

        _inputQueue[_stack] = (int)keyType;
        _stack++;

        OnInputQueueChanged?.Invoke();
        
        if (CheckGameOver())
        {
            _collider2D.enabled = false;
            PlayExplosion();
        }
        else if (CheckMapChange())
        {
            GameEvents.RaiseTileMapChanged(); //맵이 변경됐다는 이벤트 발동 (MapManager에서 수신)

            ResetQueue(); // 맵이 전환 된 후, Queue 초기화
            _inputHistory.Clear();

            GameEvents.RaiseInputLockChanged(true); // 입력 잠금 이벤트 발동 (StackManager에서 수신하여 _isInputLocked를 true로 설정)

            float time = 1.0f;
            
            // 패널 변경 효과를 나중에 셰이더로 수정해보자
            //StartCoroutine(FadeSwitchPanel(time)); // 패널 변경 이벤트 시작

            Physics2D.SyncTransforms();
            // 맵 전환 직후 지연 후 바닥 체크 (물리 엔진 갱신 대기)
            Invoke(nameof(CheckForGround), time);

            GameEvents.RaiseInputLockChanged(false); // 입력 잠금 해제 이벤트
        }
    }

    bool CheckGameOver()
    {
        return (_inputQueue[0] == (int)KeyType.Alt && _inputQueue[1] == (int)KeyType.F4)
               || (_inputQueue[1] == (int)KeyType.Alt && _inputQueue[2] == (int)KeyType.F4);
    }

    bool CheckMapChange()
    {
        return (_inputQueue[0] == (int)KeyType.Alt && _inputQueue[1] == (int)KeyType.Tab)
               || (_inputQueue[1] == (int)KeyType.Alt && _inputQueue[2] == (int)KeyType.Tab);
    }

    void ResetQueue()
    {
        _inputQueue = new List<int>(new int[MaxQueueSize]);
        _stack = 0;

        //큐가 리셋 될 경우에도 발동
        OnInputQueueChanged?.Invoke();
    }

    public void UpdateDirection(int rotation) => _playerDirection = (PlayerDirection)(((int)_playerDirection + rotation + 4) % 4);

    // ICommand 중 MoveCommand를 위한 메서드
    public void MovePlayer()
    {
        Vector2 moveDirection = _playerDirection switch
        {
            PlayerDirection.Right => Vector2.right,
            PlayerDirection.Down => Vector2.down,
            PlayerDirection.Left => Vector2.left,
            PlayerDirection.Up => Vector2.up,
            _ => Vector2.zero
        };

        _lastMoveDirection = moveDirection; // 방향 기억

        //Ice타일 반영
        if (_isOnIce)
        {
            // 이미 얼음 위에서 다시 이동 명령을 내린 경우 (방향 전환 등), 기존 미끄러짐을 교체
            if (_slideCoroutine != null) StopCoroutine(_slideCoroutine);
            _slideCoroutine = StartCoroutine(Slide(moveDirection));

            SetInputLock(true);
        }
        else
        {
            //Vector2 targetPos = _rigidbody2D.position + moveDirection;
            //_rigidbody2D.MovePosition(targetPos);
            _rigidbody2D.AddForce(moveDirection * forceAmount, ForceMode2D.Impulse);

            SetInputLock(true);
            Invoke(nameof(UnlockInputAfterMove), 0.2f);
        }
        
        GameEvents.RaisePlayerMoved(moveCount);
        GameEvents.RaisePlayerActed(totalActionCount);
        
        if (!isUndoRedo)
        {
            Invoke(nameof(RaiseActionFinished), 0.15f);
        }

        //맵 밖으로 벗어났는지 체크
        Invoke(nameof(CheckForGround), 0.1f);
        
    }

    // ICommand 중 ClockwiseRotateCommand/CounterClockwiseRotateCommand를 위한 메서드
    public void RotateArrow(bool immediate = false)
    {
        float targetAngle = _playerDirection switch
        {
            PlayerDirection.Right => 0f,
            PlayerDirection.Down => 270f,
            PlayerDirection.Left => 180f,
            PlayerDirection.Up => 90f,
            _ => 0f
        };

        if (immediate)
        {
            // 즉시 회전 (Undo/Redo 또는 맵 회전 직후용) - 이벤트 발생 안 함
            arrow.transform.rotation = Quaternion.Euler(0, 0, targetAngle);
        }
        else
        {
            // 회전 애니메이션 완료 후 이벤트 발생
            // RaisePlayerRotated/Acted는 회전이 실제로 끝난 뒤 발동해야
            // 토글 타일이 올바른 타이밍에 반응합니다.
            _isRotating = true;
            bool wasUndoRedo = isUndoRedo; // OnComplete 시점엔 플래그가 이미 해제돼있으므로 캡처
            arrow.transform
                .DORotate(new Vector3(0, 0, targetAngle), DOTweenDuration)
                .SetEase(Ease.OutElastic)
                .OnComplete(() =>
                {
                    _isRotating = false;
                    if (!wasUndoRedo)
                    {
                        GameEvents.RaisePlayerRotated(rotationCount);
                        GameEvents.RaisePlayerActed(totalActionCount);
                        GameEvents.RaisePlayerActionFinished(); // 턴 전환 체크 신호
                    }
                });

            transform.DOPunchRotation(DOTweenPunch, DOTweenDuration, DOTweenVibrato, 0.5f);
        }
        
        Invoke(nameof(CheckForGround), 0.1f);
    }

    public int CheckInputQueue(int slot)
    {
        return _inputQueue[slot];
    }

    public void PlayExplosion()
    {
        // 이미 폭발 애니메이션이 재생 중이라면 중복 실행 방지
        if (_animator.GetCurrentAnimatorStateInfo(0).IsName("Explosion")) return;

        _rigidbody2D.velocity = Vector2.zero;
        _animator.Play("Explosion");
        arrow.SetActive(false);

        // 효과음 실행
        soundEffectPlayer.PlaySoundEffect(explosionSound);
        GameEvents.RaisePlayerDied(); //플레이어가 죽었다는 방송을 내보낸다
    }

    public void ReachedDestination()
    {
        // 커맨드 버퍼 비우기 및 입력 잠금
        _isInputLocked = true;
        _commandBuffer.Clear();
        
        _animator.Play("Clear");
        arrow.SetActive(false);

        // 1초 뒤 코루틴 실행
        StartCoroutine(StageClear(1.0f));
    }

    private void StopParticle()
    {
        // 파티클이 재생 중일 경우
        if (particle.isPlaying)
        {
            // 새로운 파티클 생성을 막고, 기존 파티클도 즉시 제거합니다.
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        _isTriggerd = false;
    }

    IEnumerator StageClear(float time) // 목적지에 닿자마자 곧바로 Clear!이란 UI 텍스트가 출력되지 않도록 하는 코루틴
    {
        yield return new WaitForSeconds(time);

        GameEvents.RaiseStageCleared(); //게임이 클리어 됐다는 방송을 내보냄
    }

    public void FreezePlayerPhysicalLogic(bool freeze)
    {
        _isMapBusy = freeze;

        if (freeze)
        {
            _rigidbody2D.velocity = Vector2.zero;
            _rigidbody2D.angularVelocity = 0f;
        }

        _collider2D.enabled = !freeze;        
        _rigidbody2D.simulated = !freeze;

        if (!freeze) // 재활성화 됐을 시 물리 적용
        {
            //FreezeEnemyPhysicalLogic와는 달리, Physics2D.SyncTransforms()를 선언해버리면 타일이 무한회전해버리는 문제가 발생함
            Invoke(nameof(CheckForGround), 0.05f);
        }    
    }

    public bool IsFirstTile() // TileBehaviour의 FirstDestination/SecondDestination의 OnPlayerEnter에서 사용
    {
        return mapManager.IsFirstRoot();
    }

    public void InitPlayer()
    {
        // 물리 및 상태 초기화
        _rigidbody2D.velocity = Vector2.zero;
        _isRotating = false;
        
        //Ice 모드 초기화
        _isOnIce = false;
        _slideCoroutine = null;
        
        _stack = 0;

        _collider2D.enabled = true;
        
        // 파티클 초기화
        StopParticle();
    
        // 카운트 초기화
        moveCount = 0;
        rotationCount = 0;
        
        _inputHistory.Clear(); // Undo/Redo용 inputHistory 초기화
        ResetQueue(); // Command Queue 초기화

        // 3. 방향 초기화 (기본 우측)
        _playerDirection = PlayerDirection.Right;
        arrow.SetActive(true);
        RotateArrow(true); // 화살표 즉시 갱신

        // 4. 위치 이동
        transform.position = new Vector3(0.5f, 0.5f,0f);
        Physics2D.SyncTransforms();
        
        // 5. 애니메이션 초기화
        _animator.Play("Idle");
        
        GameEvents.RaiseUndoRedoCountChanged(0, 0);
        
    }
    

}